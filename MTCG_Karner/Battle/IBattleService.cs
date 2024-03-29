using System.Text;
using MTCG_Karner.Database.Repository;
using MTCG_Karner.Models;

namespace MTCG_Karner.Battle
{
    public interface IBattleService
    {
        void RunBattle(User user1, User user2);
    }

    public class BattleService : IBattleService
    {
        private readonly UserRepository _userRepository;
        private CardRepository _cardRepository = new CardRepository();
        private StringBuilder _battleLog = new StringBuilder();
        private List<Guid> defeatedCardIds = new List<Guid>();

        public BattleService()
        {
            _userRepository = new UserRepository();
        }

        public void RunBattle(User user1, User user2)
        {
            int round = 0;
            var user1RoundsWon = 0;
            var user2RoundsWon = 0;
            _battleLog.Clear();
            _battleLog.AppendLine("Battle started");

            var deck1 = _cardRepository.GetDeckByUserId(user1.Id);
            Shuffle(deck1);
            var deck2 = _cardRepository.GetDeckByUserId(user2.Id);
            Shuffle(deck2);

            _battleLog.AppendLine("Decks shuffled");
            _battleLog.AppendLine("");

            for (round = 0; round < deck1.Count; round++)
            {
                int true_round = round + 1;
                _battleLog.AppendLine("Battle-Round-" + true_round);

                var card1 = deck1[round];
                var card2 = deck2[round];

                var roundResult = BattleRound(card1, card2);
                if (roundResult == card1)
                {
                    user1RoundsWon++;
                    defeatedCardIds.Add(card2.Id);
                    LogRoundResult(round, user1, card1, card2);
                }
                else if (roundResult == card2)
                {
                    user2RoundsWon++;
                    defeatedCardIds.Add(card1.Id);
                    LogRoundResult(round, user2, card1, card2);
                }
                else
                {
                    LogRoundResult(round, null, card1, card2);
                }

                _battleLog.AppendLine("");
            }

            _battleLog.AppendLine("Battles are over - long live the king");
            if (round != 4) throw new BattleException("Law of four rounds has been broken");

            User battleWinner = null;
            User battleLoser = null;
            if (user1RoundsWon > user2RoundsWon)
            {
                battleWinner = user1;
                battleLoser = user2;
            }
            else if (user2RoundsWon > user1RoundsWon)
            {
                battleWinner = user2;
                battleLoser = user1;
            }

            if (battleWinner != null && battleLoser != null)
            {
                TransferDefeatedCards(battleWinner, battleLoser, defeatedCardIds);
                UpdateStats(battleWinner, battleLoser);
                LogBattleResult(battleWinner, battleLoser);
            }
            else
            {
                UpdateStatsDraw(user1, user2);
                LogBattleDraw(user1, user2);
            }

            CreateBattleLogFile(_battleLog.ToString());
        }

        public Card BattleRound(Card card1, Card card2)
        {
            if (!(card1 is MonsterCard && card2 is MonsterCard))
            {
                ApplyElementalEffectivenessBothDirections(card1, card2);
            }

            ApplySpecialAbilities(card1, card2);

            if (card1.Damage > card2.Damage) return card1;
            if (card2.Damage > card1.Damage) return card2;
            return null;
        }

        public void TransferDefeatedCards(User winner, User loser, List<Guid> defeatedCardIds)
        {
            var loserDeckCardIds = _cardRepository.GetDeckByUserId(loser.Id)
                .Select(card => card.Id)
                .ToHashSet();

            foreach (var cardId in defeatedCardIds)
            {
                if (loserDeckCardIds.Contains(cardId))
                {
                    _cardRepository.RemoveCardFromDeck(loser.Id, cardId);
                    _cardRepository.TransferCardOwnership(cardId, winner.Id);
                }
            }

            _cardRepository.RefillUserDeck(loser.Id);
            var winnerCards = _cardRepository.GetCardsByUserId(winner.Id);
            var loserCards = _cardRepository.GetCardsByUserId(loser.Id);

            Console.WriteLine($"Winner {winner.Username} now has {winnerCards.Count} cards.");
            _battleLog.AppendLine($"Winner {winner.Username} now possesses {winnerCards.Count} cards.");
            Console.WriteLine($"{loser.Username} now has {loserCards.Count} cards.");
            _battleLog.AppendLine($"{loser.Username} now possesses {loserCards.Count} cards.");
        }

        private void LogRoundResult(int roundNumber, User winner, Card card1, Card card2)
        {
            roundNumber += 1;
            if (winner != null)
            {
                Console.WriteLine(
                    $"Round {roundNumber}: {winner.Username} wins with {card1.Name} against {card2.Name}");
                _battleLog.AppendLine($"{winner.Username} wins with {card1.Name} against {card2.Name}");
            }
            else
            {
                Console.WriteLine($"Round {roundNumber}: Draw between {card1.Name} and {card2.Name}");
                _battleLog.AppendLine($"Draw between {card1.Name} and {card2.Name}");
            }
        }

        private void LogBattleResult(User winner, User loser)
        {
            Console.WriteLine("-B-Log:");
            Console.WriteLine($"Battle finished: {winner.Username} won against {loser.Username}");
            _battleLog.AppendLine($"The mighty {winner.Username} won against {loser.Username} the rat");
            _battleLog.AppendLine("");
            _battleLog.AppendLine("");
        }

        private void LogBattleDraw(User user1, User user2)
        {
            var drawMessage = "Battle resulted in a draw between " + user1.Username + " and " + user2.Username;
            Console.WriteLine(drawMessage);
            _battleLog.AppendLine(drawMessage);
        }

        private void CreateBattleLogFile(string logContent)
        {
            try
            {
                string basePath = AppDomain.CurrentDomain.BaseDirectory;
                string logDirectory = Path.Combine(basePath, "Battle", "BattleLogs");
                Console.WriteLine("-B-LogDirectory: " + logDirectory);

                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                string logFileName = $"{DateTime.Now:yyyyMMddHHmmss}.txt";
                string logFilePath = Path.Combine(logDirectory, logFileName);
                Console.WriteLine("-B-LogName: " + logFileName);
                File.WriteAllText(logFilePath, logContent);
                Console.WriteLine($"Log saved to: {logFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create battle log file: {ex.Message}");
            }
        }

        public void UpdateStats(User winner, User loser)
        {
            Console.WriteLine("-B-UpdateStats");
            Console.WriteLine("-B-UpdateStats-winner: " + winner.Username);
            Console.WriteLine("-B-UpdateStats-loser: " + loser.Username);

            _userRepository.UpdateUserStats(winner.Id, 3, 1, 0);
            _userRepository.UpdateUserStats(loser.Id, -5, 0, 1);
        }

        private void UpdateStatsDraw(User user1, User user2)
        {
            Console.WriteLine("-B-UpdateStatsDraw");

            var user1Elo = _userRepository.GetUserElo(user1.Id);
            var user2Elo = _userRepository.GetUserElo(user2.Id);

            if (user1Elo > user2Elo)
            {
                _userRepository.UpdateUserStats(user1.Id, -2, 0, 0);
                _userRepository.UpdateUserStats(user2.Id, 1, 0, 0);
                Console.WriteLine("user1Elo > user2Elo");
            }
            else if (user2Elo > user1Elo)
            {
                _userRepository.UpdateUserStats(user2.Id, -2, 0, 0);
                _userRepository.UpdateUserStats(user1.Id, 1, 0, 0);
                Console.WriteLine("user2Elo > user1Elo");
            }
            else
            {
                _userRepository.UpdateUserStats(user2.Id, 0, 0, 0);
                _userRepository.UpdateUserStats(user1.Id, 0, 0, 0);
                Console.WriteLine("Both players have equal ELO, no ELO changes applied.");
            }
        }

        public void ApplySpecialAbilities(Card card1, Card card2)
        {
            Console.WriteLine("-ApplySpecialAbilities");
            if (card1.MonsterType == MonsterType.Goblin && card2.MonsterType == MonsterType.Dragon)
            {
                _battleLog.AppendLine("Goblin is too afraid of Dragon to attack.");
                card1.Damage = 0;
            }

            if (card1.MonsterType == MonsterType.Wizard && card2.MonsterType == MonsterType.Ork)
            {
                _battleLog.AppendLine("Wizard can control Ork so they are not able to damage them.");
                card2.Damage = 0;
            }

            if (card1.ElementType == ElementType.Water && card2.MonsterType == MonsterType.Knight)
            {
                _battleLog.AppendLine("The armor of Knights is so heavy that WaterSpells make them drown instantly.");
                card1.Damage *= 1000;
            }

            if (card2.MonsterType == MonsterType.Kraken && card1.MonsterType == null)
            {
                _battleLog.AppendLine("The Kraken is immune against spells.");
                card1.Damage = 0;
            }

            if (card1.MonsterType == MonsterType.FireElf && card2.MonsterType == MonsterType.Dragon)
            {
                _battleLog.AppendLine("The FireElves know Dragons since they were little and can evade their attacks.");
                card2.Damage = 0;
            }
        }

        public void ApplyElementalEffectivenessBothDirections(Card card1, Card card2)
        {
            ApplyElementalEffectiveness(card1, card2);
            ApplyElementalEffectiveness(card2, card1);
        }

        public void ApplyElementalEffectiveness(Card card1, Card card2)
        {
            Console.WriteLine("-ApplyElementalEffectiveness");
            card1.ElementType = DetermineElementType(card1.Name);
            card2.ElementType = DetermineElementType(card2.Name);
            _battleLog.AppendLine("Elements: " + card1.ElementType + "-" + card2.ElementType);
            if (card1.ElementType == ElementType.Water && card2.ElementType == ElementType.Fire)
            {
                card1.Damage *= 2;
                _battleLog.AppendLine("(Effective)");
            }
            else if (card1.ElementType == ElementType.Fire && card2.ElementType == ElementType.Normal)
            {
                card1.Damage *= 2;
                _battleLog.AppendLine("(Effective)");
            }
            else if (card1.ElementType == ElementType.Normal && card2.ElementType == ElementType.Water)
            {
                card1.Damage *= 2;
                _battleLog.AppendLine("(Effective)");
            }
            else if (card1.ElementType == ElementType.Fire && card2.ElementType == ElementType.Water)
            {
                card1.Damage *= 0.5;
                _battleLog.AppendLine("(Not Effective)");
            }
            else if (card1.ElementType == ElementType.Normal && card2.ElementType == ElementType.Fire)
            {
                card1.Damage *= 0.5;
                _battleLog.AppendLine("(Not Effective)");
            }
            else if (card1.ElementType == ElementType.Water && card2.ElementType == ElementType.Normal)
            {
                card1.Damage *= 0.5;
                _battleLog.AppendLine("(Not Effective)");
            }
            else
            {
                _battleLog.AppendLine("(No Effect)");
            }
        }

        private ElementType DetermineElementType(string cardName)
        {
            switch (cardName)
            {
                case "Dragon":
                case "FireElf":
                case "FireSpell":
                    return ElementType.Fire;

                case "Ork":
                case "WaterSpell":
                case "WaterGoblin":
                    return ElementType.Water;

                case "Knight":
                case "RegularSpell":
                    return ElementType.Normal;

                default:
                    Console.WriteLine("Error: Card name does not match any known element types.");
                    return ElementType.Normal;
            }
        }

        private static Random _rng = new Random();

        private void Shuffle<T>(IList<T> deck)
        {
            int n = deck.Count;
            while (n > 1)
            {
                n--;
                int k = _rng.Next(n + 1);
                T value = deck[k];
                deck[k] = deck[n];
                deck[n] = value;
            }
        }
    }

    public class BattleException : Exception
    {
        public BattleException(string message) : base(message)
        {
        }
    }
}
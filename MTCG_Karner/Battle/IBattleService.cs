using MTCG_Karner.Database.Repository;
using MTCG_Karner.Models;

namespace MTCG_Karner.Battle;

public interface IBattleService
{
    void RunBattle(User user1, User user2);
}

public class BattleService : IBattleService
{
    private readonly UserRepository _userRepository;
    private CardRepository _cardRepository = new CardRepository();

    public BattleService()
    {
        _userRepository = new UserRepository();
    }

    public void RunBattle(User user1, User user2)
    {
        int round = 0;
        var user1RoundsWon = 0;
        var user2RoundsWon = 0;

        // Load user decks
        var deck1 = _cardRepository.GetDeckByUserId(user1.Id);
        Shuffle(deck1);
        var deck2 = _cardRepository.GetDeckByUserId(user2.Id);
        Shuffle(deck2);
        Console.WriteLine("-B-RunBattle-DecksShuffled");
        Console.WriteLine("-B-RunBattle-" + deck1.Count + "-Cards");
        // Execute the fixed number of battle rounds
        for (round = 0; round < deck1.Count; round++)
        {
            var card1 = deck1[round]; // Assuming deck1 has at least 4 cards
            var card2 = deck2[round]; // Assuming deck2 has at least 4 cards

            var roundResult = BattleRound(card1, card2);
            if (roundResult == card1)
            {
                user1RoundsWon++;
                LogRoundResult(round, user1, card1, card2);
            }

            if (roundResult == card2)
            {
                user2RoundsWon++;
                LogRoundResult(round, user2, card1, card2);
            }

            // Log the result of this round
        }

        if (round != 4) throw new BattleException("Law of four rounds has been broken");

        // Determine the overall winner based on rounds won
        User battleWinner;
        User battleLoser;
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
        else
        {
            // It's a draw, handle accordingly
            HandleDraw(user1, user2);
            return;
        }

        // Transfer the defeated cards to the winner
        TransferDefeatedCards(battleWinner, battleLoser);

        // Update stats for both users
        UpdateStats(battleWinner, battleLoser);

        // Log the result of the battle
        LogBattleResult(battleWinner, battleLoser);
    }

    private Card BattleRound(Card card1, Card card2)
    {
        // Implement the logic to execute a battle round using the card1 and card2
        // This should apply the special abilities and determine the winner of the round
        ApplySpecialAbilities(card1, card2);
        if (!(card1 is MonsterCard && card2 is MonsterCard))
        {
            ApplyElementalEffectivenessBothDirections(card1, card2);
        }

        if (card1.Damage > card2.Damage) return card1;
        if (card2.Damage > card1.Damage) return card2;
        return null; // Draw
    }


    private void TransferDefeatedCards(User winner, User loser)
    {
        // Implement the logic to transfer defeated cards to the winner
        // and refill the loser's deck with the next strongest cards
    }

    private void HandleDraw(User user1, User user2)
    {
        // Implement what happens in case of a draw
        Console.WriteLine("-B-RunBattle-Draw");
    }

    private void LogRoundResult(int roundNumber, User winner, Card card1, Card card2)
    {
        roundNumber += 1;
        if (winner != null)
        {
            Console.WriteLine($"Round {roundNumber}: {winner.Username} wins with {card1.Name} against {card2.Name}");
        }
        else
        {
            Console.WriteLine($"Round {roundNumber}: Draw between {card1.Name} and {card2.Name}");
        }
    }

    private void LogBattleResult(User winner, User loser)
    {
        Console.WriteLine("-B-Log:");
        // Implement logging of the battle result
        // You could serialize the battle log to JSON, or just a plain string, and then save it to the database or a file
        Console.WriteLine($"Battle finished: {winner.Username} won against {loser.Username}");
    }

    private void UpdateStats(User winner, User loser)
    {
        Console.WriteLine("-B-UpdateStats");
        Console.WriteLine("-B-UpdateStats-winner: " + winner.Username);
        Console.WriteLine("-B-UpdateStats-looser: " + loser.Username);

        _userRepository.UpdateUserStats(winner.Id, 3, 1, 0); // +3 ELO for the winner
        _userRepository.UpdateUserStats(loser.Id, -5, 0, 1); // -5 ELO for the loser
    }


    public void ApplySpecialAbilities(Card card1, Card card2)
    {
        // Goblin is too afraid of Dragon to attack.
        if (card1.MonsterType == MonsterType.Goblin && card2.MonsterType == MonsterType.Dragon)
            card1.Damage = 0;

        // Wizard can control Ork so they are not able to damage them.
        if (card1.MonsterType == MonsterType.Wizard && card2.MonsterType == MonsterType.Ork)
            card2.Damage = 0;

        // The armor of Knights is so heavy that WaterSpells make them drown instantly.
        if (card1.ElementType == ElementType.Water && card2.MonsterType == MonsterType.Knight)
            card1.Damage *= 1000;

        // The Kraken is immune against spells.
        if (card2.MonsterType == MonsterType.Kraken &&
            card1.MonsterType == null) // Assuming MonsterType is null for spells
            card1.Damage = 0;

        // The FireElves know Dragons since they were little and can evade their attacks.
        if (card1.MonsterType == MonsterType.FireElf && card2.MonsterType == MonsterType.Dragon)
            card2.Damage = 0;
    }

    public void ApplyElementalEffectivenessBothDirections(Card card1, Card card2)
    {
        ApplyElementalEffectiveness(card1, card2);
        ApplyElementalEffectiveness(card2, card1);
    }

    public void ApplyElementalEffectiveness(Card card1, Card card2)
    {
        if (card1.ElementType == ElementType.Water && card2.ElementType == ElementType.Fire)
        {
            card1.Damage *= 2;
        }
        else if (card1.ElementType == ElementType.Fire && card2.ElementType == ElementType.Normal)
        {
            card1.Damage *= 2;
        }
        else if (card1.ElementType == ElementType.Normal && card2.ElementType == ElementType.Water)
        {
            card1.Damage *= 2;
        }
        else if (card1.ElementType == ElementType.Fire && card2.ElementType == ElementType.Water)
        {
            card1.Damage *= 0.5;
        }
        else if (card1.ElementType == ElementType.Normal && card2.ElementType == ElementType.Fire)
        {
            card1.Damage *= 0.5;
        }
        else if (card1.ElementType == ElementType.Water && card2.ElementType == ElementType.Normal)
        {
            card1.Damage *= 0.5;
        }
        //no effect doesnt change damage
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
    public BattleException(string lawOfFourRoundsHasBeenBroken)
    {
        throw new NotImplementedException();
    }
}
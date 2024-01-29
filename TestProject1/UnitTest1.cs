using MTCG_Karner.Battle;
using MTCG_Karner.Database.Repository;
using MTCG_Karner.Models;
using NUnit.Framework;

namespace TestProject1
{
    [TestFixture]
    public class UnitTest1
    {
        private BattleService _battleService;
        private UserRepository _userRepository;

        [OneTimeSetUp]
        public void GlobalSetUp()
        {
            BattleTestUtility.ResetDatabase();
        }

        [SetUp]
        public void SetUp()
        {
            _battleService = new BattleService();
            _userRepository = new UserRepository();
        }

        [Test]
        public void Battle_Starts_When_Two_Users_Are_Present()
        {
            var user1 = BattleTestUtility.CreateTestUserWithDeck("User1");
            var user2 = BattleTestUtility.CreateTestUserWithDeck("User2");
            Assert.DoesNotThrow(() => _battleService.RunBattle(user1, user2));
        }

        [Test]
        public void Battle_Does_Not_Start_With_Only_One_User()
        {
            var user1 = BattleTestUtility.CreateTestUserWithDeck("User1");
            Assert.Throws<BattleException>(() => _battleService.RunBattle(user1, null));
        }

        [Test]
        public void User_Cannot_Join_Without_Valid_Deck()
        {
            var user1 = BattleTestUtility.CreateTestUser("User1");
            var lobby = BattleLobby.GetInstance(_battleService);
            Assert.Throws<NoValidDeckException>(() => lobby.TryJoinLobby(user1));
        }

        [Test]
        public void User_With_Full_Deck_Can_Join_Lobby()
        {
            var user1 = BattleTestUtility.CreateTestUserWithDeck("User1");
            var lobby = BattleLobby.GetInstance(_battleService);
            Assert.IsTrue(lobby.TryJoinLobby(user1));
        }

        [Test]
        public void Elemental_Effectiveness_Is_Applied_During_Battle()
        {
            var user1 = BattleTestUtility.CreateTestUserWithElementalDeck("User1", ElementType.Fire);
            var user2 = BattleTestUtility.CreateTestUserWithElementalDeck("User2", ElementType.Water);
            _battleService.RunBattle(user1, user2);
            var battleLog = _battleService.GetBattleLog();
            StringAssert.Contains("Water is effective against Fire", battleLog);
        }
    }
}

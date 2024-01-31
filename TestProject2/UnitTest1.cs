using System.Diagnostics;
using MTCG_Karner.Battle;
using MTCG_Karner.Controller;
using MTCG_Karner.Database.Repository;
using MTCG_Karner.Models;
using MTCG_Karner.Server;
using Newtonsoft.Json;
using NUnit.Framework;

namespace TestProject2;

[TestFixture]
public class Tests
{
    [SetUp]
    public void Setup()
    {
        DBTestUtility.ResetDatabase();
    }

    //------------------------------------------------------------------
    //--USER------------------------------------------------------------
    //------------------------------------------------------------------
    [Test]
    public void CreateUser_AddsNewUser_WhenUserDataIsValid()
    {
        //Arrange
        var username = "newUser";
        var password = "newPass";
        var user = new User { Username = username, Password = password };
        var userRepository = new UserRepository();

        //Act
        userRepository.CreateUser(user);

        //Assert
        var retrievedUser = userRepository.GetUserByUsername(username);
        Assert.IsNotNull(retrievedUser);
        Assert.AreEqual(username, retrievedUser.Username);
    }

    [Test]
    public void GetUserByUsername_ReturnsUser_WhenUserExists()
    {
        var username = "existingUser";
        var password = "testPassword";
        DBTestUtility.CreateUser(username, password);
        var userRepository = new UserRepository();

        var result = userRepository.GetUserByUsername(username);

        Assert.IsNotNull(result);
        Assert.AreEqual(username, result.Username);
    }

    [Test]
    public void GetUserByUsername_ReturnsNull_WhenUserDoesNotExist()
    {
        var userRepository = new UserRepository();

        var result = userRepository.GetUserByUsername("nonExistingUser");

        Assert.IsNull(result);
    }

    [Test]
    public void AuthenticateUser_ReturnsUser_WhenCredentialsAreValid()
    {
        var username = "authUser";
        var password = "authPass";
        DBTestUtility.CreateUser(username, password);
        var userRepository = new UserRepository();

        var headerText = $"Authorization: Bearer {username}-mtcgToken";
        var headers = new List<HttpHeader> { new HttpHeader(headerText) };
        var httpSvrEventArgs = new HttpSvrEventArgs(null, new List<HttpHeader> { new HttpHeader(headerText) });

        var result = userRepository.AuthenticateUser(httpSvrEventArgs);

        Assert.IsNotNull(result);
        Assert.AreEqual(username, result.Username);
    }

    [Test]
    public void DeleteUser_RemovesUser_WhenUserExists()
    {
        var username = "deleteUser";
        var password = "deletePass";
        DBTestUtility.CreateUser(username, password);
        var userRepository = new UserRepository();

        userRepository.DeleteUser(username);

        var result = userRepository.GetUserByUsername(username);
        Assert.IsNull(result);
    }

    [Test]
    public void UpdateUserData_UpdatesUser_WhenDataIsValid()
    {
        var username = "updateUser";
        var password = "updatePass";
        var newUser = DBTestUtility.CreateUser(username, password);
        var userRepository = new UserRepository();
        var updatedData = new UserDataDTO
        {
            Name = "Updated Name",
            Bio = "Updated Bio",
            Image = "Updated Image URL"
        };

        userRepository.UpdateUserData(username, updatedData);
        var updatedUser = userRepository.GetUserData(username);

        Assert.IsNotNull(updatedUser);
        Assert.AreEqual(updatedData.Name, updatedUser.Name);
        Assert.AreEqual(updatedData.Bio, updatedUser.Bio);
        Assert.AreEqual(updatedData.Image, updatedUser.Image);
    }

    [Test]
    public void GetUserStats_ReturnsCorrectStats_WhenUserExists()
    {
        var username = "statsUser";
        var password = "statsPass";
        var newUser = DBTestUtility.CreateUser(username, password);
        var userRepository = new UserRepository();

        var userStats = userRepository.GetUserStats(newUser.Id);

        Assert.IsNotNull(userStats);
        Assert.AreEqual(0, userStats.Wins);
        Assert.AreEqual(0, userStats.Losses);
        Assert.AreEqual(0, userStats.GamesPlayed);
        Assert.AreEqual(100, userStats.Elo);
    }


    //------------------------------------------------------------------
    //--PACKAGE---------------------------------------------------------
    //------------------------------------------------------------------
    [Test]
    public void CreatePackage_SuccessfullyCreatesPackage()
    {
        var packageRepository = new PackageRepository();
        var packageCards = new List<Card>
        {
            new Card { Id = Guid.NewGuid(), Name = "Card1", Damage = 10 },
            new Card { Id = Guid.NewGuid(), Name = "Card2", Damage = 20 },
            new Card { Id = Guid.NewGuid(), Name = "Card3", Damage = 30 },
            new Card { Id = Guid.NewGuid(), Name = "Card4", Damage = 40 },
            new Card { Id = Guid.NewGuid(), Name = "Card5", Damage = 50 }
        };

        packageRepository.CreatePackage(packageCards);

        Assert.IsTrue(DBTestUtility.CheckPackageExists(packageCards.Select(card => card.Id)));
    }

    [Test]
    public void AcquirePackageForUser_UserAcquiresPackageSuccessfully()
    {
        var userRepository = new UserRepository();
        var packageRepository = new PackageRepository();
        var username = "packageUser";
        var password = "packagePass";
        var user = DBTestUtility.CreateUser(username, password);
        DBTestUtility.CreateTestPackage();

        packageRepository.AcquirePackageForUser(user);

        Assert.IsTrue(DBTestUtility.CheckUserAcquiredPackage(user.Id));
    }

    [Test]
    public void IsPackageAvailable_ReturnsTrueWhenPackagesExist()
    {
        var packageRepository = new PackageRepository();
        DBTestUtility.CreateTestPackage();

        var result = packageRepository.IsPackageAvailable();

        Assert.IsTrue(result);
    }


    //------------------------------------------------------------------
    //--DECK------------------------------------------------------------
    //------------------------------------------------------------------
    [Test]
    public void GetDeck_ReturnsUserDeck_WhenUserExists()
    {
        var user = DBTestUtility.CreateUser("testUser", "testPassword");
        var card1 = DBTestUtility.CreateCard("Card1", 10);
        var card2 = DBTestUtility.CreateCard("Card2", 20);
        DBTestUtility.AddCardsToUserDeck(user.Id, new List<Guid> { card1.Id, card2.Id });

        var deckController = new DeckController();
        var mockHttpEventArgs = MockHttpSvrEventArgs(user.Username);

        deckController.GetDeck(mockHttpEventArgs);
        var resultDeck =
            JsonConvert.DeserializeObject<List<Card>>(mockHttpEventArgs.ResponseData);

        Assert.IsNotNull(resultDeck);
        Assert.AreEqual(2, resultDeck.Count);
        Assert.IsTrue(resultDeck.Any(c => c.Id == card1.Id));
        Assert.IsTrue(resultDeck.Any(c => c.Id == card2.Id));
    }

    [Test]
    public void ConfigureDeck_UpdatesUserDeck_WhenDeckIsValid()
    {
        var user = DBTestUtility.CreateUser("testUser", "testPassword");
        var card1 = DBTestUtility.CreateCard("Card1", 10);
        var card2 = DBTestUtility.CreateCard("Card2", 20);
        DBTestUtility.AddCardsToUserDeck(user.Id, new List<Guid> { card1.Id });

        var deckController = new DeckController();
        var newDeck = new List<Guid> { card2.Id };
        var mockHttpEventArgs = MockHttpSvrEventArgs(user.Username, JsonConvert.SerializeObject(newDeck));

        deckController.ConfigureDeck(mockHttpEventArgs);
        var updatedDeck = DBTestUtility.GetDeckByUserId(user.Id);

        Assert.IsNotNull(updatedDeck);
        Assert.AreEqual(1, updatedDeck.Count);
        Assert.AreEqual(card1.Id, updatedDeck.First().Id);
    }

    [Test]
    public void AddCardsToDeck_AddsCards_WhenDeckIsNotFull()
    {
        var user = DBTestUtility.CreateUser("testUser", "testPass");
        var card1 = DBTestUtility.CreateCard("Card1", 10, user.Username);
        var card2 = DBTestUtility.CreateCard("Card2", 20, user.Username);
        var cardRepository = new CardRepository();
        DBTestUtility.AddCardsToUserDeck(user.Id, new List<Guid> { card1.Id });

        cardRepository.AddCardsToDeck(user.Id, new List<Guid> { card2.Id });

        var updatedDeck = DBTestUtility.GetDeckByUserId(user.Id);
        Assert.AreEqual(2, updatedDeck.Count);
        Assert.IsTrue(updatedDeck.Any(c => c.Id == card1.Id));
        Assert.IsTrue(updatedDeck.Any(c => c.Id == card2.Id));
    }

    [Test]
    public void RemoveCardFromDeck_RemovesCard_WhenCardIsInDeck()
    {
        var user = DBTestUtility.CreateUser("testUser", "testPass");
        var card1 = DBTestUtility.CreateCard("Card1", 10, user.Username);
        var cardRepository = new CardRepository();
        DBTestUtility.AddCardsToUserDeck(user.Id, new List<Guid> { card1.Id });

        cardRepository.RemoveCardFromDeck(user.Id, card1.Id);

        var updatedDeck = DBTestUtility.GetDeckByUserId(user.Id);
        Assert.AreEqual(0, updatedDeck.Count);
        Assert.IsFalse(updatedDeck.Any(c => c.Id == card1.Id));
    }

    [Test]
    public void TransferCardOwnership_ChangesCardOwner_WhenUserExists()
    {
        var user1 = DBTestUtility.CreateUser("User1", "pass1");
        var user2 = DBTestUtility.CreateUser("User2", "pass2");
        var card = DBTestUtility.CreateCard("Card", 30, user1.Username);
        var cardRepository = new CardRepository();

        cardRepository.TransferCardOwnership(card.Id, user2.Id);

        var cardsOfUser2 = cardRepository.GetCardsByUserId(user2.Id);
        Assert.IsTrue(cardsOfUser2.Any(c => c.Id == card.Id));
    }


    //------------------------------------------------------------------
    //--BATTLE----------------------------------------------------------
    //------------------------------------------------------------------
    [Test]
    public void BattleRequest_JoinsLobbySuccessfully_WhenUserHasValidDeck()
    {
        var user = DBTestUtility.CreateUser("battleUser1", "pass");
        var card1 = DBTestUtility.CreateCard("Card1", 10, user.Username);
        var card2 = DBTestUtility.CreateCard("Card2", 20, user.Username);
        var card3 = DBTestUtility.CreateCard("Card3", 30, user.Username);
        var card4 = DBTestUtility.CreateCard("Card4", 40, user.Username);
        DBTestUtility.AddCardsToUserDeck(user.Id, new List<Guid> { card1.Id, card2.Id, card3.Id, card4.Id });
        var battleController = new BattleController();
        var mockHttpEventArgs = MockHttpSvrEventArgs(user.Username);

        battleController.HandleBattleRequest(mockHttpEventArgs);

        Assert.AreEqual(200, mockHttpEventArgs.ResponseCode);
        Assert.AreEqual("The battle has been carried out successfully.", mockHttpEventArgs.ResponseData);
    }

    [Test]
    public void BattleRequest_FailsToJoinLobby_WhenUserHasInvalidDeck()
    {
        var user = DBTestUtility.CreateUser("battleUser2", "pass");
        var card = DBTestUtility.CreateCard("Card1", 10, user.Username);
        DBTestUtility.AddCardsToUserDeck(user.Id, new List<Guid> { card.Id });
        var battleController = new BattleController();
        var mockHttpEventArgs = MockHttpSvrEventArgs(user.Username);

        battleController.HandleBattleRequest(mockHttpEventArgs);

        Assert.AreEqual(409, mockHttpEventArgs.ResponseCode);
        Assert.AreEqual("Could not join the lobby - invalid deck", mockHttpEventArgs.ResponseData);
    }

    [Test]
    public void StartBattle_RunsSuccessfully_WhenTwoUsersInLobby()
    {
        var user1 = DBTestUtility.CreateUser("battleUser3", "pass");
        var user2 = DBTestUtility.CreateUser("battleUser4", "pass");

        var card1User1 = DBTestUtility.CreateCard("Card1User1", 10, user1.Username);
        var card2User1 = DBTestUtility.CreateCard("Card2User1", 20, user1.Username);
        var card3User1 = DBTestUtility.CreateCard("Card3User1", 30, user1.Username);
        var card4User1 = DBTestUtility.CreateCard("Card4User1", 40, user1.Username);
        DBTestUtility.AddCardsToUserDeck(user1.Id,
            new List<Guid> { card1User1.Id, card2User1.Id, card3User1.Id, card4User1.Id });

        var card1User2 = DBTestUtility.CreateCard("Card1User2", 10, user2.Username);
        var card2User2 = DBTestUtility.CreateCard("Card2User2", 20, user2.Username);
        var card3User2 = DBTestUtility.CreateCard("Card3User2", 30, user2.Username);
        var card4User2 = DBTestUtility.CreateCard("Card4User2", 40, user2.Username);
        DBTestUtility.AddCardsToUserDeck(user2.Id,
            new List<Guid> { card1User2.Id, card2User2.Id, card3User2.Id, card4User2.Id });

        var battleLobby = BattleLobby.GetInstance(new BattleService());
        battleLobby.TryJoinLobby(user1);
        battleLobby.TryJoinLobby(user2);

        Assert.DoesNotThrow(() => battleLobby.CheckForBattle());
    }

    
    [Test]
    public void BattleRound_ResultIsDraw_WhenEqualDamage()
    {
        var card1 = new Card { Id = Guid.NewGuid(), Name = "Card1", Damage = 20 };
        var card2 = new Card { Id = Guid.NewGuid(), Name = "Card2", Damage = 20 };
        var battleService = new BattleService();

        var result = battleService.BattleRound(card1, card2);

        Assert.IsNull(result);
    }

    [Test]
    public void UpdateStats_ChangesEloAndStatsAfterBattle()
    {
        var winner = DBTestUtility.CreateUser("battleUser7", "pass");
        var loser = DBTestUtility.CreateUser("battleUser8", "pass");
        var battleService = new BattleService();

        battleService.UpdateStats(winner, loser);

        var winnerStats = DBTestUtility.GetUserStats(winner.Id);
        var loserStats = DBTestUtility.GetUserStats(loser.Id);
        Assert.Greater(winnerStats.Elo, 100);
        Assert.Less(loserStats.Elo, 100);
    }

    [Test]
    public void BattleRound_AppliesSpecialAbilities()
    {
        var goblinCard = new MonsterCard { Id = Guid.NewGuid(), Name = "Goblin", Damage = 20, MonsterType = MonsterType.Goblin };
        var dragonCard = new MonsterCard { Id = Guid.NewGuid(), Name = "Dragon", Damage = 50, MonsterType = MonsterType.Dragon };
        var battleService = new BattleService();

        var result = battleService.BattleRound(goblinCard, dragonCard);

        Assert.AreEqual(dragonCard, result);
    }



    //------------------------------------------------------------------
    //--GENERAL---------------------------------------------------------
    //------------------------------------------------------------------
    [Test]
    public void Test2()
    {
        Assert.Pass("This is a basic test that should pass.");
    }

    [TearDown]
    public void TearDown()
    {
        DBTestUtility.CleanUpDatabase();
    }

    private HttpSvrEventArgs MockHttpSvrEventArgs(string username, string payload = "")
    {
        var headers = new Dictionary<string, string>
        {
            { "Authorization", $"Bearer {username}-mtcgToken" }
        };
        return HttpSvrEventArgs.CreateForTest("GET", "/path", payload, headers);
    }
}

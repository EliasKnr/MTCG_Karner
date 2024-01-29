using MTCG_Karner.Database;
using MTCG_Karner.Models;
using Npgsql;
using System;
using System.Collections.Generic;

namespace TestProject1
{
    public static class BattleTestUtility
    {
        private static readonly string TestConnectionString = DBAccess.ConnectionString + ";Include Error Detail=true;";

        public static void ResetDatabase()
        {
        }

        public static User CreateTestUserWithDeck(string username)
        {
            var user = CreateTestUser(username);
            var cards = CreateTestCards();
            AddCardsToUserDeck(user.Id, cards);
            return user;
        }

        public static User CreateTestUserWithElementalDeck(string username, ElementType elementType)
        {
            var user = CreateTestUser(username);
            var cards = CreateElementalTestCards(elementType);
            AddCardsToUserDeck(user.Id, cards);
            return user;
        }
    }
}
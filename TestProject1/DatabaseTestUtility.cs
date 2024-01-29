using MTCG_Karner.Database;
using MTCG_Karner.Models;
using Npgsql;

namespace TestProject1
{
    public static class DatabaseTestUtility
    {
        private static readonly string TestConnectionString = DBAccess.ConnectionString + ";Include Error Detail=true;";

        public static void ResetDatabase()
        {
            using var conn = new NpgsqlConnection(TestConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                TRUNCATE TABLE users RESTART IDENTITY CASCADE;
                TRUNCATE TABLE cards RESTART IDENTITY CASCADE;
                TRUNCATE TABLE decks RESTART IDENTITY CASCADE;
            ";
            cmd.ExecuteNonQuery();
            conn.Close();
        }

        public static User CreateTestUser(string username)
        {
            using var conn = new NpgsqlConnection(TestConnectionString);
            conn.Open();
            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        INSERT INTO users (username, password, coins) VALUES (@username, @password, @coins) RETURNING id;
    ";
            // Hier generieren wir einen eindeutigen Benutzernamen mit einem Guid, um Kollisionen zu vermeiden.
            var uniqueUsername = $"{username}_{Guid.NewGuid()}";
            cmd.Parameters.AddWithValue("@username", uniqueUsername);
            cmd.Parameters.AddWithValue("@password", BCrypt.Net.BCrypt.HashPassword("password"));
            cmd.Parameters.AddWithValue("@coins", 100); // Default coins
            var userId = (int)cmd.ExecuteScalar();
            conn.Close();
            return new User { Id = userId, Username = uniqueUsername, Password = "password", Coins = 100 };
        }


        public static void CreateTestUsers()
        {
            using var conn = new NpgsqlConnection(TestConnectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand();
            cmd.Connection = conn;

            cmd.CommandText = @"
        INSERT INTO users (username, password) VALUES ('testUser1', 'testPassword1');
        INSERT INTO users (username, password) VALUES ('testUser2', 'testPassword2');
    ";
            cmd.ExecuteNonQuery();
        }

        public static List<Card> CreateTestCards()
        {
            var cards = new List<Card>
            {
                new Card { Id = Guid.NewGuid(), Name = "Dragon", Damage = 50 },
                new Card { Id = Guid.NewGuid(), Name = "Goblin", Damage = 10 },
                new Card { Id = Guid.NewGuid(), Name = "FireElf", Damage = 10 },
                new Card { Id = Guid.NewGuid(), Name = "Knight", Damage = 40 },
                // ... weitere Karten
            };

            using var conn = new NpgsqlConnection(TestConnectionString);
            conn.Open();
            foreach (var card in cards)
            {
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
            INSERT INTO cards (id, name, damage) VALUES (@id, @name, @damage);
        ";
                cmd.Parameters.AddWithValue("@id", card.Id);
                cmd.Parameters.AddWithValue("@name", card.Name);
                cmd.Parameters.AddWithValue("@damage", card.Damage);
                cmd.ExecuteNonQuery();
            }

            conn.Close();
            return cards;
        }


        public static void AddCardsToUserDeck(int userId, List<Card> cards)
        {
            if (cards.Count != 4)
            {
                throw new ArgumentException("Exactly 4 cards are required to create a deck.");
            }

            using var conn = new NpgsqlConnection(TestConnectionString);
            conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = @"
        INSERT INTO decks (user_id, card_id1, card_id2, card_id3, card_id4) 
        VALUES (@userId, @cardId1, @cardId2, @cardId3, @cardId4);
    ";
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@cardId1", cards[0].Id);
            cmd.Parameters.AddWithValue("@cardId2", cards[1].Id);
            cmd.Parameters.AddWithValue("@cardId3", cards[2].Id);
            cmd.Parameters.AddWithValue("@cardId4", cards[3].Id);
            cmd.ExecuteNonQuery();

            conn.Close();
        }


        public static void CleanUpDatabase()
        {
            // This method can remain identical to ResetDatabase if you want to clear all test data
            ResetDatabase();
        }
    }
}
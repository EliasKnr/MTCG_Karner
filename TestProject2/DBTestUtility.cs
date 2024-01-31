using MTCG_Karner.Database;
using MTCG_Karner.Models;
using Npgsql;

namespace TestProject2
{
    public static class DBTestUtility
    {
        private static string ConnectionString = DBAccess.ConnectionString;

        public static void ResetDatabase()
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();
            var cmd = new NpgsqlCommand
            {
                Connection = conn,
                CommandText = @"
                    TRUNCATE TABLE users CASCADE;
                    TRUNCATE TABLE cards CASCADE;
                    TRUNCATE TABLE packages CASCADE;
                    TRUNCATE TABLE decks CASCADE;
                "
            };
            cmd.ExecuteNonQuery();
        }

        public static User CreateUser(string username, string password)
        {
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();
            var cmd = new NpgsqlCommand
            {
                Connection = conn,
                CommandText =
                    "INSERT INTO users (username, password, coins) VALUES (@username, @password, 100) RETURNING id;",
                Parameters =
                {
                    new NpgsqlParameter("@username", username),
                    new NpgsqlParameter("@password", hashedPassword)
                }
            };
            var userId = (int)cmd.ExecuteScalar();
            return new User { Id = userId, Username = username, Password = hashedPassword };
        }

        public static Card CreateCard(string name, double damage, string ownerUsername = null)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();

            object ownerId = DBNull.Value; // Set default to DBNull
            if (ownerUsername != null)
            {
                using var cmdOwner = new NpgsqlCommand("SELECT id FROM users WHERE username = @username", conn);
                cmdOwner.Parameters.AddWithValue("@username", ownerUsername);
                var ownerIdResult = cmdOwner.ExecuteScalar();
                if (ownerIdResult != null)
                    ownerId = ownerIdResult; // Assign actual ID if found
            }

            var cardId = Guid.NewGuid();
            using var cmdCard =
                new NpgsqlCommand(
                    "INSERT INTO cards (id, name, damage, owner_id) VALUES (@id, @name, @damage, @ownerId)", conn);
            cmdCard.Parameters.AddWithValue("@id", cardId);
            cmdCard.Parameters.AddWithValue("@name", name);
            cmdCard.Parameters.AddWithValue("@damage", damage);
            cmdCard.Parameters.AddWithValue("@ownerId", ownerId);
            cmdCard.ExecuteNonQuery();

            return new Card { Id = cardId, Name = name, Damage = damage };
        }


        public static void CreateTestPackage()
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();
            using var transaction = conn.BeginTransaction();

            try
            {
                // Create five test cards
                List<Guid> cardIds = new List<Guid>();
                for (int i = 0; i < 5; i++)
                {
                    var cardId = Guid.NewGuid();
                    var insertCardQuery = "INSERT INTO cards (id, name, damage) VALUES (@Id, @Name, @Damage)";
                    using var cmdCard = new NpgsqlCommand(insertCardQuery, conn);
                    cmdCard.Parameters.AddWithValue("@Id", cardId);
                    cmdCard.Parameters.AddWithValue("@Name", $"TestCard{i}");
                    cmdCard.Parameters.AddWithValue("@Damage", 10 * (i + 1));
                    cmdCard.ExecuteNonQuery();
                    cardIds.Add(cardId);
                }

                // Create a package with these cards
                var insertPackageQuery =
                    "INSERT INTO packages (card_id1, card_id2, card_id3, card_id4, card_id5) VALUES (@CardId1, @CardId2, @CardId3, @CardId4, @CardId5)";
                using var cmdPackage = new NpgsqlCommand(insertPackageQuery, conn);
                for (int i = 1; i <= 5; i++)
                {
                    cmdPackage.Parameters.AddWithValue($"@CardId{i}", cardIds[i - 1]);
                }

                cmdPackage.ExecuteNonQuery();

                transaction.Commit();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception($"Error creating test package: {ex.Message}", ex);
            }
        }

        public static bool CheckPackageExists(IEnumerable<Guid> cardIds)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();
            var query =
                "SELECT COUNT(*) FROM packages WHERE card_id1 = @CardId1 OR card_id2 = @CardId2 OR card_id3 = @CardId3 OR card_id4 = @CardId4 OR card_id5 = @CardId5";
            using var cmd = new NpgsqlCommand(query, conn);
            var cardIdArray = cardIds.ToArray();
            for (int i = 1; i <= 5; i++)
            {
                cmd.Parameters.AddWithValue($"@CardId{i}", cardIdArray[i - 1]);
            }

            var count = (long)cmd.ExecuteScalar();
            return count > 0;
        }

        public static bool CheckUserAcquiredPackage(int userId)
        {
            using var conn = new NpgsqlConnection(ConnectionString);
            conn.Open();
            var query = "SELECT COUNT(*) FROM cards WHERE owner_id = @UserId";
            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UserId", userId);
            var count = (long)cmd.ExecuteScalar();
            return count > 0;
        }

        public static void AddCardsToUserDeck(int userId, List<Guid> cardIds)
        {
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                conn.Open();
                foreach (var cardId in cardIds)
                {
                    var cmd = new NpgsqlCommand(
                        "INSERT INTO decks (user_id, card_id1, card_id2, card_id3, card_id4) VALUES (@UserId, @CardId, NULL, NULL, NULL)",
                        conn);
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@CardId", cardId);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static List<Card> GetDeckByUserId(int userId)
        {
            List<Card> userDeck = new List<Card>();
            string query = @"
        SELECT c.id, c.name, c.damage 
        FROM cards c 
        INNER JOIN decks d ON c.id = d.card_id1 OR c.id = d.card_id2 OR c.id = d.card_id3 OR c.id = d.card_id4
        WHERE d.user_id = @UserId";

            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            userDeck.Add(new Card
                            {
                                Id = reader.GetGuid(0),
                                Name = reader.GetString(1),
                                Damage = reader.GetDouble(2)
                            });
                        }
                    }
                }
            }

            return userDeck;
        }
        
        public static List<Card> GetCardsByUserId(int userId)
        {
            List<Card> cards = new List<Card>();
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                conn.Open();
                var cmd = new NpgsqlCommand("SELECT id, name, damage FROM cards WHERE owner_id = @UserId", conn);
                cmd.Parameters.AddWithValue("@UserId", userId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        cards.Add(new Card
                        {
                            Id = reader.GetGuid(0),
                            Name = reader.GetString(1),
                            Damage = reader.GetDouble(2)
                        });
                    }
                }
            }
            return cards;
        }

        public static UserStats GetUserStats(int userId)
        {
            UserStats userStats = null;
            using (var conn = new NpgsqlConnection(ConnectionString))
            {
                conn.Open();
                var cmd = new NpgsqlCommand("SELECT wins, losses, games_played, elo FROM users WHERE id = @UserId", conn);
                cmd.Parameters.AddWithValue("@UserId", userId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        userStats = new UserStats
                        {
                            Wins = reader.GetInt32(0),
                            Losses = reader.GetInt32(1),
                            GamesPlayed = reader.GetInt32(2),
                            Elo = reader.GetInt32(3)
                        };
                    }
                }
            }
            return userStats;
        }



        public static void CleanUpDatabase()
        {
            ResetDatabase(); // Optionally, implement more fine-grained cleanup logic if needed
        }
    }
}
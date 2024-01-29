using System.Security.Authentication;
using MTCG_Karner.Models;
using MTCG_Karner.Server;
using Npgsql;

namespace MTCG_Karner.Database.Repository
{
    public class PackageRepository
    {
        private UserRepository _userRepository = new UserRepository();

        public void CreatePackage(List<Card> packageCards)
        {
            if (packageCards.Count != 5)
                throw new ArgumentException("A package must contain exactly 5 cards.");

            using (var conn = new NpgsqlConnection(DBAccess.ConnectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        List<Guid> cardIds = new List<Guid>();
                        foreach (var card in packageCards)
                        {
                            string insertCardQuery =
                                "INSERT INTO cards (id, name, damage, owner_id) VALUES (@Id, @Name, @Damage, @OwnerId) RETURNING id";
                            var cmd = new NpgsqlCommand(insertCardQuery, conn);
                            cmd.Parameters.AddWithValue("@Id", card.Id);
                            cmd.Parameters.AddWithValue("@Name", card.Name);
                            cmd.Parameters.AddWithValue("@Damage", card.Damage);
                            cmd.Parameters.AddWithValue("@OwnerId", DBNull.Value);
                            cardIds.Add((Guid)cmd.ExecuteScalar());
                        }

                        string insertPackageQuery =
                            "INSERT INTO packages (card_id1, card_id2, card_id3, card_id4, card_id5) VALUES (@CardId1, @CardId2, @CardId3, @CardId4, @CardId5)";
                        var packageCmd = new NpgsqlCommand(insertPackageQuery, conn);
                        for (int i = 1; i <= 5; i++)
                        {
                            packageCmd.Parameters.AddWithValue($"@CardId{i}", cardIds[i - 1]);
                        }

                        packageCmd.ExecuteNonQuery();

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public void AcquirePackageForUser(User user)
        {
            if (!IsPackageAvailable())
            {
                throw new NoPackagesAvailableException("No packages available.");
            }

            using (var conn = new NpgsqlConnection(DBAccess.ConnectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        var selectCmd = new NpgsqlCommand(
                            "SELECT id, card_id1, card_id2, card_id3, card_id4, card_id5 FROM packages LIMIT 1",
                            conn);

                        int packageId;
                        List<Guid> packageCardIds = new List<Guid>();

                        using (var reader = selectCmd.ExecuteReader())
                        {
                            if (!reader.Read())
                            {
                                throw new NoPackagesAvailableException("No packages available.");
                            }

                            packageId = reader.GetInt32(reader.GetOrdinal("id"));

                            for (int i = 1; i <= 5; i++)
                            {
                                packageCardIds.Add(reader.GetGuid(reader.GetOrdinal($"card_id{i}")));
                            }

                            reader.Close();
                        }

                        foreach (var cardId in packageCardIds)
                        {
                            var updateCmd = new NpgsqlCommand(
                                "UPDATE cards SET owner_id = @OwnerId WHERE id = @CardId", conn);
                            updateCmd.Parameters.AddWithValue("@OwnerId", user.Id);
                            updateCmd.Parameters.AddWithValue("@CardId", cardId);
                            updateCmd.ExecuteNonQuery();
                        }

                        var deleteCmd = new NpgsqlCommand("DELETE FROM packages WHERE id = @PackageId", conn);
                        deleteCmd.Parameters.AddWithValue("@PackageId", packageId);
                        deleteCmd.ExecuteNonQuery();

                        var checkDeckExistsCmd =
                            new NpgsqlCommand("SELECT COUNT(*) FROM decks WHERE user_id = @UserId", conn);
                        checkDeckExistsCmd.Parameters.AddWithValue("@UserId", user.Id);
                        var deckExists = (long)checkDeckExistsCmd.ExecuteScalar() > 0;

                        if (!deckExists)
                        {
                            var topCardIds = packageCardIds.OrderByDescending(id =>
                            {
                                var damageCmd = new NpgsqlCommand("SELECT damage FROM cards WHERE id = @CardId", conn);
                                damageCmd.Parameters.AddWithValue("@CardId", id);
                                return Convert.ToDouble(damageCmd.ExecuteScalar());
                            }).Take(4).ToList();

                            var deckInsertCmd = new NpgsqlCommand(
                                "INSERT INTO decks (user_id, card_id1, card_id2, card_id3, card_id4) VALUES (@UserId, @CardId1, @CardId2, @CardId3, @CardId4)",
                                conn);
                            deckInsertCmd.Parameters.AddWithValue("@UserId", user.Id);
                            for (int i = 0; i < topCardIds.Count; i++)
                            {
                                deckInsertCmd.Parameters.AddWithValue($"@CardId{i + 1}", topCardIds[i]);
                            }

                            deckInsertCmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch (NoPackagesAvailableException)
                    {
                        transaction.Rollback();
                        throw;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"Error during package acquisition: {ex}");
                        throw;
                    }
                }
            }
        }

        public bool IsPackageAvailable()
        {
            using (var conn = new NpgsqlConnection(DBAccess.ConnectionString))
            {
                conn.Open();
                var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM packages", conn);
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count > 0;
            }
        }

        public class NoPackagesAvailableException : Exception
        {
            public NoPackagesAvailableException(string message) : base(message)
            {
            }
        }
    }
}

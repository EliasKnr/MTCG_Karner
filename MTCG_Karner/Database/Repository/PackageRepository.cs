using MTCG_Karner.Models;
using Npgsql;

namespace MTCG_Karner.Database.Repository;

public class PackageRepository
{
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
                    // Insert cards and get their IDs
                    List<Guid> cardIds = new List<Guid>();
                    foreach (var card in packageCards)
                    {
                        string insertCardQuery =
                            "INSERT INTO cards (id, name, damage, owner_id) VALUES (@Id, @Name, @Damage, @OwnerId) RETURNING id";
                        var cmd = new NpgsqlCommand(insertCardQuery, conn);
                        cmd.Parameters.AddWithValue("@Id", card.Id);
                        cmd.Parameters.AddWithValue("@Name", card.Name);
                        cmd.Parameters.AddWithValue("@Damage", card.Damage);
                        cmd.Parameters.AddWithValue("@OwnerId", DBNull.Value); // Or use a specific admin ID
                        cardIds.Add((Guid)cmd.ExecuteScalar());
                    }

                    // Insert package with card references
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
    using (var conn = new NpgsqlConnection(DBAccess.ConnectionString))
    {
        conn.Open();
        using (var transaction = conn.BeginTransaction())
        {
            try
            {
                // Fetch a random package
                var selectCmd = new NpgsqlCommand("SELECT id, card_id1, card_id2, card_id3, card_id4, card_id5 FROM packages ORDER BY RANDOM() LIMIT 1", conn);
                int packageId;
                List<Guid> cardIds = new List<Guid>(); // List to store card IDs

                using (var reader = selectCmd.ExecuteReader())
                {
                    if (!reader.Read())
                        throw new Exception("No packages available.");

                    packageId = reader.GetInt32(reader.GetOrdinal("id"));

                    // Retrieve and store all card IDs
                    for (int i = 1; i <= 5; i++)
                    {
                        cardIds.Add(reader.GetGuid(reader.GetOrdinal($"card_id{i}")));
                    }
                }

                // Transfer ownership of cards
                foreach (var cardId in cardIds)
                {
                    var updateCmd = new NpgsqlCommand("UPDATE cards SET owner_id = @OwnerId WHERE id = @CardId", conn);
                    updateCmd.Parameters.AddWithValue("@OwnerId", user.Id);
                    Console.WriteLine($"OWNER-ID: " + user.Id);
                    updateCmd.Parameters.AddWithValue("@CardId", cardId); // Use the stored card ID
                    Console.WriteLine($"CARD-ID: " + cardId);
                    updateCmd.ExecuteNonQuery();
                }

                // Remove the acquired package
                var deleteCmd = new NpgsqlCommand("DELETE FROM packages WHERE id = @PackageId", conn);
                deleteCmd.Parameters.AddWithValue("@PackageId", packageId);
                deleteCmd.ExecuteNonQuery();

                transaction.Commit(); // Commit the transaction
            }
            catch (Exception ex)
            {
                transaction.Rollback(); // Rollback the transaction in case of an error
                throw; // Re-throw the exception to be handled elsewhere
            }
        }
    }
}

}
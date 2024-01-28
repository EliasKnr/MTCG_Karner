using MTCG_Karner.Models;
using Npgsql;

namespace MTCG_Karner.Database.Repository;

public class CardRepository
{
    public List<Card> GetCardsByUserId(int userId)
    {
        List<Card> userCards = new List<Card>();
        string query = "SELECT * FROM cards WHERE owner_id = @UserId";

        using (var conn = new NpgsqlConnection(DBAccess.ConnectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var card = new Card
                        {
                            Id = reader.GetGuid(reader.GetOrdinal("id")),
                            Name = reader.GetString(reader.GetOrdinal("name")),
                            Damage = reader.GetDouble(reader.GetOrdinal("damage"))
                            // Add additional fields if your Card model has more
                        };
                        userCards.Add(card);
                    }
                }
            }
        }

        return userCards;
    }

    public List<Card> GetDeckByUserId(int userId)
    {
        List<Card> userDeck = new List<Card>();

        try
        {
            using (var conn = new NpgsqlConnection(DBAccess.ConnectionString))
            {
                conn.Open();
                var cmd = new NpgsqlCommand(@"SELECT c.id, c.name, c.damage 
                                          FROM cards c 
                                          INNER JOIN decks d ON c.id = d.card_id1 OR c.id = d.card_id2 OR c.id = d.card_id3 OR c.id = d.card_id4
                                          WHERE d.user_id = @UserId", conn);
                cmd.Parameters.AddWithValue("@UserId", userId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var card = new Card
                        {
                            Id = reader.GetGuid(0),
                            Name = reader.GetString(1),
                            Damage = reader.GetDouble(2),
                            Destroyed = false
                        };
                        userDeck.Add(card);
                    }
                }
            }
        }
        catch (NpgsqlException ex)
        {
            Console.WriteLine($"Database error in GetDeckByUserId: {ex.Message}");
            // Handle or log the database error as needed
            throw;
        }

        return userDeck;
    }

    public int GetDeckSizeByUserId(int userId)
    {
        string query = "SELECT SUM(CASE WHEN card_id1 IS NULL THEN 1 ELSE 0 END + CASE WHEN card_id2 IS NULL THEN 1 ELSE 0 END + CASE WHEN card_id3 IS NULL THEN 1 ELSE 0 END + CASE WHEN card_id4 IS NULL THEN 1 ELSE 0 END) AS NullCount FROM decks WHERE user_id = @UserId";
        using (var conn = new NpgsqlConnection(DBAccess.ConnectionString))
        using (var cmd = new NpgsqlCommand(query, conn))
        {
            cmd.Parameters.AddWithValue("@UserId", userId);
            conn.Open();
            int missing_cards = Convert.ToInt32(cmd.ExecuteScalar());
            int card_cnt = 4 - missing_cards;
            return card_cnt;
        }
    }


    //#### own DeckRepo maybe???
    public void ConfigureDeck(int userId, List<Guid> cardIds)
    {
        using (var conn = new NpgsqlConnection(DBAccess.ConnectionString))
        {
            conn.Open();
            using (var transaction = conn.BeginTransaction())
            {
                try
                {
                    // Check if the user owns the cards
                    foreach (var cardId in cardIds)
                    {
                        var checkOwnershipCmd =
                            new NpgsqlCommand("SELECT COUNT(*) FROM cards WHERE id = @CardId AND owner_id = @OwnerId",
                                conn);
                        Console.WriteLine("-userID--: " + userId);
                        checkOwnershipCmd.Parameters.AddWithValue("@CardId", cardId);
                        checkOwnershipCmd.Parameters.AddWithValue("@OwnerId", userId);

                        long ownershipCount = (long)checkOwnershipCmd.ExecuteScalar();
                        if (ownershipCount == 0)
                        {
                            throw new CardOwnershipException("User does not own one or more specified cards.");
                        }
                    }

                    // Delete the current deck (if exists)
                    var deleteDeckCmd = new NpgsqlCommand("DELETE FROM decks WHERE user_id = @UserId", conn);
                    deleteDeckCmd.Parameters.AddWithValue("@UserId", userId);
                    deleteDeckCmd.ExecuteNonQuery();

                    // Insert new deck configuration
                    var insertDeckCmd =
                        new NpgsqlCommand(
                            "INSERT INTO decks (user_id, card_id1, card_id2, card_id3, card_id4) VALUES (@UserId, @CardId1, @CardId2, @CardId3, @CardId4)",
                            conn);
                    insertDeckCmd.Parameters.AddWithValue("@UserId", userId);
                    insertDeckCmd.Parameters.AddWithValue("@CardId1", cardIds[0]);
                    insertDeckCmd.Parameters.AddWithValue("@CardId2", cardIds[1]);
                    insertDeckCmd.Parameters.AddWithValue("@CardId3", cardIds[2]);
                    insertDeckCmd.Parameters.AddWithValue("@CardId4", cardIds[3]);
                    insertDeckCmd.ExecuteNonQuery();

                    transaction.Commit();
                }
                catch (NpgsqlException ex)
                {
                    Console.WriteLine($"Database error in ConfigureDeck: {ex.Message}");
                    transaction.Rollback();
                    throw;
                }
                catch (CardOwnershipException)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
    }

    public class CardOwnershipException : Exception
    {
        public CardOwnershipException(string message) : base(message)
        {
        }
    }
}
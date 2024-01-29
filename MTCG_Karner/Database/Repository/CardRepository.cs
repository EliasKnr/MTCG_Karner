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
            throw;
        }

        return userDeck;
    }

    public int GetDeckSizeByUserId(int userId)
    {
        string query = @"
        SELECT SUM(
            CASE WHEN card_id1 IS NOT NULL THEN 1 ELSE 0 END +
            CASE WHEN card_id2 IS NOT NULL THEN 1 ELSE 0 END +
            CASE WHEN card_id3 IS NOT NULL THEN 1 ELSE 0 END +
            CASE WHEN card_id4 IS NOT NULL THEN 1 ELSE 0 END
        ) AS CardCount 
        FROM decks 
        WHERE user_id = @UserId";

        using (var conn = new NpgsqlConnection(DBAccess.ConnectionString))
        using (var cmd = new NpgsqlCommand(query, conn))
        {
            cmd.Parameters.AddWithValue("@UserId", userId);
            conn.Open();
            var result = cmd.ExecuteScalar();

            if (result is DBNull)
                return 0;

            return Convert.ToInt32(result);
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

                    var deleteDeckCmd = new NpgsqlCommand("DELETE FROM decks WHERE user_id = @UserId", conn);
                    deleteDeckCmd.Parameters.AddWithValue("@UserId", userId);
                    deleteDeckCmd.ExecuteNonQuery();

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

    public void RemoveCardFromDeck(int userId, Guid cardId)
    {
        //Console.WriteLine("-RemoveCardFromDeck");
        string[] cardSlots = { "card_id1", "card_id2", "card_id3", "card_id4" };

        using (var conn = new NpgsqlConnection(DBAccess.ConnectionString))
        {
            conn.Open();
            foreach (var slot in cardSlots)
            {
                var query = $"UPDATE decks SET {slot} = NULL WHERE user_id = @UserId AND {slot} = @CardId";
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@CardId", cardId);
                    if (cmd.ExecuteNonQuery() > 0)
                    {
                        break;
                    }
                }
            }
        }
    }


    public void TransferCardOwnership(Guid cardId, int newOwnerId)
    {
        //Console.WriteLine("-TransferCardOwnership");

        string query = "UPDATE cards SET owner_id = @NewOwnerId WHERE id = @CardId";
        using (var conn = new NpgsqlConnection(DBAccess.ConnectionString))
        using (var cmd = new NpgsqlCommand(query, conn))
        {
            cmd.Parameters.AddWithValue("@NewOwnerId", newOwnerId);
            cmd.Parameters.AddWithValue("@CardId", cardId);

            conn.Open();
            cmd.ExecuteNonQuery();
        }
    }

    public void RefillUserDeck(int userId)
    {
        Console.WriteLine("-RefillUserDeck");
        var deck = GetDeckSizeByUserId(userId);
        Console.WriteLine("-RefillUserDeck-DeckSize: " + deck);
        int cardsNeeded = 4 - deck;
        Console.WriteLine("-RefillUserDeck-CardsNeeded: " + cardsNeeded);
        if (cardsNeeded > 0)
        {
            var availableCards = GetCardsByUserId(userId);
            Console.WriteLine("-RefillUserDeck-AvailableCards: " + availableCards);
            if (availableCards.Count < cardsNeeded)
            {
                Console.WriteLine(
                    $"User {userId} does not have enough cards to refill the deck. Please acquire more cards. (Only {availableCards.Count})");
                return;
            }

            var selectedCards = availableCards.OrderBy(x => Guid.NewGuid()).Take(cardsNeeded).ToList();
            AddCardsToDeck(userId, selectedCards.Select(card => card.Id));
        }
    }

    public void AddCardsToDeck(int userId, IEnumerable<Guid> cardIds)
    {
        var deck = GetDeckByUserId(userId);

        foreach (var cardId in cardIds)
        {
            if (deck.Count < 4)
            {
                string slot = $"card_id{deck.Count + 1}";
                string query = $"UPDATE decks SET {slot} = @CardId WHERE user_id = @UserId";

                using (var conn = new NpgsqlConnection(DBAccess.ConnectionString))
                using (var cmd = new NpgsqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@UserId", userId);
                    cmd.Parameters.AddWithValue("@CardId", cardId);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                deck.Add(new Card { Id = cardId }); 
            }
            else
            {
                break; // The deck is already full
            }
        }
    }


    /*
    public void TransferCardOwnership(Guid cardId, int newOwnerId)
    {
        string queryOwnership = "UPDATE cards SET owner_id = @NewOwnerId WHERE id = @CardId";
        string queryDeckRemoval = "DELETE FROM deck WHERE card_id = @CardId";

        using (var conn = new NpgsqlConnection(DBAccess.ConnectionString))
        {
            conn.Open();

            using (var cmdOwnership = new NpgsqlCommand(queryOwnership, conn))
            {
                cmdOwnership.Parameters.AddWithValue("@NewOwnerId", newOwnerId);
                cmdOwnership.Parameters.AddWithValue("@CardId", cardId);
                cmdOwnership.ExecuteNonQuery();
            }

            using (var cmdDeckRemoval = new NpgsqlCommand(queryDeckRemoval, conn))
            {
                cmdDeckRemoval.Parameters.AddWithValue("@CardId", cardId);
                cmdDeckRemoval.ExecuteNonQuery();
            }
        }
    }*/


    public class CardOwnershipException : Exception
    {
        public CardOwnershipException(string message) : base(message)
        {
        }
    }
}
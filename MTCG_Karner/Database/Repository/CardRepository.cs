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
                            Damage = reader.GetDouble(2)
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
}
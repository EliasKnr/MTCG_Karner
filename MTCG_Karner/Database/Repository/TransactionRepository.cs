using MTCG_Karner.Database;
using MTCG_Karner.Database.Repository;
using MTCG_Karner.Models;
using Npgsql;

namespace MTCG_Karner.Database.Repository;

public class TransactionRepository
{
    public User AuthenticateUser(string usernameiotoken)
    {
        // ### TOKEN NEEDED ### Replace the following with your actual database logic to authenticate the user by token
         string query = "SELECT * FROM users WHERE username = @username";

        using (var conn = new NpgsqlConnection(DBAccess.ConnectionString))
        using (var cmd = new NpgsqlCommand(query, conn))
        {
            try
            {
                cmd.Parameters.AddWithValue("@username", usernameiotoken);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                        throw new Exception("Authentication failed.");

                    // Assuming your User model includes the token and coins
                    return new User
                    {
                        Id = int.Parse(reader["id"].ToString()),
                        Username = reader["username"].ToString(),
                        Coins = int.Parse(reader["coins"].ToString())
                        // Populate other fields as necessary
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error authenticating user by username: {ex.Message}");
                throw;
            }
        }
    }

    public void DeductCoins(User user, int cost)
    {
        if (user.Coins < cost)
            throw new Exception("Insufficient coins.");

        string updateQuery = "UPDATE users SET coins = coins - @Cost WHERE username = @Username";

        using (var conn = new NpgsqlConnection(DBAccess.ConnectionString))
        using (var cmd = new NpgsqlCommand(updateQuery, conn))
        {
            cmd.Parameters.AddWithValue("@Cost", cost);
            cmd.Parameters.AddWithValue("@Username", user.Username);
            conn.Open();

            if (cmd.ExecuteNonQuery() != 1)
                throw new Exception("Failed to deduct coins.");
        }
    }
}
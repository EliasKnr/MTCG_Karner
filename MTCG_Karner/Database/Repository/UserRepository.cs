using MTCG_Karner.Models;
using Npgsql;

namespace MTCG_Karner.Database.Repository;

public class UserRepository
{
    
    
    public void CreateUser(User user)
    {
        string insertQuery = "INSERT INTO users (username, password) VALUES (@username, @password)";

        using (NpgsqlConnection conn = new NpgsqlConnection(DBAccess.ConnectionString))
        using (NpgsqlCommand cmd = new NpgsqlCommand(insertQuery, conn))
        {
            try
            {
                conn.Open();
                cmd.Parameters.AddWithValue("@username", user.Username);
                cmd.Parameters.AddWithValue("@password", user.Password);
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating user: {ex.Message}");
                throw;
            }
            finally
            {
                conn.Close();
            }
        }
    }

    public User GetUserByUsername(string username)
    {
        User user = null;
        string query = "SELECT * FROM users WHERE username = @username";
        using (NpgsqlConnection conn = new NpgsqlConnection(DBAccess.ConnectionString))
        using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
        {
            try
            {
                conn.Open();
                cmd.Parameters.AddWithValue("@username", username);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        user = new User
                        {
                            Username = reader["username"].ToString(),
                            Password = reader["password"].ToString()
                            // Map other properties as needed
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting user by username: {ex.Message}");
                throw;
            }
            finally
            {
                conn.Close();
            }
        }
        return user;
    }
    
    //Baustelle - Doch in TransactionRepo
    /*
    public User AuthenticateUser(string username)
    {
        User user = null;
        string query = "SELECT * FROM users WHERE username = @username";
        using (NpgsqlConnection conn = new NpgsqlConnection(DBAccess.ConnectionString))
        using (NpgsqlCommand cmd = new NpgsqlCommand(query, conn))
        {
            try
            {
                conn.Open();
                cmd.Parameters.AddWithValue("@username", username);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        user = new User
                        {
                            Username = reader["username"].ToString(),
                            Password = reader["password"].ToString(),
                            Coins = int.Parse(reader["coins"].ToString()),
                            // Map other yow
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error authenticating user by username: {ex.Message}");
                throw;
            }
            finally
            {
                conn.Close();
            }
        }
        return user;
    }
    */

}
using MTCG_Karner.Controller;
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


    public UserDataDTO GetUserData(string username)
    {
        UserDataDTO userData = null;
        string query = "SELECT name, bio, image FROM users WHERE username = @Username";

        using (var conn = new NpgsqlConnection(DBAccess.ConnectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Username", username);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        userData = new UserDataDTO
                        {
                            Name = reader["name"].ToString(),
                            Bio = reader["bio"].ToString(),
                            Image = reader["image"].ToString()
                        };
                    }
                }
            }
        }

        return userData;
    }

    public void UpdateUserData(string username, UserDataDTO updatedUser)
    {
        string query = "UPDATE users SET name = @Name, bio = @Bio, image = @Image WHERE username = @Username";

        using (var conn = new NpgsqlConnection(DBAccess.ConnectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Name", updatedUser.Name);
                cmd.Parameters.AddWithValue("@Bio", updatedUser.Bio);
                cmd.Parameters.AddWithValue("@Image", updatedUser.Image);

                int affectedRows = cmd.ExecuteNonQuery();
                if (affectedRows == 0)
                {
                    throw new UserController.UserNotFoundException($"User with username {username} not found.");
                }
            }
        }
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
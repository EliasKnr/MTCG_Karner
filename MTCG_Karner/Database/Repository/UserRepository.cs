using System.Security.Authentication;
using MTCG_Karner.Controller;
using MTCG_Karner.Models;
using MTCG_Karner.Server;
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


    //GetUserFromAuthHeader
    public User AuthenticateUser(HttpSvrEventArgs e)
    {
        // Check for the presence of the event argument and the headers
        if (e == null || e.Headers == null)
        {
            throw new AuthenticationException("Access token is missing or invalid");
        }

        // Try to find the Authorization header
        string authHeader = e.Headers.FirstOrDefault(h => h.Name.Equals("Authorization"))?.Value;

        // If the Authorization header is not found or does not contain the expected bearer token, throw an exception
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            throw new AuthenticationException("Access token is missing or invalid");
        }

        // Extract the token part after "Bearer "
        string token = authHeader.Substring("Bearer ".Length).Trim();

        // Extract the username part from the token
        string username = token.Replace("-mtcgToken", "");

        if (string.IsNullOrEmpty(username))
        {
            throw new AuthenticationException("Access token is missing or invalid");
        }

        string query = "SELECT * FROM users WHERE username = @Username";

        using (var conn = new NpgsqlConnection(DBAccess.ConnectionString))
        using (var cmd = new NpgsqlCommand(query, conn))
        {
            try
            {
                cmd.Parameters.AddWithValue("@Username", username);
                conn.Open();

                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        throw new AuthenticationException("Authentication failed. User not found.");
                    }

                    return new User
                    {
                        Id = int.Parse(reader["id"].ToString()),
                        Username = reader["username"].ToString(),
                        Coins = int.Parse(reader["coins"].ToString())
                        // Populate other fields as necessary
                    };
                }
            }
            catch (NpgsqlException ex)
            {
                Console.WriteLine($"Database error when authenticating user: {ex.Message}");
                throw;
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"Data format error when reading user data: {ex.Message}");
                throw;
            }
        }
    }


    //for LoginUser
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

    public UserStats GetUserStats(int userId)
    {
        var query = "SELECT wins, losses, games_played FROM users WHERE id = @UserId";
        using (var conn = new NpgsqlConnection(DBAccess.ConnectionString))
        {
            conn.Open();
            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new UserStats
                        {
                            Wins = reader.GetInt32(reader.GetOrdinal("wins")),
                            Losses = reader.GetInt32(reader.GetOrdinal("losses")),
                            GamesPlayed = reader.GetInt32(reader.GetOrdinal("games_played")),
                        };
                    }
                    else
                    {
                        throw new UserController.UserNotFoundException("User not found.");
                    }
                }
            }
        }
    }

}
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

    public void DeleteUser(string username)
    {
        using (NpgsqlConnection connection = new NpgsqlConnection(DBAccess.ConnectionString))
        {
            connection.Open();
            using (NpgsqlCommand command = new NpgsqlCommand())
            {
                command.Connection = connection;
                command.CommandText = "DELETE FROM users WHERE username = @username";

                command.Parameters.AddWithValue("@username", username);

                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    Console.WriteLine("User not found or deleted.");
                }
                else
                {
                    Console.WriteLine("User deleted successfully.");
                }
            }
        }
    }


    //GetUserFromAuthHeader
    public User AuthenticateUser(HttpSvrEventArgs e)
    {
        if (e == null || e.Headers == null)
        {
            throw new AuthenticationException("Access token is missing or invalid");
        }

        string authHeader = e.Headers.FirstOrDefault(h => h.Name.Equals("Authorization"))?.Value;

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            throw new AuthenticationException("Access token is missing or invalid");
        }

        string token = authHeader.Substring("Bearer ".Length).Trim();
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
        var query = "SELECT wins, losses, games_played, elo FROM users WHERE id = @UserId";
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
                        int wins = reader.GetInt32(reader.GetOrdinal("wins"));
                        int losses = reader.GetInt32(reader.GetOrdinal("losses"));
                        int draws = wins + losses;

                        return new UserStats
                        {
                            Wins = wins,
                            Losses = losses,
                            Draws = draws,
                            GamesPlayed = reader.GetInt32(reader.GetOrdinal("games_played")),
                            Elo = reader.GetInt32(reader.GetOrdinal("elo")),
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

    public void UpdateUserStats(int userId, int eloChange, int winsChange, int lossesChange)
    {
        Console.WriteLine("-UpdateUserStats");
        try
        {
            string query = @"
    UPDATE users
    SET elo = CASE
                WHEN elo + @EloChange < 0 THEN 0
                ELSE elo + @EloChange
              END,
        wins = wins + @WinsChange,
        losses = losses + @LossesChange,
        games_played = games_played + 1
    WHERE id = @UserId";

            using (var conn = new NpgsqlConnection(DBAccess.ConnectionString))
            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@EloChange", eloChange);
                cmd.Parameters.AddWithValue("@WinsChange", winsChange);
                cmd.Parameters.AddWithValue("@LossesChange", lossesChange);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating user stats: {ex.Message}");
            throw;
        }

        Console.WriteLine("-B-UpdatedUserStats-" + userId);
    }

    public int GetUserElo(int userId)
    {
        string query = "SELECT elo FROM users WHERE id = @UserId";
        using (var conn = new NpgsqlConnection(DBAccess.ConnectionString))
        using (var cmd = new NpgsqlCommand(query, conn))
        {
            cmd.Parameters.AddWithValue("@UserId", userId);
            conn.Open();
            return (int)cmd.ExecuteScalar();
        }
    }
}
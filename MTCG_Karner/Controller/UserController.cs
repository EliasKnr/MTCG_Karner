using System.Security.Authentication;
using System.Text.RegularExpressions;
using MTCG_Karner.Database.Repository;
using MTCG_Karner.Models;
using MTCG_Karner.Server;
using Newtonsoft.Json;
using Npgsql;

namespace MTCG_Karner.Controller;

public class UserController
{
    private UserRepository _userRepository = new UserRepository();
    private CardRepository _cardRepository = new CardRepository();
    private PackageRepository _packageRepository = new PackageRepository();
    private TransactionRepository _transactionRepository = new TransactionRepository();


    public void CreateUser(HttpSvrEventArgs e)
    {
        var userDto =
            JsonConvert.DeserializeObject<UserDTO>(e
                .Payload); // Assuming you have a DTO that includes username and password

        try
        {
            // Hash the password before saving to the database
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(userDto.Password);
            User user = new User
            {
                Username = userDto.Username,
                Password = hashedPassword
                // Add other fields as necessary
            };

            _userRepository.CreateUser(user);
            e.Reply(201, "User Created");
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            e.Reply(409, "User Already Exists");
        }
    }


    public void LoginUser(HttpSvrEventArgs e)
    {
        var loginRequest = JsonConvert.DeserializeObject<UserDTO>(e.Payload);
        try
        {
            var user = _userRepository.GetUserByUsername(loginRequest.Username);
            if (user != null && VerifyPassword(loginRequest.Password, user.Password))
            {
                var token = GenerateToken(user.Username);
                e.Reply(200, JsonConvert.SerializeObject(new { token = token }));
            }
            else
            {
                e.Reply(401, "Invalid username/password provided");
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine(exception);
            e.Reply(500, "An error occurred during login");
        }
    }

    private bool VerifyPassword(string providedPassword, string storedPassword)
    {
        // Use bcrypt to verify the hashed password
        return BCrypt.Net.BCrypt.Verify(providedPassword, storedPassword);
    }

    private string GenerateToken(string username)
    {
        // Simple token generation
        return $"{username}-mtcgToken";
    }


    public void GetUserCards(HttpSvrEventArgs e)
    {
        try
        {
            var user = _userRepository.AuthenticateUser(e);
            var cards = _cardRepository.GetCardsByUserId(user.Id);

            if (cards.Count == 0)
            {
                e.Reply(204, null);
                return;
            }

            string jsonResponse = JsonConvert.SerializeObject(cards);
            e.Reply(200, jsonResponse);
        }
        catch (AuthenticationException)
        {
            e.Reply(401, "Access token is missing or invalid");
        }
        catch (NpgsqlException ex)
        {
            Console.WriteLine($"Database error: {ex}");
            e.Reply(500, "Internal Server Error: Database operation failed");
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"Data format error: {ex}");
            e.Reply(500, "Internal Server Error: Data format issue");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex}");
            e.Reply(500, "Internal Server Error: Unexpected problem");
        }
    }


    public void GetUser(HttpSvrEventArgs e)
    {
        var username = e.Path.Split('/')[2];
        string authHeader = e.Headers.FirstOrDefault(h => h.Name.Equals("Authorization")).Value;
        string tokenUsername = authHeader?.Split(' ').LastOrDefault()?.Replace("-mtcgToken", "");

        // Verify that the requested username matches the username from the token or is "admin"
        if (tokenUsername != username && tokenUsername != "admin")
        {
            e.Reply(401, "Access token is missing or invalid");
            return;
        }

        try
        {
            var user = _userRepository.GetUserData(username);
            if (user != null)
            {
                e.Reply(200, JsonConvert.SerializeObject(user));
            }
            else
            {
                e.Reply(404, "User not found");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetUser: {ex}");
            e.Reply(500, "Internal Server Error");
        }
    }

    public void UpdateUser(HttpSvrEventArgs e, string username)
    {
        // Fake authentication: extract username from the Authorization header
        string authHeader = e.Headers.FirstOrDefault(h => h.Name.Equals("Authorization")).Value;
        string tokenUsername = authHeader?.Split(' ').LastOrDefault()?.Replace("-mtcgToken", "");

        // Verify that the requested username matches the username from the token or is "admin"
        if (tokenUsername != username && tokenUsername != "admin")
        {
            e.Reply(401, "Access token is missing or invalid");
            return;
        }

        try
        {
            var updatedUserData = JsonConvert.DeserializeObject<UserDataDTO>(e.Payload);
            if (updatedUserData == null)
            {
                e.Reply(400, "Bad request");
                return;
            }

            _userRepository.UpdateUserData(username, updatedUserData);
            e.Reply(200, "User updated successfully");
        }
        catch (UserNotFoundException)
        {
            e.Reply(404, "User not found");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in UpdateUser: {ex}");
            e.Reply(500, "Internal Server Error");
        }
    }

    public void GetStats(HttpSvrEventArgs e)
    {
        try
        {
            var user = _userRepository.AuthenticateUser(e);
            var stats = _userRepository.GetUserStats(user.Id);

            e.Reply(200, JsonConvert.SerializeObject(stats));
        }
        catch (AuthenticationException)
        {
            e.Reply(401, "Access token is missing or invalid");
        }
        catch (NpgsqlException ex)
        {
            Console.WriteLine($"Database error: {ex.Message}");
            e.Reply(500, "Internal Server Error: Database operation failed");
        }
        catch (UserNotFoundException)
        {
            e.Reply(404, "User not found");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving stats: {ex}");
            e.Reply(500, "Internal Server Error: Could not retrieve stats");
        }
    }


    public class UserNotFoundException : Exception
    {
        public UserNotFoundException(string message) : base(message)
        {
        }
    }
}
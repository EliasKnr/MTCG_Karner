using System.Text.RegularExpressions;
using MTCG_Karner.Database.Repository;
using MTCG_Karner.Models;
using MTCG_Karner.Server;
using Newtonsoft.Json;

namespace MTCG_Karner.Controller;

public class UserController
{
    private UserRepository _userRepository = new UserRepository();
    private CardRepository _cardRepository = new CardRepository();
    private PackageRepository _packageRepository = new PackageRepository();
    private TransactionRepository _transactionRepository = new TransactionRepository();


    public void CreateUser(HttpSvrEventArgs e)
    {
        var user = JsonConvert.DeserializeObject<User>(e.Payload);
        Console.WriteLine("-P--: " + e.Payload);
        //Console.WriteLine(user.Password);
        try
        {
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
                // Placeholder for when you implement actual token generation
                var token = GenerateToken(user);
                e.Reply(200, JsonConvert.SerializeObject(new { token = token }));
            }
            else
            {
                e.Reply(401, "Invalid credentials");
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
        // Placeholder for password verification logic
        return providedPassword == storedPassword; // Adjust for actual password verification
    }

    private string GenerateToken(User user)
    {
        // Placeholder for token generation logic
        return "token_placeholder"; // Adjust for actual token generation
    }

    public void GetUserCards(HttpSvrEventArgs e)
    {
        string authHeader = e.Headers.FirstOrDefault(h => h.Name.Equals("Authorization")).Value;
        string token = authHeader?.Split(' ').LastOrDefault();

        // ### TOKEN BITTE
        // Extract username from token, replace with actual token validation later
        string pattern = @"^([^-]+)-mtcgToken$";
        string username = "failed";
        Match match = Regex.Match(token, pattern);
        if (!match.Success)
        {
            e.Reply(401, "Unauthorized: Token is missing or invalid");
            return;
        }

        username = match.Groups[1].Value;

        try
        {
            var user = _transactionRepository.AuthenticateUser(username);
            var cards = _cardRepository.GetCardsByUserId(user.Id);

            if (cards.Count == 0)
            {
                e.Reply(204, null);
                return;
            }

            string jsonResponse = JsonConvert.SerializeObject(cards);
            e.Reply(200, jsonResponse);
        }
        catch (UserNotFoundException)
        {
            e.Reply(404, "User not found");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving cards: {ex}");
            e.Reply(500, "Internal Server Error: Could not retrieve cards");
        }
    }


    public void GetUser(HttpSvrEventArgs e, string username)
    {
        // Fake authentication: extract username from the Authorization header
        string authHeader = e.Headers.FirstOrDefault(h => h.Name.Equals("Authorization")).Value;
        string tokenUsername = authHeader?.Split(' ').LastOrDefault()?.Replace("-mtcgToken", "");

        // Verify that the requested username matches the username from the token or is "admin"
        if (tokenUsername != username && tokenUsername != "admin")
        {
            e.Reply(401, "Unauthorized access");
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
            e.Reply(401, "Unauthorized access");
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


    public class UserNotFoundException : Exception
    {
        public UserNotFoundException(string message) : base(message)
        {
        }
    }
}
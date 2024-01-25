using MTCG_Karner.Database.Repository;
using MTCG_Karner.Models;
using MTCG_Karner.Server;
using Newtonsoft.Json;

namespace MTCG_Karner.Controller;

public class UserController
{
    private UserRepository _userRepository = new UserRepository();
    private PackageRepository _packageRepository = new PackageRepository();
    
    
    public void CreateUser(HttpSvrEventArgs e)
    {
        var user = JsonConvert.DeserializeObject<User>(e.Payload);
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
    
    // ### move to PackageController/Repo
    public void CreatePackage(HttpSvrEventArgs e)
    {
        // Deserialize the JSON body to a list of card objects
        var package = JsonConvert.DeserializeObject<List<Card>>(e.Payload);

        // Call a method in the repository to insert the package into the database
        try
        {
            _packageRepository.CreatePackage(package);
            e.Reply(201, "Package created successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            e.Reply(500, "Internal Server Error: Could not create package");
        }
    }
    
}
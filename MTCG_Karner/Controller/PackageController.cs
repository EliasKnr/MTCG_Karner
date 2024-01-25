using System.Security.Authentication;
using MTCG_Karner.Database.Repository;
using MTCG_Karner.Server;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace MTCG_Karner.Controller;

public class PackageController
{
    private TransactionRepository _transactionRepository = new TransactionRepository();
    private PackageRepository _packageRepository = new PackageRepository();

    public void AcquirePackage(HttpSvrEventArgs e)
    {
        string authHeader = e.Headers.FirstOrDefault(h => h.Name.Equals("Authorization")).Value;
        string token = authHeader?.Split(' ').LastOrDefault();


        //TOKEN BITTE - HIER NOTLÖSUNG MIT USERNAME ### ----------------
        string pattern = @"^([^-]+)-mtcgToken$";
        string username_iotoken = "failed";
        Console.WriteLine("###TOKEN####: " + token);
        Match match = Regex.Match(token, pattern);

        // Check if a match was found
        if (match.Success)
        {
            // Extract and return the captured group
            username_iotoken = match.Groups[1].Value;
            Console.WriteLine("#######" + username_iotoken);
        }
        else
        {
            // Return null or an appropriate value if no match was found
            throw new Exception("REGEX THING FAILED: " + username_iotoken + " /from/ " + token);
            return;
        }
        // -------------------------------------------------------------


        // Authenticate the user
        var user = _transactionRepository.AuthenticateUser(username_iotoken);

        // Deduct coins (assuming a package costs 5 coins, adjust as needed)
        const int packageCost = 5;
        _transactionRepository.DeductCoins(user, packageCost);

        // Assign a package to the user
        try
        {
            _packageRepository.AcquirePackageForUser(user);
            e.Reply(200, "Package acquired successfully");
        }
        catch (AuthenticationException ex)
        {
            e.Reply(401, $"Authentication failed: {ex.Message}");
        }
        catch (InsufficientCoinsException ex)
        {
            e.Reply(402, $"Insufficient coins: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in AcquirePackage: {ex}");
            e.Reply(500, "Internal Server Error: Could not acquire package");
        }
    }
}

public class InsufficientCoinsException : Exception
{
}
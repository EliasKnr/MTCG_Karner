using System.Security.Authentication;
using MTCG_Karner.Database.Repository;
using MTCG_Karner.Server;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using MTCG_Karner.Models;

namespace MTCG_Karner.Controller;

public class PackageController
{
    private TransactionRepository _transactionRepository = new TransactionRepository();
    private UserRepository _userRepository = new UserRepository();
    private PackageRepository _packageRepository = new PackageRepository();

    public void AcquirePackage(HttpSvrEventArgs e)
    {
        try
        {
            var user = _userRepository.AuthenticateUser(e);

            // Check for package availability first without deducting coins
            if (!_packageRepository.IsPackageAvailable())
            {
                e.Reply(404, "No card package available for buying.");
                return;
            }

            // If a package is available, then deduct coins
            const int packageCost = 5;
            if (!_transactionRepository.DeductCoins(user, packageCost))
            {
                e.Reply(403, "Not enough money for buying a card package");
                return;
            }

            // Now that we have checked for package availability and deducted coins, we can acquire the package
            _packageRepository.AcquirePackageForUser(user);
            e.Reply(200, "Package acquired successfully");
        }
        catch (AuthenticationException)
        {
            e.Reply(401, "Access token is missing or invalid");
        }
        catch (NoPackagesAvailableException)
        {
            e.Reply(404, "No card package available for buying.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in AcquirePackage: {ex}");
            e.Reply(500, "Internal Server Error: Could not acquire package");
        }
    }


    public void CreatePackage(HttpSvrEventArgs e)
    {
        try
        {
            var user = _userRepository.AuthenticateUser(e);
            if (user.Username != "admin")
            {
                e.Reply(401, "Unauthorized: Only admin can create packages");
                return;
            }

            // Deserialize the JSON body to a list of card objects
            var package = JsonConvert.DeserializeObject<List<Card>>(e.Payload);
            _packageRepository.CreatePackage(package);
            e.Reply(201, "Package created successfully");
        }
        catch (JsonSerializationException)
        {
            e.Reply(400, "Bad Request: Invalid package format");
        }
        catch (ArgumentException ex)
        {
            e.Reply(400, $"Bad Request: {ex.Message}");
        }
        catch (AuthenticationException ex)
        {
            e.Reply(401, $"Unauthorized: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            e.Reply(500, "Internal Server Error: Could not create package");
        }
    }
}

//### hier so? wo sonst? andere exc?
public class NoPackagesAvailableException : Exception
{
}
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

        string pattern = @"^([^-]+)-mtcgToken$";
        string username_iotoken = "failed";
        Match match = Regex.Match(token, pattern);

        if (match.Success)
        {
            username_iotoken = match.Groups[1].Value;
        }
        else
        {
            e.Reply(401, "Access token is missing or invalid");
            return;
        }

        try
        {
            var user = _transactionRepository.AuthenticateUser(username_iotoken);

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
            e.Reply(401, "Authentication failed.");
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
}

//### hier so? wo sonst? andere exc?
public class NoPackagesAvailableException : Exception
{
}
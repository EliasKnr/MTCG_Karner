using System.Security.Authentication;
using MTCG_Karner.Database.Repository;
using MTCG_Karner.Server;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Npgsql;

namespace MTCG_Karner.Controller;

public class DeckController
{
    private TransactionRepository _transactionRepository = new TransactionRepository();
    private CardRepository _cardRepository = new CardRepository();

    public void GetDeck(HttpSvrEventArgs e)
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
            var deck = _cardRepository.GetDeckByUserId(user.Id);
            Console.WriteLine("### DECK: " + deck);

            if (deck.Count == 0)
            {
                e.Reply(204, null);
            }
            else
            {
                e.Reply(200, JsonConvert.SerializeObject(deck));
            }
        }
        catch (AuthenticationException)
        {
            e.Reply(401, "Authentication failed.");
        }
        catch (NpgsqlException ex)
        {
            Console.WriteLine($"Database error: {ex.Message}");
            e.Reply(500, "Internal Server Error: Database operation failed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
            e.Reply(500, "Internal Server Error");
        }
    }
}
using System.Security.Authentication;
using MTCG_Karner.Database.Repository;
using MTCG_Karner.Server;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using MTCG_Karner.Models;
using Npgsql;

namespace MTCG_Karner.Controller;

public class DeckController
{
    private UserRepository _userRepository = new UserRepository();
    private CardRepository _cardRepository = new CardRepository();

    public void GetDeck(HttpSvrEventArgs e)
    {
        string authHeader = e.Headers.FirstOrDefault(h => h.Name.Equals("Authorization")).Value;

        try
        {
            var user = _userRepository.AuthenticateUser(authHeader);
            var deck = _cardRepository.GetDeckByUserId(user.Id);

            // Extract format parameter from query string
            bool isPlainFormat = false;
            string queryString = e.Path.Contains("?") ? e.Path.Split('?')[1] : "";

            // Split the query string into individual key-value pairs
            string[] queryParams = queryString.Split('&');

            foreach (string param in queryParams)
            {
                string[] keyValue = param.Split('=');
                //Console.WriteLine("-Parameter--: " + keyValue[0]);
                if (keyValue.Length == 2 && keyValue[0] == "format")
                {
                    isPlainFormat = keyValue[1] == "plain";
                    break;
                }
            }


            if (deck.Count == 0)
            {
                e.Reply(204, null);
            }
            else
            {
                var response = isPlainFormat ? FormatDeckAsPlainText(deck) : JsonConvert.SerializeObject(deck);
                e.Reply(200, response);
            }
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
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
            e.Reply(500, "Internal Server Error");
        }
    }

    private string FormatDeckAsPlainText(List<Card> deck)
    {
        // Implement logic to format deck as a plain text string
        return string.Join("\n", deck.Select(card => $"{card.Name} - Damage: {card.Damage}"));
    }


    public void ConfigureDeck(HttpSvrEventArgs e)
    {
        string authHeader = e.Headers.FirstOrDefault(h => h.Name.Equals("Authorization")).Value;

        try
        {
            var user = _userRepository.AuthenticateUser(authHeader);
            var newDeck = JsonConvert.DeserializeObject<List<Guid>>(e.Payload);

            if (newDeck == null || newDeck.Count != 4)
            {
                e.Reply(400, "The provided deck did not include the required amount of cards");
                return;
            }

            _cardRepository.ConfigureDeck(user.Id, newDeck);
            e.Reply(200, "The deck has been successfully configured");
        }
        catch (AuthenticationException)
        {
            e.Reply(401, "Access token is missing or invalid");
        }
        catch (CardRepository.CardOwnershipException)
        {
            e.Reply(403, "At least one of the provided cards does not belong to the user or is not available.");
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
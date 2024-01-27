using System.Security.Authentication;
using MTCG_Karner.Database.Repository;
using MTCG_Karner.Server;
using Npgsql;

namespace MTCG_Karner.Battle;

public class BattleController
{
    private UserRepository _userRepository = new UserRepository();
    private IBattleService _battleService = new BattleService();
    private BattleLobby _battleLobby;

    public BattleController()
    {
        _battleService = new BattleService();
        _battleLobby = BattleLobby.GetInstance(_battleService);
    }


    public void HandleBattleRequest(HttpSvrEventArgs e)
    {
        Console.WriteLine("-B-HandleBattleRequest");
        try
        {
            // Extract the token and authenticate the user
            var user = _userRepository.AuthenticateUser(e);

            // Attempt to join the user to the lobby
            if (_battleLobby.TryJoinLobby(user))
            {
                // If successful, check if a battle can be started
                _battleLobby.CheckForBattle();
            }
            else
            {
                e.Reply(409, "Could not join the lobby"); // or another appropriate status code
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
}
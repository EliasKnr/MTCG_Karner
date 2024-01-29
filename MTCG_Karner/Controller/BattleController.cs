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
            var user = _userRepository.AuthenticateUser(e);

            if (_battleLobby.TryJoinLobby(user))
            {
                _battleLobby.CheckForBattle();
                e.Reply(200,"The battle has been carried out successfully.");
            }
            else
            {
                e.Reply(409, "Could not join the lobby");
            }
        }
        catch(NoValidDeckException ex)
        {
            Console.WriteLine($"Database error: {ex.Message}");
            e.Reply(409, "Could not join the lobby - invalid deck");
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
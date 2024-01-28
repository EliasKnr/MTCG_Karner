using MTCG_Karner.Models;

namespace MTCG_Karner.Battle;

public class BattleLobby
{
    private readonly Queue<User> _waitingUsers = new Queue<User>();
    private readonly object _lock = new object();
    private readonly IBattleService _battleService;
    private static BattleLobby _instance;
    private static readonly object _instanceLock = new object();

    private BattleLobby(IBattleService battleService)
    {
        _battleService = battleService;
    }

    public static BattleLobby GetInstance(IBattleService battleService)
    {
        if (_instance == null)
        {
            lock (_instanceLock)
            {
                if (_instance == null)
                {
                    _instance = new BattleLobby(battleService);
                }
            }
        }

        return _instance;
    }

    public bool TryJoinLobby(User user)
    {
        Console.WriteLine($"-B-{user.Username}(ID:{user.Id})-TryingtoJoinLobby");
        lock (_lock)
        {
            if (_waitingUsers.Any(u => u.Id == user.Id))
            {
                Console.WriteLine($"-B-{user.Username} already in the lobby");
                return false;
            }

            _waitingUsers.Enqueue(user);
            Console.WriteLine($"-B-{user.Username} joined -InLobby: {_waitingUsers.Count}");
            return true;
        }
    }

    public void CheckForBattle()
    {
        Console.WriteLine("-B-CheckForBattle");
        Console.WriteLine("-B-CheckForBattle-InLobby: " + _waitingUsers.Count);
        lock (_lock)
        {
            if (_waitingUsers.Count >= 2) // Assuming a battle requires two users
            {
                Console.WriteLine("-B-CheckForBattle-StartBattle");
                var user1 = _waitingUsers.Dequeue();
                var user2 = _waitingUsers.Dequeue();
                StartBattle(user1, user2);
            }
        }
    }

    private void StartBattle(User user1, User user2)
    {
        Console.WriteLine("-B-StartBattle---/-\\--");
        // Run battle logic in a separate thread to not block the lobby or the HTTP server
        try
        {
            Task.Run(() => _battleService.RunBattle(user1, user2));
        }
        catch (BattleException ex)
        {
            Console.WriteLine($"BattleException: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error during battle: {ex.Message}");
        }
    }
}
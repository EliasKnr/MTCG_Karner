using MTCG_Karner.Database.Repository;
using MTCG_Karner.Models;

namespace MTCG_Karner.Battle;

public interface IBattleService
{
    void RunBattle(User user1, User user2);
}

public class BattleService : IBattleService
{
    private readonly UserRepository _userRepository;

    public BattleService()
    {
        _userRepository = new UserRepository();
    }

    public void RunBattle(User user1, User user2)
    {
        // Implement battle logic based on the rules provided
        // This should be a comprehensive method that handles all aspects of a battle
        // including element types, special rules, and ELO calculations

        // Placeholder for the battle logic
        var winner = new Random().Next(0, 2) == 0 ? user1 : user2; // Randomly pick a winner for now

        // Update stats for both users
        UpdateStats(winner, winner == user1 ? user2 : user1);

        // Log the result of the battle
        LogBattleResult(winner, winner == user1 ? user2 : user1);
    }

    private void UpdateStats(User winner, User loser)
    {
        // Implement the logic to update the stats of the winner and loser
        // This would interact with the UserRepository to update the database
        Console.WriteLine("-B-winner: " + winner.Username);
        Console.WriteLine("-B-looser: " + loser.Username);

        // Example: Increase wins for the winner and losses for the loser
        _userRepository.UpdateUserStats(winner.Id, 3, 1, 0); // +3 ELO for the winner
        _userRepository.UpdateUserStats(loser.Id, -5, 0, 1); // -5 ELO for the loser
    }

    private void LogBattleResult(User winner, User loser)
    {
        Console.WriteLine("-B-Log:");
        // Implement logging of the battle result
        // You could serialize the battle log to JSON, or just a plain string, and then save it to the database or a file
        Console.WriteLine($"Battle finished: {winner.Username} won against {loser.Username}");
    }
}
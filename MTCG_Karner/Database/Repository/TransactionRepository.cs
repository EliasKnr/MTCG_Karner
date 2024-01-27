using MTCG_Karner.Database;
using MTCG_Karner.Database.Repository;
using MTCG_Karner.Models;
using Npgsql;

namespace MTCG_Karner.Database.Repository;

public class TransactionRepository
{
    public bool DeductCoins(User user, int cost)
    {
        if (user.Coins < cost)
        {
            return false;
        }
        else
        {
            string updateQuery = "UPDATE users SET coins = coins - @Cost WHERE username = @Username";

            using (var conn = new NpgsqlConnection(DBAccess.ConnectionString))
            using (var cmd = new NpgsqlCommand(updateQuery, conn))
            {
                cmd.Parameters.AddWithValue("@Cost", cost);
                cmd.Parameters.AddWithValue("@Username", user.Username);
                conn.Open();

                if (cmd.ExecuteNonQuery() != 1)
                    throw new Exception("Failed to deduct coins.");
            }

            return true;
        }
    }
}
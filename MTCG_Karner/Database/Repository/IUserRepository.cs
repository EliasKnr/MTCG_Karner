using MTCG_Karner.Models;
using MTCG_Karner.Server;

namespace MTCG_Karner.Database.Repository;

public interface IUserRepository
{
    // Define methods that are in UserRepository.
    User GetUserByUsername(string username);

    void CreateUser(User user);
    // Add other methods as required.
    User AuthenticateUser(HttpSvrEventArgs httpSvrEventArgs);
    UserDataDTO GetUserData(string username);
    void UpdateUserData(string username, UserDataDTO updatedUserData);
    UserStats GetUserStats(int userID);
    void DeleteUser(string usernameToDelete);
}
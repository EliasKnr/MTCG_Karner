namespace MTCG_Karner.Models;

public class UserDTO
{
    //Userklasse für Client damit nicht direkt Zugriff
    //alles an Client Kopie Klasse (DTO Klasse)
    
    public string Username { get; set; }
    public string Password { get; set; }
    public string Name { get; set; }
    public int Coins { get; set; }
    public int Id { get;  set; }
}
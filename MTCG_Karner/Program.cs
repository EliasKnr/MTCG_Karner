using MTCG_Karner.Controller;
using MTCG_Karner.Server;

namespace MTCG_Karner
{
    internal class Program
    {
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // entry point                                                                                                      //
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        /// <summary>Main entry point.</summary>
        /// <param name="args">Arguments.</param>
        static void Main(string[] args)
        {
            HttpSvr svr = new();
            svr.Incoming += _ProcessMessage;

            svr.Run();
        }


        /// <summary>Event handler for incoming server requests.</summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event arguments.</param>
        private static void _ProcessMessage(object sender, HttpSvrEventArgs e)
        {
            UserController _userController = new UserController();
            PackageController _packageController = new PackageController();
            DeckController _deckController = new DeckController();

            Console.WriteLine("-theWay: " + e.Path);

            if (e.Path.StartsWith("/users") && e.Method.Equals("POST"))
            {
                Console.WriteLine("Contents of e:");
                foreach (var property in e.GetType().GetProperties())
                {
                    var name = property.Name;
                    var value = property.GetValue(e, null);
                    Console.WriteLine($"{name}: {value}");
                }

                _userController.CreateUser(e);
            }
            else if (e.Path.StartsWith("/sessions") && e.Method.Equals("POST"))
            {
                _userController.LoginUser(e);
            }
            else if (e.Path.StartsWith("/packages") && e.Method.Equals("POST"))
            {
                    _packageController.CreatePackage(e);
            }
            else if (e.Path.StartsWith("/transactions/packages") && e.Method.Equals("POST"))
            {
                _packageController.AcquirePackage(e);
            }
            else if (e.Path.Equals("/cards") && e.Method.Equals("GET"))
            {
                _userController.GetUserCards(e);
            }
            else if (e.Path.StartsWith("/deck") && e.Method.Equals("GET"))
            {
                Console.WriteLine("GetDeck");
                _deckController.GetDeck(e);
            }
            else if (e.Path.Equals("/deck") && e.Method.Equals("PUT"))
            {
                Console.WriteLine("-P--: " + e.Payload);
                _deckController.ConfigureDeck(e);
            }
            else if (e.Path.StartsWith("/users/") && e.Method.Equals("GET"))
            {
                _userController.GetUser(e);
            }
            else if (e.Path.StartsWith("/users/") && e.Method.Equals("PUT"))
            {
                var username = e.Path.Split('/')[2];
                _userController.UpdateUser(e, username);
            }


            //Console.WriteLine(e.PlainMessage);
            //e.Reply(200, "Yo! Understood.");
        }
    }
}
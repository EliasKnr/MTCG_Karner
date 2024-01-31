UNIT TESTS:

CreateUser_AddsNewUser_WhenUserDataIsValid:             Überprüft, ob ein neuer Benutzer korrekt erstellt wird, wenn Benutzerdaten gültig sind
GetUserByUsername_ReturnsUser_WhenUserExists:           Stellt sicher, dass ein Benutzer korrekt abgerufen wird, wenn er existiert
GetUserByUsername_ReturnsNull_WhenUserDoesNotExist:     Ob null zurückgegeben wird, wenn ein Benutzername nicht existiert
AuthenticateUser_ReturnsUser_WhenCredentialsAreValid:   Überprüft Authentifizierung eines Benutzers mit gültigen Anmeldeinformationen
DeleteUser_RemovesUser_WhenUserExists:                  Testet, ob ein Benutzer korrekt gelöscht wird, wenn er existiert
UpdateUserData_UpdatesUser_WhenDataIsValid:             Überprüft Aktualisieren von Benutzerdaten, wenn neue Daten gültig
GetUserStats_ReturnsCorrectStats_WhenUserExists:        Korrekte Benutzerstatistiken, wenn Benutzer existiert
CreatePackage_SuccessfullyCreatesPackage:               Testet erfolgreiche Erstellen eines Pakets von Spielkarten
AcquirePackageForUser_UserAcquiresPackageSuccessfully:  Überprüft, ob ein Benutzer erfolgreich ein Paket erwerben kann
IsPackageAvailable_ReturnsTrueWhenPackagesExist:        Stellt sicher, dass true zurückgegeben wird, wenn Pakete (min 1) verfügbar sind
GetDeck_ReturnsUserDeck_WhenUserExists:                 Dass das Kartendeck eines Benutzers korrekt abgerufen wird, wenn Benutzer existiert
ConfigureDeck_UpdatesUserDeck_WhenDeckIsValid:          Testet Aktualisieren des Benutzerdecks, wenn neues Deck gültig
AddCardsToDeck_AddsCards_WhenDeckIsNotFull:              Karten können zum Deck hinzugefügt werden, solange Deck nicht voll ist
RemoveCardFromDeck_RemovesCard_WhenCardIsInDeck:        Überprüft Entfernen einer Karte aus dem Deck, wenn sie sich im Deck befindet
TransferCardOwnership_ChangesCardOwner_WhenUserExists:  Testet Übertragen des Besitzes einer Karte an einen an Winner
BattleRequest_JoinsLobbySuccessfully_WhenUserHasValidDeck: Ob ein Benutzer erfolgreich Lobby beitreten kann, wenn Deck gültig
BattleRequest_FailsToJoinLobby_WhenUserHasInvalidDeck:  Ob Beitritt Lobby fehlschlägt, wenn Deck ungültig
StartBattle_RunsSuccessfully_WhenTwoUsersInLobby:       Ein Kampf beginnt, wenn zwei Benutzer in Lobby
BattleRound_ResultIsDraw_WhenEqualDamage:               Überprüft, ob Ergebnis Kampf unentschieden, wenn beide Karten den gleichen Schaden haben
UpdateStats_ChangesEloAndStatsAfterBattle:              Testet Aktualisierung von ELO-Werten und Statistiken nach Kampf
BattleRound_AppliesSpecialAbilities:                    Ob spezielle Fähigkeiten von Karten während eines Kampfes angewendet werden
Test2:                                                  Grundlegender Test, sollte immer bestehen (test ob NUnit geht)


Challenges and Solutions:

Handling HTTP Requests: Struggle initially with managing HTTP requests in the UserController.
Mocking Repositories: Ran into issues mocking the UserRepository for testing. 
Overcame this by introducing an interface, removing it again and writing a DBTestUtility.
Setting Up NUnit Tests: Setting up the test environment was tricky but managed to get it working after some trial and error.


Unit Testing Focus:

Concentrated on essential functions like user login, package handling, and battle logic.
Chose tests that covered both successful operations and potential sources of failure in the code.

Reasons for Chosen Tests:

User Login and Registration: These tests were needed to ensure the security of user accounts.
Package Transactions: To confirm that users could buy and create packages correctly.
Battle Logic: Tested to guarantee the main game mechanics were functioning as needed.
Security Concerns: Included tests to safeguard against SQL injections, keeping data safe.

Criticality of Tested Code:

These tests were crucial for making sure the main features of our card game worked without issues.
They helped catch bugs early, which I learned is super important in software development.
Time Management:

Planning and Design: 2h
Coding and Debugging: 35h
Testing and Refinement: 9h
Total Time: 46h

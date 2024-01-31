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
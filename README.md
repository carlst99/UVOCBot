# UVOCBot

![Stable Release](https://github.com/carlst99/UVOCBot/workflows/Stable%20Release/badge.svg)

Provides various functions to assist with the experience of the UVOC outfit Discord server. Current features include:

- Tweet relaying - Posts tweets from Twitter users into a Discord channel
- Reaction-based role assignment
- Bulk voice channel movement
- Temporary group creation (for use with movement commands)
- Random team generation
- PlanetSide 2 server population and status querying
- Various other features, such as coinflips and bonking people :smirk:

To my knowledge, there isn't a publicly hosted instance of UVOCBot available. Hence if you'd like to use it in your own server, you'll have to host your own, or find someone kind enough to do it for you. See [Setup](#Setup) for more info.

# Setup for Hosting

Before continuing, you should note that UVOCBot is designed with a linux system in mind; it has support for systemd and I only provide linux binaries. If you'd like support for Windows services and binaries, please open an Issue.

1. Create a new application in the Discord Developer portal - https://discord.com/developers/applications. Give it a name and icon and **add a bot** to the application.
    1. :warning: Ensure that you enable the `Presence Intent` and `Server Members Intent`, found under the Bot tab of your application.
2. Head on over to the wiki page [Hosting on Linux](https://github.com/carlst99/UVOCBot/wiki/Hosting-on-Linux) and follow the instructions there
3. Invite the bot to your server using [https://discord.com/api/oauth2/authorize?client_id=<YOUR_CLIENT_ID>&permissions=2435927120&scope=bot](https://discord.com/api/oauth2/authorize?client_id=<YOUR_CLIENT_ID>&permissions=2435927120&scope=bot)

# Building and Developing

1. Install the [.NET 5 SDK](https://dotnet.microsoft.com/download/dotnet/5.0).
2. Install [MariaDB](https://mariadb.org/) and create a database.
3. Modify the requisite `appsettings.json` files to include your API keys and database connection string
4. Update the database to the latest migration. If you are using the .NET Core CLI, run the command:
    ```
    dotnet ef database update
    ```

    If you are using the Visual Studio Package Manager, run the command:
    ```
    Update-Database
    ```

6. If you're building with Visual Studio I recommend utilising the *Multiple Startup* feature so that you can easily debug both projects

### Project Structure

UVOCBot has two main components:
- The bot itself, `UVOCBot`. This is a .NET 5 Worker project, based on the `Microsoft.Extensions.Hosting` framework.
- A RESTful API that abstracts the MariaDB database, `UVOCBot.Api`. An `ASP.NET Core 5` project utilising `Entity Framework Core`. This is by and large a data abstraction layer; very little data manipulation occurs here, except for where it would be inefficient for the client to perform non-user-dependent actions.

There is also a shared project, `UVOCBot.Core`, which contains models common to both projects.

# Acknowledgements

UVOCBot is built on the following amazing libraries and frameworks:

- [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/)
- [DbgCensus](https://github.com/carlst99/DbgCensus)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [FuzzySharp](https://github.com/JakeBayer/FuzzySharp)
- [Pomelo.EntityFrameworkCore.MySql](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql)
- [RestSharp](https://restsharp.dev)
- [Remora.Discord](https://github.com/Nihlus/Remora.Discord)
- [Serilog](https://github.com/serilog/serilog)
- [System.IO.Abstractions](https://github.com/System-IO-Abstractions/System.IO.Abstractions)
- [Tweetinvi](https://github.com/linvi/tweetinvi)

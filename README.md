# UVOCBot

![Stable Release](https://github.com/carlst99/UVOCBot/workflows/Stable%20Release/badge.svg)

Provides various functions to assist with the experience of the UVOC outfit Discord server. Current features include:

- Tweet relaying - Posts tweets from Twitter users into Discord channel
- Random team generation
- Reaction-based role assignment
- PlanetSide 2 server population and status querying
- Various other features, such as coinflips and bonking people :smirk:

To my knowledge, there isn't a publicly hosted instance of UVOCBot available. Hence, you'll have to host your own, or find someone kind enough to do it for you. See [Setup](#Setup) for more info.

# Setup for Hosting

Before continuing, you should note that UVOCBot is designed with a linux system in mind; it has support for systemd and I only provide linux binaries. If you'd like support for Windows services and binaries, please open an Issue.

1. Create a new application in the Discord Developer portal - https://discord.com/developers/applications. Give it a name and icon and **add a bot** to the application.
    1. :warning: Ensure that you enable the `Presence Intent` and `Server Members Intent`, found under the Bot tab of your application.
2. Head on over to the wiki page [Hosting on Linux](https://github.com/carlst99/UVOCBot/wiki/Hosting-on-Linux) and follow the instructions there

# Building and Developing

1. Install the [.NET 5 SDK](https://dotnet.microsoft.com/download/dotnet/5.0) installed.
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

6. If you're building with Visual Studio and will be working on a feature that interacts with the RESTful API, I recommend utilising the *Multiple Startup* feature so that you can easily debug both projects

### Project Structure

UVOCBot has two main components:
- The bot itself, `UVOCBot`. This is a .NET 5 Worker project, based on the `Microsoft.Extensions.Hosting` framework.
- A RESTful API that abstracts the MariaDB database, `UVOCBot.Api`. This is built using `ASP.NET Core` and `Entity Framework Core`. This is simply a data abstraction layer; no data manipulation should occur here.

There is also a shared project, `UVOCBot.Core`, which contains models common to both projects.

# Acknowledgements

UVOCBot is built on the following amazing libraries and frameworks:

- [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/)
- [DaybreakGames.Census](https://github.com/Lampjaw/DaybreakGames.Census)
- [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [Pomelo.EntityFrameworkCore.MySql](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql)
- [Refit](https://github.com/reactiveui/refit)
- [Serilog](https://github.com/serilog/serilog)
- [System.IO.Abstractions](https://github.com/System-IO-Abstractions/System.IO.Abstractions)
- [Tweetinvi](https://github.com/linvi/tweetinvi)

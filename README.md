# UVOCBot

![.NET Core](https://github.com/carlst99/UVOCBot/workflows/.NET%20Core/badge.svg)

Provides various functions to assist with the Planetside experience of the UVOC outfit Discord server. Current features include:

- Tweet relaying - Posts tweets from Twitter users into Discord channel
- Random team generation
- Reaction-based role assignment

To my knowledge, there isn't a publicly hosting instance of UVOCBot available. Hence, you'll have to host your own, or find someone kind enough to do it for you. See [Setup](#Setup) for more info.

# Setup

:warning: I'm just scaffolding this information, so as such some of it is fairly light on detail and other bits are downright invalid - for example I don't yet have binaries available, you'll have to build and publish from source. You can do that with the dotnet-5 SDK.

Before continuing, you should note that UVOCBot is designed with a linux system in mind; it has support for systemd and I only provide linux binaries. If you'd like support for Windows services and binaries, please open an Issue.

1. Create a new application in the Discord Developer portal - https://discord.com/developers/applications. Give it a name and icon, and most importantly, **add a bot** to the application. Ensure that you enable the `Presence Intent` and `Server Members Intent`, found under the Bot tab of your application.
2. Install both the [.NET 5 Runtime and ASP.NET Core 5](https://dotnet.microsoft.com/download/dotnet/5.0) runtime packages. If this fails to work try installing the SDK instead.
3. Install and setup either [MariaDB](https://mariadb.org/) or [MySQL](https://www.mysql.com). **It is recommended you create a low-privilege user** for the API to connect with. Create a new database for UVOCBot, and grant the respective user access to it.
3. Download the latest binaries from releases. You'll need both `UVOCBot` and `UVOCBot.Api`. Included in the release are startup scripts, a SQL migration script, and optional systemd service files.
4. Modify the startup scripts by placing your various tokens and settings in the respective places. Additionally, if you'll be using the systemd service files, modify them to point towards the startup scripts.
5. Apply the migration script to the database. Instructions can be found in the wiki page for [Updating a Hosted Instance](https://github.com/carlst99/UVOCBot/wiki/Updating-a-Hosted-Instance).
6. If you'll be managing the UVOCBot components with systemd, place the service files in `/etc/systemd/system/` and enable them.
7. Done! Run UVOCBot using either `systemctl` or the startup scripts. Ensure that you start the API service before running UVOCBot.

# Building and Developing

1. Install the [.NET 5 SDK](https://dotnet.microsoft.com/download/dotnet/5.0) installed.
2. Install [MariaDB](https://mariadb.org/) or [MySQL](https://www.mysql.com). Create a database.
3. In the `UVOCBot.Api.BotContext` class, customise the database connection string to suit your setup.
4. Update the database to the latest migration. If you are using the .NET Core CLI, run the `dotnet ef database update` command. If you are using the Visual Studio Package Manager, run the `Update-Database` command.
5. Set the required environment variables (you can find them in the startup scripts; `StartUVOCBot.sh` and `StartUVOCBotAPI.sh`). I recommend doing this through the `launchSettings.json` file.
6. If you're building with Visual Studio and will be working on a feature that interacts with the RESTful API, I recommend utilising the Multiple Startup feature.

### Project Structure

UVOCBot has two main components:
- The bot itself, `UVOCBot`. This is a .NET 5 Worker project, based on the `Microsoft.Extensions.Hosting` framework.
- A RESTful API that abstracts the MySQL database, `UVOCBot.Api`. This is built using `ASP.NET Core` and `Entity Framework Core`. This is simply a data abstraction layer; no data manipulation should occur here.

There is also a shared project, `UVOCBot.Core`, which contains models common to both projects.

# Acknowledgements

UVOCBot is built on the following amazing libraries and frameworks:

- [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/)
- [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [Pomelo.EntityFrameworkCore.MySql](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql)
- [Refit](https://github.com/reactiveui/refit)
- [Serilog](https://github.com/serilog/serilog)
- [System.IO.Abstractions](https://github.com/System-IO-Abstractions/System.IO.Abstractions)
- [Tweetinvi](https://github.com/linvi/tweetinvi)

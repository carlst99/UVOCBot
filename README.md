# UVOCBot

![.NET Core](https://github.com/carlst99/UVOCBot/workflows/.NET%20Core/badge.svg)

Provides various functions to assist with the Planetside experience of the UVOC outfit Discord server. Current features include:

- Tweet relaying - Posts tweets made by Twitter users of your selection into the Discord

# Setup

:warning: I'm just scaffolding this information, so as such some of it is fairly light on detail and other bits are downright invalid - for example I don't yet have binaries available, you'll have to build and publish from source. You can do that with the dotnet-5 SDK.

To my knowledge, there isn't a publicly hosted copy of UVOCBot available. You should note that UVOCBot is designed with a linux system in mind; it has support for systemd and I only provide linux binaries. If you'd like support for Windows services and binaries, open an Issue. Here's the steps you should take to get UVOCBot running:

1. Create a new application in the Discord Developer portal - https://discord.com/developers/applications. Give it a name and icon, and most importantly, **add a bot** to the application. Ensure that you enable the `Presence Intent` and `Server Members Intent`, found under the Bot tab of your application
2. Install [.NET 5](https://dotnet.microsoft.com/download/dotnet/5.0) on your system. You should only need the Runtime package, but if this fails to work try with the SDK installed.
3. Install and setup either MySQL or MariaDB. **It is recommended you create a low-privilege user**. Create a new database for UVOCBot, and grant the respective user access to it. (Instructions coming!)
3. Download the latest binary from releases. Included in the release is a startup script, and an optional systemd service file. (Instructions coming!)
4. Modify the startup script by placing your various tokens and settings in the respective places. The script explains it.
5. Done! Startup UVOCBot through `systemctl` or by invoking the startup script.

# Acknowledgements

UVOCBot is built on the following amazing libraries:

- [DSharpPlus](https://github.com/DSharpPlus/DSharpPlus)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [Pomelo.EntityFrameworkCore.MySql](https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql)
- [Serilog](https://serilog.net/)
- [Tweetinvi](https://github.com/linvi/tweetinvi)

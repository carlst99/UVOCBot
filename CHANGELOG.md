# Changelog

## Release vNext

**Additions**
- Added the `outfit-wars registrations` command.
- Added the `outfit-wars matches` command.
- Added the `rolemenu list-menus` command.
- Spiffed up the role menu commands, and added support for emojis on role labels.
- Base capture notifications now show the amount of outfit resource earned.

**Fixes**
- Fixed Oshur status being calculated incorrectly.
- Fixed the character name autocomplete on the `character` command failing when not using all-lowercase letters.

**Miscellaneous**
- The response of the `online-friends` command is now only visible to the user.
- The client update tracking message now says '...update has been released' rather than 'detected' when updates are released.

## Release v1.5.0

**Additions**
- BREAKING CHANGE: The **Alternate Role** feature of the greeting message has been refactored
  to allow up to five 'rolesets' to be added. These rolesets are exclusive of each other, such
  that if a user selects one and then another, they'll lose any roles granted by the first set.
  This change means that you'll need to re-setup any alternate roles you wish to offer, by using
  the `add-alternate-roleset` and `delete-alternate-rolesets` commands.

**Fixes**
- *Actually* fixed the `population` and `status` commands failing to respond.

## Release v1.4.1

- Fixed the `population` and `status` commands failing to respond.
- Updated in-app release notes.

## Release v1.4.0

**Additions**
- Added the `online-friends` command.
- Base capture notifications now show any outfit members involved in the capture.
- The `character` command now shows a character's most used weapon, rather than their 'favourite' weapon.

**Fixes**
- Fixed issues with Oshur map data, thanks to [Honu's](https://wt.honu.pw) facility information.
- Fixed UVOCBot overwriting roles applied by other bots when a new member joins.
- Fixed the `online` command for non-Connery outfits.

**Miscellaneous**
- Improved overall stability of map status.
- Decreased the patch check interval to five minutes.

## Release v1.3.1

- Added the awarded resource type, and the faction a base was captured from, to base capture notifications.
- Added a new feed for notifications about updates to the live and PTS clients.
- Fixed the bot failing to respond when running certain commands.

## Release v1.3.0

- Added a basic character info command - `/character`.
- Added more info to the `GuildMemberLeave` admin log.
- Added a configuration option to disable twitter feeds.
- Added the `outfit list-tracked` command, to view the outfits that are being tracked by the guild.
- Fixed occasional failures to respond when running the population command.
- Fixed the formatting of continent status embeds on mobile.

## Release v1.2.2

- Improved the manner in which map region data is retrieved. One should see less internal errors now.
- Removed Message Content intent, which was unneeded.

## Release v1.2.1

- Fixed a bug causing the Census query semaphore to be exhausted. In other words; PlanetSide queries will once again work regardless of uptime.

## Release v1.2.0

- Unlocked continents are now shown in the `population` command.
- Fixed Oshur alert durations.
- Fixed map caching. The `status` command should be more reliable.
- Service ID configuration has changed. See the updated `appsettings.json`.

## Release v1.1.0

- Made many commands faster.
- Updated the `population` command to bundle NS characters onto their respective faction.
- Updated the `population` command to show current open continents on the requested server.
- Updated the `rolemenu create` and `rolemenu edit` commands to utilise modals for setting the title and description.
- Updated the *forum* feeds to include the first discovered image in the post.
- Disabled text commands
- Fixed a couple of bugs in `online` and `status` commands.

## Release v1.0.2

- Added PlanetSide forum post relaying.
- Removed the ForumPTSPatchNotes feed; it was never valid in the first place.
- Fixed permission requirements for toggling feeds.

## Release v1.0.1 - 04/02/2022

- Improved logging and slash command initialization.

## Release v1.0.0 - 04/02/2022

**Breaking Changes**

- Tweet relaying has been completely revamped into `feeds`. Feed relaying will need to be reconfigured.
- A database migration is required.
- The `TwitterOptions` config section has been renamed to `FeedsPluginOptions`, and the properties appended with `Twitter`.
- The API application has been completely removed!

## Release v0.5.2

- The `status` command is now much faster and shows active alerts.
- Added channel selection restrictions to relevant commands. E.g. you no longer have to filter through text channels when trying to use the `move` command between voice channels.
- Made a general sweep to improve stability and error feedback.
- Added the `Tutorial2` map.

**Technical**

- :warning: Updated to .NET 6.0
- Began refactoring the design of the bot. Individual components will, going forward, be placed into plugins which can be freely swapped in/out to customise UVOCBot's feature-set.
    - The Planetside components are the first part of the bot to be plugin-ified.
- :warning: Updated the map assets. Ensure you update them in your installation.
- :warning: Updated the `appsettings.json` files. Ensure you update them in your installation.

## Release v0.4.2 - 14/09/2021

- Added base capture tracking for specified outfits

:warning: A database update is required, and a new object has been added to `appsettings.json`.

## Release v0.4.1 - 04/09/2021

A servicing release.

- Fixed the PlanetSide status command.
- Improved the `online` command.
- Removed the `GuildSettings` database object and updated the `MemberGroup` database object - a database migration is required.
- Removed the `PlanetsideSettings` API endpoint.
- Cleaned up existing code and removed unused code.
- Removed our custom interaction responder and ephemeral system in favour of the recent Remora.Discord additions.

## Release v0.4.0 - 19/08/2021

**New features:**
- Modified the `welcome-message message` command to accept an existing message which it will replicate. This makes it much easier to use detailed formatting in your welcome messages.
- Added role menus!
- Added basic admin logging for member join/leave.
- Added ephemeral responses.

:warning: **You will need to reset the default roles and alternate roles on your welcome message.**

**Bug fixes:**
- Fixed a critical bug with the *Welcome Message* feature, that prevented the role and nickname buttons from working.
- Fixed a critical bug with permission checks that could result in UVOCBot accepting or rejecting actions that it otherwise wouldn't have.

**Other changes:**
- Moved EF Core models and contexts to `UVOCBot.Core`.
- Refactored `MessageResponseHelpers` into the `ReplyService`.
- Disabled the `status` command due to incorrect behaviour.

## Release v0.3.2 - 17/07/2021

**New Feature**
Use the `status` command to check the territory control and status of a world and it's continents.

**Other Changes**
Made the `population` command faster (by removing the world status check).

## Release v0.3.1 - 11/07/2021

**New Feature**
Use the `timestamp` command to get a snippet you can use to insert localised datetimes into messages.

**Other changes**
- Minor bug fixes and code improvements

## Release v0.3.0 - 19/06/2021

**New Feature**
This update adds the welcome message feature! This is a primarily a message sent to new guild members, but also includes the ability to assign default roles, let the new member pick alternate roles (e.g. as a friend of the outfit) and makes guesses as to their in-game nickname, based on the most recent in-game joins.

**Other changes**

- Updates map assets.
- Migrate from `Daybreakgames.Census` to `DbgCensus`.
- Update `appsettings.json` **Please update your copy**.
- Updates to the database model. **Please perform a migration**. There should be no loss of required data.

Significant updates to how interactions are handled have also been made (see `CommandInteractionResponder` and `ComponentInteractionResponder`), along with new command conditions (see `RequireGuildPermissionAttribute` and `RequireContextAttribute`). Finally, the `PermissionChecksService` has been updated with a new method (`CanManipulateRoles`).

## Release v0.2.2 - 27/05/2021

- Added the `map` command! Quickly grab an image of any of PlanetSide 2's continent maps. 
- Fixed a bug where a twitter user could not be added to the relaying system.

Technical Notes:
- Namespace `UVOCBot.Models.Planetside` -> `UVOCBot.Models.Census`.
- Updated dependencies.
- Fixed last tweet ID not being saved.

## Release v0.2.1 - 07/05/2021

This is primarily a servicing release that fixes some bugs, improves internal logic and furthers the UX.

:warning: The license has been changed to the **AGPL-3.0**, in order to comply with the license of *Remora.Discord*.

- The `online` command was added, letting users get the number of online members for a PlanetSide 2 outfit/s.

Technical Notes:
- Migrated the API services from *Refit* to *RestSharp*.
- `FisuApiService` now caches population data for five minutes.
- Add `BotConstants` class, containing values for the bot's Discord App/User IDs and default embed colour.
- Added support for sending logs to a Seq endpoint.
- The API route for getting guild twitter settings now only returns settings for guilds that both have relaying enabled and users added to relay from, when filtering by enabled status.

Migration Notes:
- The database model has changed.
- `AppSettings.json` has changed.
- The systemd service file for `UVOCBot` has been changed to require `UVOCBot.Api`, and start after it.

## Release v0.2.0 - 12/04/2021

- **Slash Commands :tada:** - Everyone hates having to use `help` every five seconds to remember how to use each command. So I removed it, then set it on :fire: for good measure. Now, you can use Discord's new slash commands with UVOCBot! Rejoice!
- **Bug Fixes** - Teased out a few edge cases and fixed many annoyances

See [#42](https://github.com/carlst99/UVOCBot/pull/44) for more info.

## Release v0.1.2 - 27/03/2021

- Fix a NullReferenceException that occurs when using the `bonk` command on a member not currently in a voice channel
- Hide the bonk command
- Remove the exception testing command
- Fix the `move` commands to reliably move *everyone* in the channel
- Improve the `population` command to include total and NS pop values, along with attribution to https://ps2.fisu.pw

## Release v0.1.1 - 14/03/2021

- Implemented a member grouping system for guilds. The following commands are utilised:
    - `create`: Creates a new group from the given members
    - `delete`: Deletes a group
    - `info`: Gets information about a group
    - `list`: Gets all of the groups created in this guild
- Implemented bulk member movement between voice chats. All members in the channel, or member groups, can be shifted, using the `move` and `move-group` commands.
- Improve information discovery in the `help` and `version` (now `about`) commands
- Improve bonk command permissions checks

Technical Notes:
- Added a `CleanupWorker` to the API project. Currently, this removes expired member groups every 15m, but it could be expanded to perform significantly more work that would be more efficient to perform locally, rather than transferring data to the client.

## Release v0.1.0 - 12/02/2021

This is the first generally available release, as I am finally happy with the architecture and design patterns being utilised.

The following commands/functionalities are included:
- Full Tweet relaying functionality, and associated commands, have been implemented
- Random team generation commands have been implemented
- Role management commands:
    - Assign role to all who have placed a specific reaction on a message
    - Remove a role from all who have it
- PlanetSide 2 server population and status querying commands have been implemented, using Census + http://ps2.fisu.pw population API
- Custom prefixes can be set

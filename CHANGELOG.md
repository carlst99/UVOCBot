# Changelog

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
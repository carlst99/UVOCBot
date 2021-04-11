# Changelog

## Release v0.2.0 - 12/04/2021

- **Slash Commands :tada:** - Everyone hates having to use `help` every five seconds to remember how to use each command. So I removed it, then set it on :fire: for good measure. Now, you can use Discord's new slash commands with UVOCBot! Rejoice!
- **Bug Fixes** - Teased out a few edge cases and fixed many annoyances

See (#42)[https://github.com/carlst99/UVOCBot/pull/44] for more info.

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
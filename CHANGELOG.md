# Changelog

## Release X - XXX

- Implemented a member grouping system for guilds. The following commands are utilised:
    - `create`: Creates a new group from the given members
    - `delete`: Deletes a group
    - `info`: Gets information about a group
    - `list`: Gets all of the groups created in this guild

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
- PlanetSide 2 server population and status querying commands have been implemented, using Census + ps2.fisu.pw population API
- Custom prefixes can be set
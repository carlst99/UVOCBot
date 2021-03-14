using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UVOCBot.Services;

namespace UVOCBot.Utils
{
    public class CustomHelpFormatter : BaseHelpFormatter
    {
        protected readonly string _prefix;

        protected DiscordEmbedBuilder _embedBuilder;
        protected StringBuilder _builder;
        protected Command _command;

        public CustomHelpFormatter(CommandContext ctx, IPrefixService prefixService)
            : base(ctx)
        {
            _embedBuilder = new DiscordEmbedBuilder
            {
                Color = Program.DEFAULT_EMBED_COLOUR,
                Title = "Help",
                Url = "https://github.com/carlst99/UVOCBot"
            };

            _builder = new StringBuilder();
            _prefix = prefixService.GetPrefix(ctx.Guild.Id);
        }

        public override CommandHelpMessage Build()
        {
            if (_command == null)
                _embedBuilder.WithDescription("Listing all top-level groups and commands. Specify a group/command to see more information.");

            return new CommandHelpMessage(embed: _embedBuilder.Build());
        }

        public override BaseHelpFormatter WithCommand(Command command)
        {
            _command = command;

            _embedBuilder.WithDescription($"{Formatter.InlineCode(command.Name)}: {command.Description ?? "No description provided."}");

            if (command is CommandGroup cgroup && cgroup.IsExecutableWithoutSubcommands)
                _embedBuilder.WithDescription($"{_embedBuilder.Description}\n\nThis group can be executed as a standalone command.");

            if (command.Aliases?.Any() == true)
                _embedBuilder.AddField("Aliases", string.Join(", ", command.Aliases.Select(Formatter.InlineCode)), false);

            if (command.Overloads?.Any() == true)
            {
                foreach (var ovl in command.Overloads.OrderByDescending(x => x.Priority))
                {
                    _builder.Append(Formatter.Bold(" ")).Append('\n').Append(command.QualifiedName);

                    foreach (var arg in ovl.Arguments)
                        _builder.Append(arg.IsOptional || arg.IsCatchAll ? " [" : " <").Append(arg.Name).Append(arg.IsCatchAll ? "..." : "").Append(arg.IsOptional || arg.IsCatchAll ? ']' : '>');

                    string title = _builder.ToString();
                    _builder.Clear();

                    foreach (var arg in ovl.Arguments)
                        _builder.Append('`').Append(arg.Name).Append(" (").Append(CommandsNext.GetUserFriendlyTypeName(arg.Type)).Append(")`: ").Append(arg.Description ?? "No description provided.").Append('\n');

                    _embedBuilder.AddField(title, _builder.ToString());
                    _builder.Clear();
                }
            }

            _builder.Clear();
            return this;
        }

        public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> subcommands)
        {
            if (_command is null)
            {
                _embedBuilder.AddField("Prefix", Formatter.InlineCode(_prefix));
                AddCommands(subcommands.Where(c => c is CommandGroup), "Command Groups");
                AddCommands(subcommands.Where(c => c is not CommandGroup), "Top-level Commands");
            }
            else
            {
                AddCommands(subcommands, "Subcommands");
            }

            return this;
        }

        private void AddCommands(IEnumerable<Command> commands, string groupName)
        {
            foreach (Command c in commands)
                _builder.Append(Formatter.InlineCode(c.Name)).Append(": ").AppendLine(c.Description ?? "No description provided.");

            if (!commands.Any())
                _embedBuilder.AddField(groupName, "None");
            else
                _embedBuilder.AddField(groupName, _builder.ToString());
            _builder.Clear();
        }
    }
}

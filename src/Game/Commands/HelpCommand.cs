using System.Collections.Generic;
using System.Linq;
using System.Text;
using Netsphere.Network;

namespace Netsphere.Commands
{
    // the console answered "Unknown command" to everything it did not know and there was no way
    // to find out what it did know, short of reading the source
    internal class HelpCommand : ICommand
    {
        public HelpCommand()
        {
            Name = "help";
            AllowConsole = true;
            Permission = SecurityLevel.User;
            SubCommands = new ICommand[0];
        }

        public string Name { get; }
        public bool AllowConsole { get; }
        public SecurityLevel Permission { get; }
        public IReadOnlyList<ICommand> SubCommands { get; }

        public bool Execute(GameServer server, Player plr, string[] args)
        {
            var level = plr?.Account.SecurityLevel ?? SecurityLevel.Developer;

            // one line each: the console window is a single line tall and the long list ran off
            // the right edge of the screen
            foreach (var cmd in server.CommandManager.Commands.Where(c => level >= c.Permission))
            {
                var text = new StringBuilder(cmd.Name);

                var subs = cmd.SubCommands.Where(c => level >= c.Permission).Select(c => c.Name).ToArray();
                if (subs.Length > 0)
                    text.Append(" [" + string.Join(" | ", subs) + "]");

                if (plr == null)
                    System.Console.WriteLine(text.ToString());
                else
                    plr.SendConsoleMessage(S4Color.Green + text.ToString());
            }

            return true;
        }

        public string Help()
        {
            return Name;
        }
    }
}

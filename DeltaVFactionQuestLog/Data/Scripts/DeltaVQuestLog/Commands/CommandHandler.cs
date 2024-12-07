using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using VRage.Utils;

namespace Invalid.DeltaVQuestLog.Commands
{
    /// <summary>
    ///     Parses commands from chat and triggers relevant methods.
    /// </summary>
    public class CommandHandler
    {
        public static CommandHandler I;

        private readonly Dictionary<string, Command> _commands = new Dictionary<string, Command>
        {
            ["help"] = new Command(
                "Faction Objectives",
                "Displays command help.",
                message => I.ShowHelp()),

            ["add"] = new Command(
                "Faction Objectives",
                "Add a new objective (leaders only)",
                CommandMethods.HandleAddObjective),
            ["list"] = new Command(
                "Faction Objectives",
                "Lists all objectives.",
                CommandMethods.HandleListObjectives),
            ["show"] = new Command(
                "Faction Objectives",
                "Show the quest log locally.",
                CommandMethods.HandleShowObjectives),
            ["remove"] = new Command(
                "Faction Objectives",
                "Remove an objective (leaders only).",
                CommandMethods.HandleRemoveObjective),
            ["broadcast"] = new Command(
                "Faction Objectives",
                "Show the quest log to all members for <time> seconds.",
                CommandMethods.HandleBroadcast),
            ["hide"] = new Command(
                "Faction Objectives",
                "Manually hide the quest log.",
                CommandMethods.HandleHideQuestLog),
            ["notifications"] = new Command(
                "Faction Objectives",
                "[on/off] Enable notifications",
                CommandMethods.HandleNotifications),
            ["clear"] = new Command(
                "Faction Objectives",
                "Clear all objectives (leaders only).",
                CommandMethods.HandleClearObjectives),
        };

        private CommandHandler()
        {
        }

        private void ShowHelp()
        {
            var fullHelpBuilder = new StringBuilder();
            var helpBuilder = new StringBuilder();
            var modNames = new List<string>();
            foreach (var command in _commands.Values)
                if (!modNames.Contains(command.modName))
                    modNames.Add(command.modName);

            MyAPIGateway.Utilities.ShowMessage("Faction Objectives Help", "");

            foreach (var modName in modNames)
            {
                foreach (var command in _commands)
                    if (command.Value.modName == modName)
                        helpBuilder.Append($"\n{{/obj {command.Key}}}: " + command.Value.helpText);

                fullHelpBuilder.AppendLine($"[{modName}]: {helpBuilder}");
                helpBuilder.Clear();
            }

            MyAPIGateway.Utilities.ShowMissionScreen(
                "Faction Objectives Help", 
                "Available Commands", 
                "", 
                fullHelpBuilder.ToString());
        }

        public static void Init()
        {
            Close(); // Close existing command handlers.
            I = new CommandHandler();
            MyAPIGateway.Utilities.MessageEnteredSender += I.Command_MessageEnteredSender;
            MyAPIGateway.Utilities.ShowMessage($"Faction Objectives",
                "Run \"/obj help\" for commands.");
        }

        public static void Close()
        {
            if (I != null)
            {
                MyAPIGateway.Utilities.MessageEnteredSender -= I.Command_MessageEnteredSender;
                I._commands.Clear();
            }

            I = null;
        }

        private void Command_MessageEnteredSender(ulong sender, string messageText, ref bool sendToOthers)
        {
            try
            {
                // Only register for commands
                if (messageText.Length == 0 || !messageText.ToLower().StartsWith("/obj"))
                    return;

                sendToOthers = false;

                var parts = messageText.Substring(4).Trim(' ').Split(' '); // Convert commands to be more parseable

                if (parts[0] == "")
                {
                    ShowHelp();
                    return;
                }

                // Really basic command handler
                if (_commands.ContainsKey(parts[0].ToLower()))
                    _commands[parts[0].ToLower()].action.Invoke(parts);
                else
                    MyAPIGateway.Utilities.ShowMessage("Faction Objectives",
                        $"Unrecognized command \"{parts[0].ToLower()}\"");
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole(ex.ToString());
            }
        }

        /// <summary>
        ///     Registers a command for Faction Objectives' command handler.
        /// </summary>
        /// <param name="command"></param>
        /// <param name="action"></param>
        /// <param name="modName"></param>
        public static void AddCommand(string command, string helpText, Action<string[]> action,
            string modName = "Faction Objectives")
        {
            if (I == null)
                return;

            command = command.ToLower();
            if (I._commands.ContainsKey(command))
            {
                MyLog.Default.WriteLineAndConsole("Attempted to add duplicate command " + command + " from [" + modName + "]");
                return;
            }

            I._commands.Add(command, new Command(modName, helpText, action));
            MyLog.Default.WriteLineAndConsole("Registered new chat command \"/{command}\" from [{modName}]");
        }

        /// <summary>
        ///     Removes a command from Faction Objectives' command handler.
        /// </summary>
        /// <param name="command"></param>
        public static void RemoveCommand(string command)
        {
            command = command.ToLower();
            if (I == null || command == "help" || command == "debug") // Debug and Help should never be removed.
                return;
            if (I._commands.Remove(command))
                MyLog.Default.WriteLineAndConsole($"De-registered chat command \"!{command}\".");
        }

        private class Command
        {
            public readonly Action<string[]> action;
            public readonly string helpText;
            public readonly string modName;

            public Command(string modName, string helpText, Action<string[]> action)
            {
                this.modName = modName;
                this.helpText = helpText;
                this.action = action;
            }
        }
    }
}

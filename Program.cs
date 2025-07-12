using DSharpPlus;
using DSharpPlus.CommandsNext;
using System;
using System.Collections.Generic;
using System.Linq;
using DSharpPlus.SlashCommands;
using System.Text;
using System.Threading.Tasks;
using chiBot.Files;
using System.IO;
using chiBot.Commands;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.DependencyInjection;
using DSharpPlus.SlashCommands.EventArgs;

namespace chiBot
{
    internal class Program
    {
        public static DiscordClient client { get; set; }

        public static CommandsNextExtension commands { get; set; }

        public static Config config;

        static async Task Main(string[] args)
        {
            config = FileHandler.ReadFromJsonFile<Config>(Path.Combine(FileHandler.GetExecutingDir(), "References", "Config", "config.json"));
            string aliasLookupPath = Path.Combine(FileHandler.GetExecutingDir(), "References", "Data", "aliasLookup.json");
            if (!File.Exists(aliasLookupPath))
            {
                AliasLookup aliases = new AliasLookup();
                aliases.lookup = new Dictionary<string, string>();
                FileHandler.WriteToJsonFile(aliasLookupPath, aliases);
            }

            var discordConfig = new DiscordConfiguration()
            {
                Intents = DiscordIntents.All,
                Token = config.token,
                TokenType = TokenType.Bot,
                AutoReconnect = true,
            };

            client = new DiscordClient(discordConfig);
            client.Ready += Client_Ready;
            client.UseInteractivity(new InteractivityConfiguration()
            {
                Timeout = TimeSpan.FromMinutes(2),
            });

            var commandsConfig = new CommandsNextConfiguration()
            {
                StringPrefixes = new string[] {config.prefix},
                EnableMentionPrefix = true,
                EnableDms = true,
                EnableDefaultHelp = false,
            };

            commands = client.UseCommandsNext(commandsConfig);

            var slashCommands = client.UseSlashCommands();
            await client.GetSlashCommands().RefreshCommands();
            slashCommands.RegisterCommands<SteamCommands>(ulong.Parse(config.serverID));
            slashCommands.RegisterCommands<HelperCommands>(ulong.Parse(config.serverID));

            slashCommands.SlashCommandInvoked += slashInvoked;
            slashCommands.SlashCommandExecuted += slashCompleted;
            slashCommands.SlashCommandErrored += slashErrored;


            //connections

            await client.ConnectAsync();

            //Command Deletion goes here
            var deleteOldCommands = false;
            if (deleteOldCommands)
            {
                Console.WriteLine("Deleting Old Commands");
                //await client.DeleteGlobalApplicationCommandAsync(commandID);
                //put all command deletions here and they are as follows. Make sure to delete them after they have been ran once
                Console.WriteLine("Old Commands Deleted");
            }

            await Task.Delay(-1);
        }






        //logging

        private static async Task slashErrored(SlashCommandsExtension sender, SlashCommandErrorEventArgs args)
        {
            Console.WriteLine("Slash Command Errord");
            Console.WriteLine($"Error: {args.Exception.Message}");
            Console.WriteLine($"StackTrace: {args.Exception.StackTrace}");
            Console.WriteLine(args.Exception.Source);
        }

        private static async Task slashCompleted(SlashCommandsExtension sender, SlashCommandExecutedEventArgs args)
        {
            Console.WriteLine("Slash Command Completed");
        }

        private static async Task slashInvoked(SlashCommandsExtension sender, SlashCommandInvokedEventArgs args)
        {
            Console.WriteLine("Slash Command Invoked");
        }


        //dependancies

        private static Task Client_Ready(DiscordClient sender, DSharpPlus.EventArgs.ReadyEventArgs args)
        {
            return Task.CompletedTask;
        }

        public class Config
        {
            public string token;
            public string serverID;
            public string prefix;
            public string steamKey;
        }
    }
}


public class SteamIDLookup
{
    public Dictionary<string, string> lookup; //Dictionary<discrodID, steamID>
}

public class UsernameToID
{
    public Dictionary<string, string> lookup; //Dictionary<username, id>
}

using chiBot.Files;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;

namespace chiBot
{
    public static class Tools
    {
        public static async Task ThrowError(InteractionContext context, Exception e)
        {
            DiscordEmbedBuilder msg = new DiscordEmbedBuilder()
            {
                Title = $"Error: {e.Message}",
                Description = e.StackTrace,
                Color = DiscordColor.Red,
            };

            await context.Channel.SendMessageAsync(embed: msg);
        }

        public static void ConsolOutDict<TKey, TValue>(Dictionary<TKey, TValue> dictIn)
        {
            foreach (var key in dictIn.Keys)
            {
                Console.WriteLine($"{key}: {dictIn[key]}");
            }
        }

        public static string GetAlias(string username)
        {
            string aliasLookupPath = Path.Combine(FileHandler.GetExecutingDir(), "References", "Data", "aliasLookup.json");
            AliasLookup aliases = FileHandler.ReadFromJsonFile<AliasLookup>(aliasLookupPath);
            if (aliases.lookup.ContainsKey(username)) //if in files
            {
                return aliases.lookup[username]; //alias
            }
            else
            {
                return username;
            }
        }
    }

    public class AliasLookup
    {
        public Dictionary<string, string> lookup;
    }
}

using chiBot.Files;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chiBot.Commands
{
    public class HelperCommands : ApplicationCommandModule
    {
        [SlashCommand("add_alias", "Adds an alias from your standard username, to a username of your choice")]
        public async Task AddAlias(InteractionContext ctx, [Option("Alias", "The alias you would Like to register")] string alias)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            string aliasLookupPath = Path.Combine(FileHandler.GetExecutingDir(), "References", "Data", "aliasLookup.json");

            AliasLookup aliases = FileHandler.ReadFromJsonFile<AliasLookup>(aliasLookupPath);

            if (aliases.lookup.ContainsKey(ctx.User.Username))
            {
                aliases.lookup[ctx.User.Username] = alias;
            }
            else
            {
                aliases.lookup.Add(ctx.User.Username, alias);
            }

            FileHandler.WriteToJsonFile(aliasLookupPath, aliases);

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Added Alias"));
        } 
    }
}

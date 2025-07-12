using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.Interactivity.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics.Contracts;

namespace chiBot.Commands
{
    public class GeneralCommands : BaseCommandModule
    {
        [Command("help"), Description("Displays all available commands")]
        public async Task HelpCommand(CommandContext context) //context is all interactions with discord must be in all commands
        {
            var commandsEnum = Program.commands.RegisteredCommands.Values;
            string commandList = "";

            foreach (var command in commandsEnum)
            {
                commandList += command.Name + $" -> {command.Description}" + "\n\n";
            }

            DiscordEmbedBuilder msg = new DiscordEmbedBuilder()
            {
                Title = "Available Commands",
                Description = commandList,
                Color = DiscordColor.HotPink,
            };

            await context.Channel.SendMessageAsync(embed: msg);
        }

        //this is for a test
        [Command("poll"), Description("Creates a Poll based off the given perameters")]
        public async Task Poll(CommandContext context, string op1, string op2, string op3, string op4, string pollLength, [RemainingText] string title)
        {
            var client = Program.client;
            var inter = client.GetInteractivity();
            DiscordEmoji[] emojiOptions = new DiscordEmoji[4] { DiscordEmoji.FromName(client, ":one:"), DiscordEmoji.FromName(client, ":two:"), DiscordEmoji.FromName(client, ":three:"), DiscordEmoji.FromName(client, ":four:") };
            TimeSpan pollTime = TimeSpan.FromSeconds(int.Parse(pollLength));

            var msg = new DiscordEmbedBuilder()
            {
                Title = title,
                Color = DiscordColor.Teal,
                Description = emojiOptions[0] + " -> " + op1 + "\n" + emojiOptions[1] + " -> " + op2 + "\n" + emojiOptions[2] + " -> " + op3 + "\n" + emojiOptions[3] + " -> " + op4
            };

            var poll = await context.Channel.SendMessageAsync(embed: msg);

            foreach (var emoji in emojiOptions) 
            {
                await poll.CreateReactionAsync(emoji);
            }

            var reacts = await inter.CollectReactionsAsync(poll, pollTime);

            int re1 = 0;
            int re2 = 0;
            int re3 = 0;
            int re4 = 0;

            foreach (var react in reacts)
            {
                if (react.Emoji == DiscordEmoji.FromName(client, ":one:"))
                {
                    re1++;
                }
                else if (react.Emoji == DiscordEmoji.FromName(client, ":two:"))
                {
                    re2++;
                }
                else if (react.Emoji == DiscordEmoji.FromName(client, ":three:"))
                {
                    re3++;
                }
                else if (react.Emoji == DiscordEmoji.FromName(client, ":four:"))
                {
                    re4++;
                }
            }

            int total = re1 + re2 + re3 + re4;

            Dictionary<string, int> counts = new Dictionary<string, int>();

            counts.Add(op1, re1);
            counts.Add(op2, re2);
            counts.Add(op3, re3);
            counts.Add(op4, re4);

            var maxRecord = counts.Aggregate((l, r) => l.Value > r.Value ? l : r);

            string results = $"{maxRecord.Key} with {maxRecord.Value} votes" +
                $"\n\n {emojiOptions[0]}: {re1} \n" +
                $"{emojiOptions[1]}: {re2} \n" +
                $"{emojiOptions[2]}: {re3} \n" +
                $"{emojiOptions[3]}: {re4} \n";

            var result = new DiscordEmbedBuilder()
            {
                Title = "The Winning Vote Is",
                Color = DiscordColor.Teal,
                Description = results
            };

            await context.Channel.SendMessageAsync(embed: result);
        }
    }
}

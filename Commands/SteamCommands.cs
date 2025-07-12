using chiBot.Files;
using chiBot.Steam_Integration;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace chiBot.Commands
{
    public class SteamCommands : ApplicationCommandModule
    {
        private static Dictionary<string, List<string>> recentSharedGamesCahce;

        [SlashCommand("getsharedgames", "Get all Mulitplayer Games that you and the passed in user share")]
        public async Task GetSharedGames(InteractionContext ctx, [Option("User", "User you want to match games with")] DiscordUser user)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Fetching Games"));

            try
            {
                string username = user.Username;
                SteamLink steam = new SteamLink();

                var otherUser = steam.GetSteamIDFromUsername(username);
                var currentUser = steam.GetSteamIDFromUsername(ctx.User.Username);

                if (otherUser == "404")
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"{Tools.GetAlias(username)} is not in registry, please have them register their steamID for this to work"));
                    return;
                }

                if (currentUser == "404")
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You are not in the registry, please register your steamID"));
                    return;
                }

                var otherUserGames = await steam.GetGamesViaId(otherUser, 0, 0, 3, ctx);
                var currentUserGames = await steam.GetGamesViaId(currentUser, 0, 0, 3, ctx);

                if (otherUserGames == null || currentUserGames == null)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Could not retrieve Games"));
                    return;
                }

                var commonEntries = otherUserGames
                    .Where(kvp => currentUserGames.ContainsKey(kvp.Key))
                    .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

                recentSharedGamesCahce = commonEntries;

                string description = "";

                Console.WriteLine("Formating Shared Games: ");
                foreach (var game in commonEntries)
                {
                    description += game.Key + "\n\n" + game.Value[1] + "\n\n" +
                                   "--------------------------------------------" + "\n\n";
                }

                var embed = new DiscordEmbedBuilder()
                {
                    Title = $"Shared Games Between {Tools.GetAlias(ctx.User.Username)} & {Tools.GetAlias(user.Username)}",
                    Description = description,
                    Color = DiscordColor.Blue,
                };

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            }
            catch (Exception ex)
            {
                await Tools.ThrowError(ctx, ex);
            }
        }

        [SlashCommand("registersteamid", "Registers a Steam ID to your account")]
        public static async Task RegisterSteamID(InteractionContext ctx, [Option("steamid", "SteamId you would like to register with this account")] string steamID)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            string regIDPath = Path.Combine(FileHandler.GetExecutingDir(), "Steam_Integration\\SteamRegister\\registeredID.json");
            string discrodUsernameToIDLookup = Path.Combine(FileHandler.GetExecutingDir(), "Steam_Integration\\SteamRegister\\discordUsernameToIDLookup.json");

            SteamIDLookup steamIDLookup = new SteamIDLookup();
            UsernameToID usernameToID = new UsernameToID();

            try
            {
                if (File.Exists(discrodUsernameToIDLookup))
                {
                    usernameToID = FileHandler.ReadFromJsonFile<UsernameToID>(discrodUsernameToIDLookup);

                    if (usernameToID.lookup.ContainsKey(ctx.User.Username))
                    {
                        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Already Added To Registry"));
                        return;
                    }

                    usernameToID.lookup[ctx.User.Username] = ctx.User.Id.ToString();
                }
                else
                {
                    usernameToID.lookup = new Dictionary<string, string>
                    {
                        { ctx.User.Username, ctx.User.Id.ToString() }
                    };
                }

                if (File.Exists(regIDPath))
                {
                    steamIDLookup = FileHandler.ReadFromJsonFile<SteamIDLookup>(regIDPath);
                    steamIDLookup.lookup[ctx.User.Id.ToString()] = steamID;
                }
                else
                {
                    steamIDLookup.lookup = new Dictionary<string, string>
                    {
                        { ctx.User.Id.ToString(), steamID }
                    };
                }

                FileHandler.WriteToJsonFile(regIDPath, steamIDLookup);
                FileHandler.WriteToJsonFile(discrodUsernameToIDLookup, usernameToID);

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Added To Registry"));
            }
            catch (Exception e)
            {
                if (e.Message == "An item with the same key has already been added.")
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Already Added To Registry"));
                }
                else
                {
                    await Tools.ThrowError(ctx, e);
                }
            }
        }

        [SlashCommand("pollsharedgames", "Creates a poll from recently shared multiplayer games")]
        public async Task PollSharedGames(InteractionContext ctx, [Option("Duration", "Length of the poll in seconds")] long pollLength = 30, [Option("Title", "Optional title for the poll")] string title = "Vote for a Game!")
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            Console.WriteLine($"Recent Shared Cache: {recentSharedGamesCahce.Count}");

            if (recentSharedGamesCahce == null || recentSharedGamesCahce.Count < 2)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Need at least 2 shared games to create a poll."));
                return;
            }

            var allGames = recentSharedGamesCahce.Keys.ToList();
            var random = new Random();
            const int maxOptions = 10;

            // Randomly pick 10 if there are more
            if (allGames.Count > maxOptions)
                allGames = allGames.OrderBy(_ => random.Next()).Take(maxOptions).ToList();

            var emojiOptions = new[]
            {
                DiscordEmoji.FromName(Program.client, ":one:"), DiscordEmoji.FromName(Program.client, ":two:"), DiscordEmoji.FromName(Program.client, ":three:"), DiscordEmoji.FromName(Program.client, ":four:"), DiscordEmoji.FromName(Program.client, ":five:"),
                DiscordEmoji.FromName(Program.client, ":six:"), DiscordEmoji.FromName(Program.client, ":seven:"), DiscordEmoji.FromName(Program.client, ":eight:"), DiscordEmoji.FromName(Program.client, ":nine:"), DiscordEmoji.FromName(Program.client, ":keycap_ten:")
            };

            string pollDescription = string.Join("\n", allGames.Select((game, i) => $"{emojiOptions[i]} → {game}"));

            var embed = new DiscordEmbedBuilder
            {
                Title = title,
                Description = pollDescription,
                Color = DiscordColor.Azure
            };

            var pollMessage = await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));

            foreach (var emoji in emojiOptions)
                await pollMessage.CreateReactionAsync(emoji);

            var interactivity = ctx.Client.GetInteractivity();
            var collected = await interactivity.CollectReactionsAsync(pollMessage, TimeSpan.FromSeconds(pollLength));

            var voteCounts = new Dictionary<string, int>();
            for (int i = 0; i < allGames.Count; i++)
                voteCounts[allGames[i]] = collected.Count(r => r.Emoji == emojiOptions[i]);

            var winner = voteCounts.OrderByDescending(v => v.Value).First();

            string resultText = $"**{winner.Key}** wins with **{winner.Value} votes**!\n\n" +
                                string.Join("\n", allGames.Select((game, i) => $"{emojiOptions[i]}: {voteCounts[game]} vote(s)"));

            var resultEmbed = new DiscordEmbedBuilder
            {
                Title = "Poll Results",
                Description = resultText,
                Color = DiscordColor.Green
            };

            await ctx.Channel.SendMessageAsync(embed: resultEmbed);
        }
    }
}

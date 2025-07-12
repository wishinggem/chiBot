using chiBot.Files;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Contexts;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace chiBot.Steam_Integration
{
    public class SteamLink
    {
        private string key;
        private string lookupPath = Path.Combine(FileHandler.GetExecutingDir(), "References", "Data", "steamGameIDLookup.json");

        public SteamLink()
        {
            key = Program.config.steamKey;
            Console.WriteLine("Steam API Key loaded.");
        }

        public async Task<ConcurrentDictionary<string, List<string>>> GetGamesViaId(
            string ID, int wait, int attempt, int maxAttempts, InteractionContext ctx)
        {
            try
            {
                GameLookup steamIDLookup = new GameLookup();
                steamIDLookup.lookup = new Dictionary<int, GameInfo>();

                if (!File.Exists(lookupPath))
                {
                    File.Create(lookupPath);
                }
                else
                {
                    steamIDLookup = FileHandler.ReadFromJsonFile<GameLookup>(lookupPath);
                }
                string url = $"https://api.steampowered.com/IPlayerService/GetOwnedGames/v1/?key={key}&steamid={ID}&include_appinfo=true&include_played_free_games=true";
                Console.WriteLine($"Fetching games from URL: {url}");

                HttpClient client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();

                string json = await response.Content.ReadAsStringAsync();
                JsonDocument doc = JsonDocument.Parse(json);
                var gamesList = new List<int>();

                if (doc.RootElement.TryGetProperty("response", out JsonElement responseElement) &&
                    responseElement.TryGetProperty("games", out JsonElement games))
                {
                    foreach (JsonElement game in games.EnumerateArray())
                    {
                        if (game.TryGetProperty("appid", out JsonElement appIdElement) &&
                            game.TryGetProperty("playtime_forever", out JsonElement timePlayed) &&
                            timePlayed.GetInt32() > 0)
                        {
                            gamesList.Add(appIdElement.GetInt32());
                        }
                    }
                }

                var multiGames = new ConcurrentDictionary<string, List<string>>();
                int maxConcurrency = 5; // tune based on API limits
                var semaphore = new SemaphoreSlim(maxConcurrency);

                var tasks = gamesList.Select(async gameId =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        if (steamIDLookup.lookup.ContainsKey(gameId)) //not in file
                        {
                            var details = steamIDLookup.lookup[gameId];
                            if (details.isMultiplayer)
                            {
                                multiGames[details.name] = new List<string>
                                {
                                    details.description,
                                    details.gameLink,
                                };
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Game ID: {gameId} not in lookup adding to lookup");
                            var details = await GetGameDetails(gameId);
                            if (details.isMultiplayer)
                            {
                                multiGames[details.name] = new List<string>
                                {
                                    details.description,
                                    $"https://store.steampowered.com/app/{gameId}/"
                                };
                            }

                            steamIDLookup.lookup.Add(gameId, new GameInfo
                            {
                                name = details.name,
                                description = details.description,
                                isMultiplayer = details.isMultiplayer,
                                gameLink = $"https://store.steampowered.com/app/{gameId}/",
                            });
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                await Task.WhenAll(tasks);

                FileHandler.WriteToJsonFile<GameLookup>(lookupPath, steamIDLookup);

                Console.WriteLine($"Completed game fetch for ID {ID}.");
                return multiGames;
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("429"))
            {
                if (attempt < maxAttempts)
                {
                    Console.WriteLine("Rate limit hit. Retrying...");
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Rate limit hit. Retrying attempt {attempt + 1} of {maxAttempts}..."));
                    await Task.Delay(wait);
                    return await GetGamesViaId(ID, wait, attempt + 1, maxAttempts, ctx);
                }

                Console.WriteLine("Max retry attempts reached. Returning null.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving games: {ex.Message}");
                return null;
            }
        }

        public async Task<(string name, string description, bool isMultiplayer)> GetGameDetails(int appId)
        {
            try
            {
                string url = $"https://store.steampowered.com/api/appdetails?appids={appId}";
                HttpClient client = new HttpClient();
                string json = await client.GetStringAsync(url);

                JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement.GetProperty(appId.ToString());

                if (!root.GetProperty("success").GetBoolean())
                    return (null, null, false);

                JsonElement data = root.GetProperty("data");

                string name = data.GetProperty("name").GetString();
                string desc = data.GetProperty("short_description").GetString();

                bool isMultiplayer = false;
                if (data.TryGetProperty("categories", out JsonElement categories))
                {
                    foreach (JsonElement category in categories.EnumerateArray())
                    {
                        string descText = category.GetProperty("description").GetString();
                        if (descText.Contains("Multi-player") || descText.Contains("Co-op"))
                        {
                            isMultiplayer = true;
                            break;
                        }
                    }
                }

                return (name, desc, isMultiplayer);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching game details for {appId}: {ex.Message}");
                return (null, null, false);
            }
        }

        public string GetSteamIDFromUsername(string username)
        {
            try
            {
                string regIDPath = Path.Combine(FileHandler.GetExecutingDir(), "Steam_Integration\\SteamRegister\\registeredID.json");
                string usernameToIdPath = Path.Combine(FileHandler.GetExecutingDir(), "Steam_Integration\\SteamRegister\\discordUsernameToIDLookup.json");

                var usernameToID = FileHandler.ReadFromJsonFile<UsernameToID>(usernameToIdPath);
                var steamIDLookup = FileHandler.ReadFromJsonFile<SteamIDLookup>(regIDPath);

                if (steamIDLookup.lookup[usernameToID.lookup[username.ToLower()]] == null || steamIDLookup.lookup[usernameToID.lookup[username.ToLower()]] == string.Empty)
                {
                    return "404";
                }
                else
                {
                    return steamIDLookup.lookup[usernameToID.lookup[username.ToLower()]];
                }

            }
            catch
            {
                return "404";
            }
        }
    }

    public class GameLookup
    {
        public Dictionary<int, GameInfo> lookup; //int = gameid
    }

    public class GameInfo
    {
        public string name;
        public string description;
        public string gameLink;
        public bool isMultiplayer;
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Beast.Common.Managers;
using Beast.Common.Models;
using CsvHelper;
using Discord;
using Discord.Audio;
using Discord.Commands;
using DiscordBot.Properties;
using Humanizer;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Extensions.MonoHttp;
using ParameterType = Discord.Commands.ParameterType;

namespace DiscordBot
{
    public class Program
    {
        static void Main(string[] args) => new Program().Start();

        private DiscordClient _client;
        private DatabaseManager _databaseManager;
        private CancellationTokenSource _tokenSource;
        private List<string> _metaNames;

        private bool _tts;

        public List<Reminder> Reminders { get; set; }


        public async void Start()
        {
            _client = new DiscordClient();
            _databaseManager = new DatabaseManager();
            _tokenSource = new CancellationTokenSource();
            _metaNames = new List<string> { "chak", "octovine", "verdant brink", "dragon's stand" };

            Reminders = LoadReminders();
            StartRemindersTask();

            _client.UsingCommands(x =>
            {
                x.PrefixChar = '!';
                x.HelpMode = HelpMode.Public;
            });

            _client.UsingAudio(x =>
            {
                x.Mode = AudioMode.Outgoing;
            });

            _client.UserJoined += (sender, args) =>
            {
                args.User.Channels.FirstOrDefault()?.SendMessage($"{args.User.Nickname} has joined.");
            };

            _client.GetService<CommandService>().CreateCommand("roll")
                .Description("Rolls 1-100")
                .Parameter("max", ParameterType.Unparsed)
                .Do(async e =>
                {
                    var random = new Random();
                    var name = string.IsNullOrEmpty(e.User.Nickname) ? e.User.Name : e.User.Nickname;

                    var max = e.Args.FirstOrDefault();

                    if (string.IsNullOrWhiteSpace(max))
                        await e.Channel.SendMessage($"{name} rolled {Format.Bold($"{random.Next(1, 101)}")}");
                    else
                    {
                        var maxRoll = 0;
                        int.TryParse(max, out maxRoll);
                        await e.Channel.SendMessage($"{name} rolled {Format.Bold($"{random.Next(1, maxRoll + 1)}")}");
                    }
                });

            _client.GetService<CommandService>().CreateCommand("item")
                .Description("Gives chat link for GW2 item, if it can find it.")
                .Parameter("ItemName", ParameterType.Unparsed)
                .Do(async e =>
                {
                    var itemName = e.Args.FirstOrDefault();
                    var items = _databaseManager.GetItemsThatMatch(itemName);
                    await e.Channel.SendMessage($"{items.FirstOrDefault()?.ItemName}\n{items.FirstOrDefault()?.ChatLink}\nhttp://wiki.guildwars2.com/index.php?title=Special%3ASearch&search={HttpUtility.UrlEncode(items.FirstOrDefault()?.ChatLink)}&fulltext=1");
                });

            _client.GetService<CommandService>().CreateCommand("penetration")
                .Alias("penne", "pene", "p", "fruit")
                .Description("Penetration!")
                .Do(async e =>
                {
                    var items = new[] { "[&AgEqMABAuV8AAA=]", "[&AgHbLwBAuV8AAA=]" };

                    await e.Channel.SendTTSMessage("Did somebody say penetration?");
                    await e.Channel.SendMessage(items[new Random().Next(0, 2)]);
                });

            _client.GetService<CommandService>().CreateCommand("narshall")
                .Alias("n")
                .Description("Ehehehe")
                .Hide()
                .Do(async e =>
                {
                    var users = e.Server.Users;

                    foreach (var user in users.Where(user => user.Name.ToLower().Contains("paragon")))
                    {
                        try
                        {
                            if (e.User.Roles.FirstOrDefault(x => x.Name == "mod") != null)
                                await user.Edit(true, true, user.VoiceChannel, user.Roles, user.Nickname);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        break;
                    }
                });

            _client.GetService<CommandService>().CreateCommand("unnarshall")
                .Alias("unn")
                .Description("Un-ehehehe")
                .Hide()
                .Do(async e =>
                {
                    var user = e.Server.Users.FirstOrDefault(x => x.Name.ToLower().Contains("paragon"));

                    try
                    {
                        if (user == e.User) return;
                        await user?.Edit(false, false, user.VoiceChannel, user.Roles, user.Nickname);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                });

            _client.GetService<CommandService>().CreateCommand("ceddy")
                .Alias("c")
                .Description("Ceddy Ehehehe")
                .Hide()
                .Do(async e =>
                {
                    var users = e.Server.Users;

                    foreach (var user in users.Where(user => user.Name.ToLower().Contains("ceddy")))
                    {
                        try
                        {
                            if (e.User.Roles.FirstOrDefault(x => x.Name == "mod") != null)
                                await user.Edit(true, true, user.VoiceChannel, user.Roles, user.Nickname);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        break;
                    }
                });

            _client.GetService<CommandService>().CreateCommand("unceddy")
                .Alias("unc")
                .Description("Ceddy Ehehehe")
                .Hide()
                .Do(async e =>
                {
                    var users = e.Server.Users;

                    foreach (var user in users.Where(user => user.Name.ToLower().Contains("ceddy")))
                    {
                        try
                        {
                            if (user == e.User) return;
                            await user.Edit(false, false, user.VoiceChannel, user.Roles, user.Nickname);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        break;
                    }
                });

            _client.GetService<CommandService>().CreateCommand("tts")
                .Description("Toggle on/off TTS announcement of boss messages")
                .Parameter("status", ParameterType.Unparsed)
                .Do(e =>
                {
                    var statusText = e.Args.FirstOrDefault()?.ToLower();
                    if (statusText.Contains("on") || statusText.Contains("yes") || statusText.Contains("true"))
                        _tts = true;
                    else if (statusText.Contains("off") || statusText.Contains("no") || statusText.Contains("false"))
                        _tts = false;
                });

            _client.GetService<CommandService>().CreateCommand("mute")
                .Description("Mute a player by name")
                .Alias("m")
                .Parameter("name", ParameterType.Unparsed)
                .Do(async e =>
                {
                    if (e.User.Roles.FirstOrDefault(x => x.Name == "mod") == null) return;

                    var name = e.Args.FirstOrDefault();
                    var user = e.Channel.Users.FirstOrDefault(x => x.Name.ToLower().Contains(name));
                    if (user != null)
                    {
                        try
                        {
                            await user.Edit(!user.IsServerMuted, user.IsServerDeafened, user.VoiceChannel, user.Roles);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                });

            _client.GetService<CommandService>().CreateCommand("boss")
                .Alias("b", "wb", "worldboss")
                .Description("Links information about world bosses. Can use \"next\" and \"meta\" to get either the next upcoming world boss, or meta event, respectively.")
                .Parameter("BossName", ParameterType.Unparsed)
                .Do(async e =>
                {
                    WorldBoss worldBoss = null;

                    var bossName = e.Args.FirstOrDefault().ToLower();

                    if (string.IsNullOrEmpty(bossName))
                    {
                        await e.Channel.SendTTSMessage("lolwut");
                        return;
                    }

                    switch (bossName)
                    {
                        case "next":
                            worldBoss =
                                WorldBosses.OrderBy(t => (t.Start - DateTime.Now).Duration())
                                    .FirstOrDefault();
                            break;
                        case "meta":
                            worldBoss =
                                WorldBosses.OrderBy(t => (t.Start - DateTime.Now).Duration())
                                    .FirstOrDefault(x => _metaNames.Any(y => x.EventName.ToLower().Contains(y)));
                            break;
                        default:
                            worldBoss =
                                WorldBosses.OrderBy(t => (t.End - DateTime.Now).Duration())
                                    .FirstOrDefault(x => x.EventName.ToLower().Contains(bossName));
                            break;
                    }

                    if (worldBoss == null)
                    {
                        await e.Channel.SendTTSMessage("lolwut");
                        return;
                    }

                    if (worldBoss.Start <= DateTime.Now)
                    {
                        if (_tts)
                        {
                            await
                                e.Channel.SendTTSMessage(
                                    $"{worldBoss.EventName} - Happening now! Started {Format.Bold(worldBoss.Start.Humanize(false))}");
                        }
                        else
                        {
                            await
                                e.Channel.SendMessage(
                                    $"{worldBoss.EventName} - Happening now! Started {Format.Bold(worldBoss.Start.Humanize(false))}");
                        }
                        await e.Channel.SendMessage($"{worldBoss.Link}");
                    }

                    else
                    {
                        if (_tts)
                        {
                            await
                                e.Channel.SendTTSMessage(
                                    $"{worldBoss.EventName} - {Format.Bold(worldBoss.Start.Humanize(false))}");
                        }
                        else
                        {
                            await
                                e.Channel.SendMessage(
                                    $"{worldBoss.EventName} - {Format.Bold(worldBoss.Start.Humanize(false))}");
                        }
                        await e.Channel.SendMessage($"{worldBoss.Link}");
                    }


                });

            _client.GetService<CommandService>().CreateCommand("reminder")
                .Alias("remind", "r")
                .Description(
                    "Set a reminder for a world boss spawn. Format is !r <minutes before boss spawns that you'd like a reminder> <boss name>")
                .Parameter("Time", ParameterType.Optional)
                .Parameter("BossName", ParameterType.Unparsed)
                .Do(async e =>
                {
                    var args = e.Args;
                    var minutes = args.Length > 1 ? int.Parse(e.Args[0]) : 15;
                    var bossName = args.Length > 1 ? e.Args[1] : e.Args[0];
                    bossName = bossName.ToLower();


                    var worldBoss =
                                WorldBosses.OrderBy(t => (t.Start - DateTime.Now).Duration())
                                    .FirstOrDefault(x => x.EventName.ToLower().Contains(bossName));


                    var reminder = new Reminder
                    {
                        BossName = bossName,
                        User = e.User.Mention,
                        ReminderTime = worldBoss.Start.AddMinutes(-minutes)
                    };

                    Reminders.Add(reminder);
                    var json = JsonConvert.SerializeObject(Reminders);
                    File.WriteAllText("reminders.json", json);

                    await e.Channel.SendMessage($"Reminder confirmed: {worldBoss.EventName} at {reminder.ReminderTime}");
                });

            _client.ExecuteAndWait(async () =>
            {
                await Connect();
            });
        }

        private List<Reminder> LoadReminders()
        {
            if (!File.Exists("reminders.json")) File.Create("reminders.json").Close();

            var json = File.ReadAllText("reminders.json");
            return JsonConvert.DeserializeObject<List<Reminder>>(json) ?? new List<Reminder>();
        }

        private async Task Connect()
        {
            await _client.Connect(Constants.ApiKey);
            Console.WriteLine("Connected!");
            StartDatabaseUpdateTask();
            StartWorldBossUpdateTask();
            Console.WriteLine("World bosses loaded");
        }


        private void StartRemindersTask()
        {
            var task = new Task(() =>
            {
                while (!_tokenSource.IsCancellationRequested)
                {
                    DoReminders();
                    Thread.Sleep(30000);
                }
            }, _tokenSource.Token, TaskCreationOptions.LongRunning);

            task.Start();
        }

        private void DoReminders()
        {
            Reminders = LoadReminders();

            Reminder reminderToRemove = null;

            foreach (var reminder in Reminders)
            {
                if (DateTime.Now.Hour == reminder.ReminderTime.Hour && DateTime.Now.Minute == reminder.ReminderTime.Minute)
                {
                    var server = _client.FindServers("aegis of flame").FirstOrDefault();
                    var user = server.Users.FirstOrDefault(x => x.Mention == reminder.User);
                    var worldBoss =
                        WorldBosses.OrderBy(t => (t.Start - DateTime.Now).Duration())
                            .FirstOrDefault(x => x.EventName.ToLower().Contains(reminder.BossName));

                    user?.SendMessage($"Hey, {reminder.BossName} is starting {worldBoss.Start.Humanize(false)}");

                    reminderToRemove = reminder;
                }
            }

            Reminders.Remove(reminderToRemove);
            File.WriteAllText("reminders.json", JsonConvert.SerializeObject(Reminders));
        }

        private void StartDatabaseUpdateTask()
        {
            var task = new Task(async () =>
            {
                while (!_tokenSource.IsCancellationRequested)
                {
                    try
                    {
                        await _databaseManager.UpdateDatabase();
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                    Thread.Sleep(300000);
                }
            }, _tokenSource.Token, TaskCreationOptions.LongRunning);

            task.Start();
        }

        private void StartWorldBossUpdateTask()
        {
            var task = new Task(() =>
            {
                while (!_tokenSource.IsCancellationRequested)
                {
                    WorldBosses?.Clear();
                    LoadWorldBosses();
                    Thread.Sleep(30000);
                }
            }, _tokenSource.Token, TaskCreationOptions.LongRunning);

            task.Start();
        }

        public BindingList<WorldBoss> WorldBosses { get; set; }

        private void LoadWorldBosses()
        {
            var sw = new Stopwatch();
            sw.Start();
            var text = Resources.bosses;

            using (TextReader sr = new StringReader(text))
            {
                var csv = new CsvReader(sr);
                csv.Configuration.RegisterClassMap(new WorldBossesMap());
                var bossTimers = csv.GetRecords<WorldBoss>();

                if (WorldBosses == null) WorldBosses = new BindingList<WorldBoss>();
                foreach (var timer in bossTimers)
                {
                    if (timer.End < DateTime.UtcNow)
                    {
                        timer.End = timer.End.AddDays(1);
                        timer.Start = timer.Start.AddDays(1);
                    }

                    timer.Start = TimeZoneInfo.ConvertTimeFromUtc(timer.Start, TimeZoneInfo.Local);
                    timer.End = TimeZoneInfo.ConvertTimeFromUtc(timer.End, TimeZoneInfo.Local);


                    WorldBosses.Add(timer);
                }
            }
            sw.Stop();
        }

    }
}

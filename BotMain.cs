using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.DependencyInjection;

namespace ScratchBot
{
    //https://docs.stillu.cc/guides/concepts/logging.html api
    //https://docs.microsoft.com/en-us/dotnet/core/rid-catalog Build info
    //dotnet publish -c Release -r linux-arm

    internal class BotMain
    {
        private readonly DiscordSocketClient m_sockClient = null;
        private readonly CommandService m_commands = null;
        private readonly IServiceProvider m_services = null;
        private static CancellationTokenSource m_cancellationTokenSource = null;

        #region getters

        internal static BotMain instance = null;
        internal DiscordSocketClient GetSock { get => m_sockClient; }
        internal CommandService GetCommands { get => m_commands; }
        internal IServiceProvider GetService { get => m_services; }

        #endregion getters

        [STAThread]
        private static void Main(string[] args)
        {
            new BotMain().MainAsync(m_cancellationTokenSource.Token).GetAwaiter().GetResult();
            Console.ReadLine();
        }

        private BotMain()
        {
            instance = this;

            m_cancellationTokenSource = new CancellationTokenSource();

            m_sockClient = new DiscordSocketClient(new DiscordSocketConfig()
            {
                LogLevel = LogSeverity.Verbose,
                ExclusiveBulkDelete = true,
                AlwaysDownloadUsers = false,
                DefaultRetryMode = RetryMode.AlwaysFail,
                UseSystemClock = true,
                RateLimitPrecision = RateLimitPrecision.Second,
            });

            m_commands = new CommandService(new CommandServiceConfig()
            {
                LogLevel = LogSeverity.Verbose,
                CaseSensitiveCommands = false,
                IgnoreExtraArgs = true,
                SeparatorChar = ' ',
            });

            m_sockClient.Log += LogAsync;
            m_commands.Log += LogAsync;

            m_services = ConfigureServices();
        }

        ~BotMain()
        {
            m_sockClient.Log -= LogAsync;
            m_commands.Log -= LogAsync;
            m_sockClient.MessageReceived -= HandleCommandAsync;
            m_sockClient.UserJoined -= SockClient_UserJoined;
        }

        public async Task MainAsync(CancellationToken _token)
        {
            await InitCommands();

            await m_sockClient.LoginAsync(TokenType.Bot, "NzAwMDc1MTQ4Nzc0MjExNTk1.XpgZWA.GqbjisMUVQMK127xHzFSLNCj8K4", true);
            await m_sockClient.StartAsync();

            while (!_token.IsCancellationRequested)
            {
                await Task.Delay(500);
            }
        }

        private Task LogAsync(LogMessage msg)
        {
            switch (msg.Severity)
            {
                case LogSeverity.Critical:
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        break;
                    }
                case LogSeverity.Error:
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    }
                case LogSeverity.Warning:
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    }
                case LogSeverity.Info:
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        break;
                    }
                case LogSeverity.Verbose:
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        break;
                    }
                case LogSeverity.Debug:
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                        break;
                    }
                default:
                    break;
            }

            Console.WriteLine($"[{DateTime.Now}]\n" +
                              $"[{msg.Severity}] {msg.Source}: {{ {msg.Message} }} {msg.Exception}\n");
            Console.ResetColor();

            return Task.CompletedTask;
        }

        // If any services require the client, or the CommandService, or something else you keep on hand,
        // pass them as parameters into this method as needed.
        // If this method is getting pretty long, you can seperate it out into another file using partials.
        private static IServiceProvider ConfigureServices()
        {
            var map = new ServiceCollection();
            // Repeat this for all the service classes
            // and other dependencies that your commands might need.
            //.AddSingleton<InfoModule>();

            // When all your required services are in the collection, build the container.
            // Tip: There's an overload taking in a 'validateScopes' bool to make sure
            // you haven't made any mistakes in your dependency graph.
            return map.BuildServiceProvider();
        }

        private async Task InitCommands()
        {
            m_sockClient.MessageReceived += HandleCommandAsync;
            m_sockClient.UserJoined += SockClient_UserJoined;
            await m_commands.AddModulesAsync(Assembly.GetEntryAssembly(), m_services);
        }

        private async Task SockClient_UserJoined(SocketGuildUser usr)
        {
            //member role id: 694987272281653328
            //await usr.AddRoleAsync(null, new RequestOptions() { AuditLogReason = "joined" });
            await Task.CompletedTask;
        }

        private async Task HandleCommandAsync(SocketMessage _msgParam)
        {
            if (!(_msgParam is SocketUserMessage msg)) return;

            if (msg.Author.Id == m_sockClient.CurrentUser.Id || msg.Author.IsBot) return;

            int pos = 0;

            //HasMentionPrefix(m_sockClient.CurrentUser
            if (msg.HasStringPrefix("$", ref pos))
            {
                SocketCommandContext context = new SocketCommandContext(m_sockClient, msg);

                IResult result = await m_commands.ExecuteAsync(context, pos, null);

                if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
                {
                    await msg.Channel.SendMessageAsync(result.ErrorReason);
                }
            }
        }

        internal async Task ShutdownAsync()
        {
            await m_sockClient.StopAsync();
            Console.WriteLine("stopasync done\n");

            Task _logout = m_sockClient.LogoutAsync();
            await _logout;
            Console.WriteLine("Logged out\n");

            await Task.Run(() => m_cancellationTokenSource.Cancel());
            Console.WriteLine("cancellation token triggert\n");
        }
    }
}
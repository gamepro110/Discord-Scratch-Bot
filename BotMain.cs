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
        //Discord_Scratch_Bot_Token
        //Discord_Scratch_Bot_WebhookLink

        private readonly DiscordSocketClient m_sockClient = null;
        private readonly CommandService m_commands = null;
        private readonly IServiceProvider m_services = null;
        private static CancellationTokenSource m_cancellationTokenSource = null;

        private readonly LoggingService m_logging = null;

        internal const string CMDPrefix = "$";

        #region getters

        internal static BotMain instance = null;
        internal DiscordSocketClient GetSock { get => m_sockClient; }
        internal CommandService GetCommands { get => m_commands; }
        internal IServiceProvider GetService { get => m_services; }
        internal LoggingService GetLogging { get => m_logging; }
        internal string WebhookLink { get; }

        #endregion getters

        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                //make the console app run async
                //new BotMain().MainAsync(m_cancellationTokenSource.Token, args[0]).GetAwaiter().GetResult();
                Console.WriteLine(Environment.GetEnvironmentVariable(args[0]) ?? "null :(");
                Console.WriteLine(Environment.GetEnvironmentVariable(args[1]) ?? "null :(");
                Console.ReadLine();
            }
            else
            {
                Console.WriteLine("no args given");
            }
        }

#nullable enable

        private BotMain(string? _webhookLink = null)
        {
            if (string.IsNullOrWhiteSpace(_webhookLink))
            {
                throw new ArgumentException("no webhook found", nameof(_webhookLink));
            }

            instance = this;

            m_cancellationTokenSource = new CancellationTokenSource();

            m_sockClient = new DiscordSocketClient(new DiscordSocketConfig()
            {
                AlwaysDownloadUsers = false,
                DefaultRetryMode = RetryMode.AlwaysFail,
                ExclusiveBulkDelete = true,
                LogLevel = LogSeverity.Verbose,
                RateLimitPrecision = RateLimitPrecision.Second,
                UseSystemClock = true,
            });

            m_commands = new CommandService(new CommandServiceConfig()
            {
                LogLevel = LogSeverity.Verbose,
                CaseSensitiveCommands = false,
                IgnoreExtraArgs = true,
                SeparatorChar = ' ',
            });

            m_logging = new LoggingService(m_sockClient, m_commands, _webhookLink);

            m_services = ConfigureServices();
            WebhookLink = _webhookLink;
        }

#nullable disable

        ~BotMain()
        {
            m_sockClient.MessageReceived -= HandleCommandAsync;
            m_sockClient.UserJoined -= SockClient_UserJoined;
        }

        public async Task MainAsync(CancellationToken _token, string _botVar)
        {
            if (Environment.GetEnvironmentVariable(_botVar) != null)
            {
                await InitCommands();

                await m_sockClient.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable(_botVar), true);
                await m_sockClient.StartAsync();

                while (!_token.IsCancellationRequested)
                {
                    await Task.Delay(500);
                }
            }
            else
            {
                await m_logging.WebTest(WebhookLink, "no bot_var found");
                Console.WriteLine("no bot_var found");
                Console.ReadLine();
            }
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
            //await usr.AddRoleAsync()
            await Task.CompletedTask;
        }

        private async Task HandleCommandAsync(SocketMessage _msgParam)
        {
            if (!(_msgParam is SocketUserMessage msg)) return;

            if (msg.Author.Id == m_sockClient.CurrentUser.Id || msg.Author.IsBot) return;

            int pos = 0;

            //HasMentionPrefix(m_sockClient.CurrentUser
            if (msg.HasStringPrefix(CMDPrefix, ref pos))
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
            await Task.Run(() => m_cancellationTokenSource.Cancel());
            Console.WriteLine("\ncancellation token triggert");

            await m_sockClient.StopAsync();
            Console.WriteLine("\nstopasync done");

            Console.WriteLine("\nLogging out");
            await Task.Run(() => m_sockClient.LogoutAsync());
        }
    }
}
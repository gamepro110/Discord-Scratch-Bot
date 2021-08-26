using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.Extensions.DependencyInjection;
using System.Linq;

namespace ScratchBot
{
    // https://docs.stillu.cc/guides/concepts/logging.html api
    // https://docs.microsoft.com/en-us/dotnet/core/rid-catalog Build info
    // dotnet publish -c Release -r linux-arm

    internal class BotMain
    {
        private readonly DiscordSocketClient m_sockClient = null;
        private readonly CommandService m_commands = null;
        private readonly IServiceProvider m_services = null;
        private static CancellationTokenSource m_cancellationTokenSource = null;
        private readonly LoggingService m_logging = null;
        private Dictionary<Task<IResult>, SocketUserMessage> _Tasks;

        internal const string CMDPrefix = "$";

        #region getters

        internal static BotMain instance = null;
        internal DiscordSocketClient GetSock { get => m_sockClient; }
        internal CommandService GetCommands { get => m_commands; }
        internal IServiceProvider GetService { get => m_services; }
        internal LoggingService GetLogging { get => m_logging; }

        private static string m_webhook = null;
        internal static string WebhookLink { get => m_webhook; }

        #endregion getters

        [STAThread]
        private static void Main()
        {
            string[] args = File.ReadAllLines("token.tkn");

            if (args.Length == 2)
            {
                m_webhook = args[1];
                //make the console app run async
                new BotMain()
                    .MainAsync(args[0], m_cancellationTokenSource.Token)
                    .GetAwaiter()
                    .GetResult();
            }
            else
            {
                FileLogger.LogToFile(
                    "Invalid Args Given.\n" +
                    "expected:\n" +
                    "token\n" +
                    "webhook link"
                    );
            }
        }

        private BotMain()
        {
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

            m_logging = new LoggingService(m_sockClient, m_commands);

            m_services = ConfigureServices();

            _Tasks = new();
        }

        ~BotMain()
        {
            m_sockClient.MessageReceived -= HandleCommandAsync;
        }

        public async Task MainAsync(string _authenticatonToken, CancellationToken _token)
        {
            if (_authenticatonToken != null)
            {
                await InitCommands();

                await m_sockClient.LoginAsync(TokenType.Bot, _authenticatonToken, true);
                await m_sockClient.StartAsync();

                while (!_token.IsCancellationRequested)
                {
                    if (_Tasks.Count > 0)
                    {
                        Task<IResult> finishedTask = await Task.WhenAny(_Tasks.Keys.ToArray());

                        if (!finishedTask.Result.IsSuccess && finishedTask.Result.Error != CommandError.UnknownCommand)
                        {
                            await Task.Run(() => _Tasks[finishedTask].Channel.SendMessageAsync(finishedTask.Result.ErrorReason));
                        }
                        _Tasks.Remove(finishedTask);
                    }
                    await Task.Delay(900, _token);
                }
            }
            else
            {
                await LoggingService.WebTest("no bot_var found");
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
            await m_commands.AddModulesAsync(Assembly.GetEntryAssembly(), m_services);
        }

        private async Task HandleCommandAsync(SocketMessage _msgParam)
        {
            if (_msgParam is not SocketUserMessage msg) return;

            if (msg.Author.Id == m_sockClient.CurrentUser.Id || msg.Author.IsBot) return;

            int pos = 0;

            if (msg.HasStringPrefix(CMDPrefix, ref pos, StringComparison.OrdinalIgnoreCase))
            {
                SocketCommandContext context = new(m_sockClient, msg);
                _Tasks.Add(m_commands.ExecuteAsync(context, pos, null), msg);
            }

            await Task.Delay(1);
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
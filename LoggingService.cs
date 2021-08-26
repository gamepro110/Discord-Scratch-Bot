using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ScratchBot
{
    internal class LoggingService
    {
        public LoggingService(DiscordSocketClient _client, CommandService _command)
        {
            _client.Log += LogAsync;
            _command.Log += LogAsync;
        }

        ~LoggingService()
        {
            BotMain.instance.GetSock.Log -= LogAsync;
            BotMain.instance.GetCommands.Log -= LogAsync;
        }

        private async Task LogAsync(LogMessage message)
        {
            bool _doWebhookLog = false;
            bool _doFileLog = false;
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                    {
                        _doWebhookLog = true;
                        _doFileLog = true;
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        break;
                    }
                case LogSeverity.Error:
                    {
                        _doWebhookLog = true;
                        _doFileLog = true;
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    }
                case LogSeverity.Warning:
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
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

            string _msg;

            if (message.Exception is CommandException _cmdEX)
            {
                _msg = $"\n[Command/{message.Severity}] {_cmdEX.Command.Aliases[0]}\n" +
                                  $"Failed to execute in {_cmdEX.Context.Channel}.\n {_cmdEX}";
                Console.WriteLine(_msg);
            }
            else
            {
                _msg = $"\n[General/{message.Severity}] ({message})";
                Console.WriteLine(_msg);
            }

            if (_doWebhookLog)
            {
                await WebhookLog(_msg);
            }

            if (_doFileLog)
            {
                FileLogger.LogToFile(_msg);
            }

            Console.ResetColor();
            await Task.CompletedTask;
        }

        public static Task WebTest(string _msg) => WebhookLog(_msg);

        private static Task WebhookLog(string _msg)
        {
            if (!string.IsNullOrEmpty(BotMain.WebhookLink))
            {
                using WebClient _client = new();
                NameValueCollection _data = new()
                {
                    { "username", "ScratchBot_WebHook" },
                    { "content", _msg },
                };

                byte[] _outp = _client.UploadValues(BotMain.WebhookLink, _data);

                if (Encoding.UTF8.GetString(_outp) == string.Empty)
                {
                    return Task.CompletedTask;
                }
                else
                {
                    FileLogger.LogToFile(Encoding.UTF8.GetString(_outp));
                    return Task.Delay(3);
                }
            }
            else
            {
                FileLogger.LogToFile("Failed to send webhook msg due to not having the environment var set up.");
                return Task.CompletedTask;
            }
        }
    }
}
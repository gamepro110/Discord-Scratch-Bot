using Discord;
using Discord.Commands;
using Discord.Webhook;
using Discord.WebSocket;
using System;
using System.Collections.Specialized;
using System.IO;
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
            switch (message.Severity)
            {
                case LogSeverity.Critical:
                    {
                        _doWebhookLog = true;
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        break;
                    }
                case LogSeverity.Error:
                    {
                        _doWebhookLog = true;
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

            string _msg = string.Empty;

            if (message.Exception is CommandException _cmdEX)
            {
                _msg = $"\n[Command/{message.Severity}] {_cmdEX.Command.Aliases.First()}\n" +
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

            Console.ResetColor();
            await Task.CompletedTask;
        }

        private Task WebhookLog(string _msg)
        {
            using (WebClient _client = new WebClient())
            {
                NameValueCollection _data = new NameValueCollection
                {
                    {"username", "ScratchBotWebHook"},
                    {"content", _msg},
                };

                byte[] _outp = _client.UploadValues("https://discordapp.com/api/webhooks/701783478521299067/yZQZZ1gBKY27kEMbCsxs_8-lVV9Rjm3B1gtPnILu53bkqKtMb_UeJcuMKp4OQkYzDlQY", _data);

                if (Encoding.UTF8.GetString(_outp) == string.Empty)
                {
                    return Task.CompletedTask;
                }
                else
                {
                    File.WriteAllText($"{Environment.CurrentDirectory}/A__log.txt", Encoding.UTF8.GetString(_outp));
                    return Task.Delay(3);
                }
            }
        }
    }
}
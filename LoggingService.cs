using Discord;
using Discord.Commands;
using Discord.Webhook;
using Discord.WebSocket;
using System;
using System.Linq;
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

        private Task LogAsync(LogMessage message)
        {
            switch (message.Severity)
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

            if (message.Exception is CommandException _cmdEX)
            {
                Console.WriteLine($"\n[Command/{message.Severity}] {_cmdEX.Command.Aliases.First()}\n" +
                                  $"Failed to execute in {_cmdEX.Context.Channel}.\n {_cmdEX}");
            }
            else
            {
                Console.WriteLine($"\n[General/{message.Severity}] ({message})");
            }

            Console.ResetColor();
            return Task.CompletedTask;
        }
    }
}
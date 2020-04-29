using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

namespace ScratchBot
{
    public class Commands : ModuleBase<SocketCommandContext>
    {
        [Command("no")]
        public async Task No()
        {
            await ReplyAsync("awh :crying_cat_face:");
        }

        #region Help

        [Command("help")]
        [Summary("Displays All Commands")]
        public async Task Help(string _name = "")
        {
            EmbedBuilder _embed = new EmbedBuilder();
            ModuleInfo _mod;
            if (_name == "")
            {
                _mod = BotMain.instance.GetCommands.Modules.FirstOrDefault(m => m.Name.Replace("Module", "").ToLower() != "");
                if (_mod == null)
                {
                    await ReplyAsync("bot confussion...");
                    return;
                }

                _embed.Description = $"{_mod.Summary}\n" +
                                     (!string.IsNullOrEmpty(_mod.Remarks) ? $"{_mod.Remarks}" : $"") +
                                     (_mod.Submodules.Any() ? $"Sub mods: {string.Join(", ", _mod.Submodules.Select(m => m.Name))}" : "");
            }
            else
            {
                _mod = BotMain.instance.GetCommands.Modules.FirstOrDefault(m => m.Name.Replace("Module", "").ToLower() == _name.ToLower());
                if (_mod == null)
                {
                    await ReplyAsync("No mod with that name was found...");
                    return;
                }

                _embed.Description = (_mod.Submodules.Any() ? $"Sub mods: {string.Join(", ", BotMain.CMDPrefix + _mod.Submodules.Select(m => m.Name))}" : "");
            }

            _embed.Title = _mod.Name;
            _embed.Color = Color.DarkBlue;

            AddCommands(_mod, ref _embed);

            await ReplyAsync(embed: _embed.Build());
        }

        private void AddCommands(ModuleInfo mod, ref EmbedBuilder embed)
        {
            foreach (CommandInfo item in mod.Commands)
            {
                item.CheckPreconditionsAsync(Context, BotMain.instance.GetService).GetAwaiter().GetResult();
                AddCommand(item, ref embed);
            }
        }

        private void AddCommand(CommandInfo info, ref EmbedBuilder embed)
        {
            embed.AddField(f =>
            {
                f.Name = $"__**{info.Name}**__";
                f.Value = $"**how to use** `{BotMain.CMDPrefix}{info.Aliases[0]}`\n" +
                $"{info.Summary}\n" +
                (!string.IsNullOrEmpty(info.Remarks) ? $"{info.Remarks}\n" : "");
            });
        }

        #endregion Help

        #region roll

        [Group("Roll")]
        public class Roll : ModuleBase<SocketCommandContext>
        {
            private enum Die
            {
                D100 = 100,
                D20 = 20,
                D12 = 12,
                D10 = 10,
                D8 = 8,
                D6 = 6,
                D4 = 4,
            }

            private Die _die = Die.D20;
            private int _dieRolled = 0;
            private Color _col = new Color();
            private int _roll = 0;

            private EmbedBuilder _em = new EmbedBuilder();

            private async Task DieMSG(Die die)
            {
                _dieRolled = (int)die;
                _roll = await GetRandomDieValue(_dieRolled);
                _col = await GetColor(_roll);

                _em.Title = $"{Context.User.Username} Rolled a D{_dieRolled}";
                _em.Color = _col;
                _em.Description = $"```cs\n" +
                                  $"{{{_roll}}}\n" +
                                  $"```";

                await ReplyAsync(embed: _em.Build());
            }

            private Task<Color> GetColor(int _rolledValue)
            {
                if (_rolledValue == 1)
                {
                    return Task.FromResult(Color.Red);
                }
                else if (_rolledValue < _dieRolled / 2)
                {
                    return Task.FromResult(Color.Orange);
                }

                return Task.FromResult(Color.Teal);
            }

            private Task<int> GetRandomDieValue(int num = 4)
            {
                num += 1;
                num *= 100;
                num -= 51;
                Random rand = new Random();
                num = rand.Next(100, num);
                num /= 100;
                return Task.FromResult(num);
            }

            [Command("D100")]
            public async Task D100()
            {
                _die = Die.D100;
                await DieMSG(_die);
            }

            [Command("D20")]
            public async Task D20()
            {
                _die = Die.D20;
                await DieMSG(_die);
            }

            [Command("D12")]
            public async Task D12()
            {
                _die = Die.D12;
                await DieMSG(_die);
            }

            [Command("D10")]
            public async Task D10()
            {
                _die = Die.D10;
                await DieMSG(_die);
            }

            [Command("D8")]
            public async Task D8()
            {
                _die = Die.D8;
                await DieMSG(_die);
            }

            [Command("D6")]
            public async Task D6()
            {
                _die = Die.D6;
                await DieMSG(_die);
            }

            [Command("D4")]
            public async Task D4()
            {
                _die = Die.D4;
                await DieMSG(_die);
            }
        }

        #endregion roll

        #region sudo

        [Group("sudo")]
        [RequireOwner(ErrorMessage = "yo aint no owner")]
        public class Sudo : ModuleBase<SocketCommandContext>
        {
            #region ping

            [Group("Ping")]
            public class PingRequest : ModuleBase<SocketCommandContext>
            {
                private EmbedBuilder m_embed = new EmbedBuilder();

                [Command("bot"), RequireOwner(Group = "owner")]
                [Summary("Bot latency ping")]
                public async Task BotPing() // Gets the estimated round-trip latency, in milliseconds, to the gateway server
                {
                    m_embed.Title = "bot latency";
                    m_embed.Description = $"pinged ({BotMain.instance.GetSock.Latency} ms)";
                    m_embed.Footer = new EmbedFooterBuilder()
                    {
                        Text = "Gets the estimated round-trip latency, in milliseconds, to the gateway server."
                    };
                    await ReplyAsync(embed: m_embed.Build());
                }

                [Command("web"), RequireOwner(Group = "owner")]
                [Summary("web latency ping")]
                public async Task WebPing()
                { // https://stackoverflow.com/questions/1281176/making-a-ping-inside-of-my-c-sharp-application
                    decimal _totaltime = 0;
                    int _timeout = 120;
                    Ping pingsender = new Ping();

                    for (int i = 0; i < 4; i++)
                    {
                        PingReply reply = await pingsender.SendPingAsync("www.google.com", _timeout);
                        if (reply.Status == IPStatus.Success)
                        {
                            _totaltime += reply.RoundtripTime;
                        }
                    }
                    _totaltime /= 4;

                    m_embed.Title = "web latency";
                    m_embed.Description = $"web ping\n {{{_totaltime} ms}}";
                    await ReplyAsync(embed: m_embed.Build());
                }
            }

            #endregion ping

            [Command("purge")]
            [Summary("Cleanup x messages. (default = 10)")]
            [RequireUserPermission(ChannelPermission.ManageMessages), RequireBotPermission(ChannelPermission.ManageMessages)]
            public async Task PurgeMessages(int _amount = 10)
            {
                EmbedBuilder _em = new EmbedBuilder();

                if (_amount <= 0)
                {
                    _em.Color = Color.DarkBlue;
                    _em.Title = "cant delete anything if you dont give me an amount to delete...";
                }
                else
                {
                    IEnumerable<IMessage> _messages = await Context.Channel.GetMessagesAsync(Context.Message, Direction.Before, _amount).FlattenAsync();
                    IEnumerable<IMessage> _filteredMessages = _messages.Where(x => (DateTimeOffset.UtcNow - x.Timestamp).TotalDays <= 14); // trying to bulk delete messages older than 14 days will result in a bad request!!
                    int _filteredCount = _filteredMessages.Count();

                    if (_filteredCount == 0)
                    {
                        _em.Color = Color.DarkBlue;
                        _em.Title = "got nothing to delete tho...";
                    }
                    else
                    {
                        await (Context.Channel as ITextChannel).DeleteMessagesAsync(_filteredMessages);

                        _em.Color = Color.Green;
                        _em.Title = "did thing. you proud??";
                        _em.Description = $"deleted {_filteredCount} {(_filteredCount > 1 ? "messages" : "message")}.";
                    }
                }

                await ReplyAsync(embed: _em.Build());
            }

            [Command("say"), RequireOwner(ErrorMessage = "wrong role mate", Group = "owner")]
            [Summary("Echoes a message.")]
            [Remarks("???")]
            public Task SayAsync([Remainder] [Summary("The text to echo")] string echo) => ReplyAsync(echo);

            [Command("exit"), RequireOwner()]
            public async Task Exit()
            {
                await ReplyAsync("key bye");
                await BotMain.instance.ShutdownAsync();
            }
        }

        #endregion sudo
    }
}
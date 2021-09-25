using Koubot.SDK.API;
using Koubot.SDK.Models.Entities;
using Koubot.SDK.PluginInterface;
using Koubot.Shared.Interface;
using Koubot.Shared.Models;
using Koubot.Shared.Protocol;
using Koubot.Shared.Protocol.Event;
using Koubot.Tool.Extensions;
using Koubot.Tool.General;
using Koubot.Tool.Random;
using Koubot.Tool.Web.RateLimiter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.KouEnum;

namespace KouFunctionPlugin
{
    [KouPluginClass(
        "poke",
        "戳一戳",
        Author = "7zou")]
    public class KouTickle : KouPlugin<KouTickle>,
        IWantKouPlatformUser,
        IWantKouGlobalConfig,
        IWantCommandLifeKouContext,
        IWantKouPlatformGroup
    {

        private static readonly List<string> _thirdDescription = new()
        {
            "这就算了",
            "可还行？看看下面的",
            "嗯。。。",
            "还可以"
        };
        private static readonly List<string> _secondDescription = new()
        {
            "有够闲得啊",
            "这还不算什么，还有更离谱的！",
            "说什么好呢",
            "挺闲的呢",
            "戳出了好几个好"
        };
        private static readonly List<string> _firstDescription = new()
        {
            "也太几把闲的慌了，建议多戳戳自己的肚皮",
            "就那么喜欢听我骂你吗",
            "建议戳纵连也这样戳噢",
            "无语了呢..."
        };

        private static readonly List<List<string>> _descriptionList = new()
        {
            _firstDescription,
            _secondDescription,
            _thirdDescription
        };

        private static readonly KouColdDown<PlatformGroup> _rankCD = new();
        [KouPluginFunction(
            Name = "本群戳一戳情况",
            ActivateKeyword = "rank",
            Help = "得到当前群的戳一戳次数排行",
            OnlyUsefulInGroup = true)]
        public object Rank()
        {
            if (_rankCD.IsInCd(CurrentPlatformGroup, new TimeSpan(0, 5, 0)))
                return "在冷却中噢~ (5min)";
            if (!_pokeInfoDict.ContainsKey(CurrentPlatformGroup)) return $"自从我上次重启以来({KouGlobalConfig.SystemStartTime})，居然没发现有人无聊到玩戳一戳诶";
            var groupInfoDict = _pokeInfoDict[CurrentPlatformGroup];
            if (groupInfoDict.Count == 1)
            {
                var info = groupInfoDict.First();
                return $"自从我上次重启以来({KouGlobalConfig.SystemStartTime})，居然只有这个人，{info.Key.Name}，" +
                       $"戳了我{info.Value.TimesOfPokeBot}下，戳了别人{info.Value.TimesOfPokeOthers}下";
            }
            if (groupInfoDict.Count == 2)
            {
                string reply = $"自从我上次重启以来({KouGlobalConfig.SystemStartTime})，只有两个人：\n";
                foreach (var info in groupInfoDict)
                {
                    reply += $"{info.Key.Name}，" +
                           $"戳了我{info.Value.TimesOfPokeBot}下，戳了别人{info.Value.TimesOfPokeOthers}下\n";
                }

                return reply.TrimEnd();
            }
            string prologue = $"接下来公布一下自本Kou上次醒来({KouGlobalConfig.SystemStartTime})，本群最闲着没事干玩戳一戳的人";
            CurrentPlatformGroup.SendGroupMessage(prologue);
            Thread.Sleep(2000);
            var list = groupInfoDict.OrderByDescending(p => p.Value.Total)
                .ToList();
            for (int i = 2; i >= 0; i--)
            {
                var info = list[i];
                string reply = "";
                switch (i)
                {
                    case 2:
                        reply += "第三名";
                        break;
                    case 1:
                        reply += "第二名";
                        break;
                    case 0:
                        reply += $"{(info.Value.Total > list[i + 1].Value.Total + 20 ? "最几把离谱的" : null)}第一名";
                        break;
                }

                reply += $"，{info.Key.Name}，" +
                         $"一共{info.Value.TimesOfPokeBot.BeIfNotDefault($"戳了我{info.Value.TimesOfPokeBot}次，")}" +
                         $"{info.Value.TimesOfPokeOthers.BeIfNotDefault($"戳了其他人{info.Value.TimesOfPokeOthers}次，")}" +
                         $"{_descriptionList[i].RandomGetOne()}";
                CurrentPlatformGroup.SendGroupMessage(reply);
                Thread.Sleep(RandomTool.GenerateRandomInt(1500, 3500));
            }
            return null;
        }
        [KouPluginFunction(Name = "帮助")]
        public override object Default(string str = null)
        {
            return ReturnHelp();
        }

        class PokeInfo
        {
            public int TimesOfPokeBot;
            public int TimesOfPokeOthers;
            public int Total => TimesOfPokeBot + TimesOfPokeOthers;
        }
        /// <summary>
        /// 单群用户戳一戳记录表
        /// </summary>
        private static readonly Dictionary<PlatformGroup, Dictionary<PlatformUser, PokeInfo>> _pokeInfoDict =
            new();


        [KouPluginEventHandler]
        public override KouEventHandlerResult OnReceiveTickleEvent(TickleEventArgs e)
        {
            if (e.FromGroup == null || e.TargetUser == null || e.FromUser == null) return null;
            if (!_pokeInfoDict.ContainsKey(e.FromGroup))
            {
                _pokeInfoDict.TryAdd(e.FromGroup, new Dictionary<PlatformUser, PokeInfo>());
            }

            var curGroupDict = _pokeInfoDict[e.FromGroup];
            if (!curGroupDict.ContainsKey(e.FromUser))
            {
                curGroupDict.TryAdd(e.FromUser, new PokeInfo());
            }

            var curUserPokeInfo = curGroupDict[e.FromUser];
            if (e.TargetUser != e.BotAccount)//记录用户poke信息
            {
                Interlocked.Increment(ref curUserPokeInfo.TimesOfPokeOthers);
                return null;
            }

            Interlocked.Increment(ref curUserPokeInfo.TimesOfPokeBot);
            if (0.1.ProbablyTrue()) return null;//90%概率触发
            using (var limiter = new LeakyBucketRateLimiter(nameof(KouTickle), 1))
            {
                if (!limiter.TryRequestOnce()) return null;
            }
            var list = TickleReply.GetAutoModelCache();
            var reply = list.RandomGetOne()?.Reply;
            return reply;
        }

        [KouPluginFunction(Name = "遗忘", ActivateKeyword = "del|delete", Help = "删除学习过的戳一戳反馈")]
        public string DeleteItem([KouPluginArgument(Name = "戳一戳ID")] List<int> id)
        {
            var result = new StringBuilder();
            foreach (var i in id)
            {
                var reply = TickleReply.SingleOrDefault(a => a.ID == i);
                if (reply == null) result.Append($"\n不记得ID{i}");
                else if (reply.SourceUser != null && reply.SourceUser != CurrentPlatformUser &&
                         !CurrentPlatformUser.HasTheAuthority(Authority.BotManager))
                    result.Append($"\nID{i}是别人贡献的，不可以删噢");
                else
                {
                    result.Append($"\n忘记了{reply.ToString(FormatType.Brief)}");
                    reply.DeleteThis();
                };
            }

            return result.ToString().TrimStart();
        }

        [KouPluginFunction(Help = "教Kou戳一戳怎么回应", Name = "教教", ActivateKeyword = "add", EnableAutoNext = true)]
        public string AddItem(
            [KouPluginArgument(Name = "回应内容")]
            string reply)
        {
            if (reply.IsNullOrWhiteSpace()) return "好好教我嘛";
            var success = TickleReply.Add(almanac =>
            {
                almanac.Reply = reply;
                almanac.SourceUser = CurrentPlatformUser.FindThis(KouContext);
            }, out var added, out var error, KouContext);
            if (success)
            {
                var reward = RandomTool.GenerateRandomInt(1, 2);
                CurrentPlatformUser.KouUser.GainCoinFree(reward);
                return $"学会了，别人戳我我就：\n{added.ToString(FormatType.Brief)}\n" +
                       $"[您获得了{CurrentKouGlobalConfig.CoinFormat(reward)}!]";
            }
            return $"没学会，就突然：{error}";
        }

        public KouGlobalConfig CurrentKouGlobalConfig { get; set; }
        public KouContext KouContext { get; set; }
        public PlatformUser CurrentPlatformUser { get; set; }
        public PlatformGroup CurrentPlatformGroup { get; set; }
    }
}

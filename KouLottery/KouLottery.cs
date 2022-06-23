using Koubot.SDK.API;
using Koubot.SDK.PluginInterface;
using Koubot.SDK.System;
using Koubot.Tool.Extensions;
using Koubot.Tool.KouData;
using Koubot.Tool.Math;
using Koubot.Tool.Random;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Koubot.SDK.PluginExtension;
using Koubot.Shared.Models;
using Koubot.Shared.Models.Calendar;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.KouEnum;

namespace KouFunctionPlugin
{
    [KouPluginClass("lot|抽奖", "抽奖机",
        Author = "7zou",
        Authority = Authority.NormalUser,
        Introduction = "抽奖机",
        PluginType = PluginType.Function)]
    public class KouLottery : KouPlugin<KouLottery>, IWantPluginGlobalConfig<LotteryConfig>
    {
        [KouPluginParameter(ActivateKeyword = "count|c", Name = "抽签数量", Help = "范围在1-100",
            Min = 1, Max = 100)]
        public int Count { get; set; } = 1;

        [KouPluginParameter(ActivateKeyword = "可重复|r", Name = "可重复", Help = "指示能否重复中同一个签")]
        public bool CanRepeat { get; set; }

        private static double chanceBonus;

        private static void RefreshChanceBonus()
        {
            var today = CalendarData.GetToday();
            var count = today.HappyFestivalCount();
            chanceBonus = RandomTool.GenerateRandomDouble(1, 1.5, DateTime.Now.Date);
            if(count > 0 ) chanceBonus *= 3 * count;
        }
        static KouLottery()
        {
            RefreshChanceBonus();
            AddCronTab(new TimeSpan(1,0,0), () =>
            {
                RefreshChanceBonus();
                _hasTookPartInSet.Clear();
            });

            PluginEventList.FetchFestivalDesc += (sender) =>
            {
                if (sender == null) return null;
                if (sender.CurGroup == null || sender.CurGroup.HasInstallPlugin(GetPluginMetadataStatic().Info))
                {
                    return $"{sender.CurKouGlobalConfig.FreeCoinName}池{chanceBonus:0.##}倍概率";
                }
                return null;
            };
        }

        [KouPluginFunction(Name = "当前群自由抽签", ActivateKeyword = "抽奖会场",
            Help = "当前群所有回复1的人加入抽奖")]
        public object CurrentGroup([KouPluginArgument(Name = "抽取个数（默认一个）")] int count = 1)
        {
            return "施工中";
            var message = SessionService.AskGroup($"开始抽奖啦，抽取{count}个人，回复1加入抽签", setting =>
            {
                setting.Attribute = KouSessionSetting.SessionAttribute.CircularSession;
            });
            return null;
        }

        private static readonly List<string> rejectList = new()
        {
            "Kou建议你选择睡觉",
            "不想选",
            "你猜我选的什么？",
            "不选！",
            "我才不选！",
            "嗯...让我也犹豫一下",
            "好难，选不出来",
            "小孩子才做选择，我全都要！",
            "Kou建议你选择我",
            "Kou建议你都不选",
            "Kou建议你去学习"
        };
        private static readonly List<string> verbList = new()
        {
            "打",
            "整",
            "吃",
            "看",
            "弄",
            "搞",
            "摸摸",
            "瞅瞅"
        };

        private static readonly List<string> prepList = new()
        {
            "选择",
            "选",
            "去"
        };
        private static readonly List<string> kouThinkList = new()
        {
            "Kou认为",
            "Kou觉得",
            "我认为",
            "我觉得"
        };
        private static readonly List<string> maybeList = new()
        {
            "大概",
            "也许",
            "兴许",
            "或许",
            "可能",
            "很可能",
            "大致",
            "好像",
            "大约",
            "大抵",
            "约",
            "应该",
            ""
        };
        private static readonly List<string> speakList = new()
        {
            "Kou建议你选择：{0}",
            "Kou建议你{1}：{0}",
            "Kou会{1}：{0}",
            "Kou才不会{1}{0}",
            "Kou帮你选了：{0}",
            "Kou认为你该：{0}",
            "我觉得你该{1}{0}",
            "我猜的没错的话，你其实想{1}{0}，对吧？",
            "反正我才不会{1}{0}！",
            "{1}{0}的话还不如去睡觉",
            "我觉得{1}{0}也许不错",
            "{1}{0}，不好别怪我555",
            "{0}",
            "不如{1}{0}吧？",
            "Kou抽出了：{0}"
        };



        [KouPluginFunction(ActivateKeyword = "pool status", Name = "硬币池状态")]
        public object? CoinPoolStatus()
        {
            var config = this.GlobalConfig();
            return config.GetStatus(CurKouGlobalConfig);
        }

        private static readonly ReaderWriterLockSlim _coinPoolLock = new();
        private static readonly HashSet<UserAccount> _hasTookPartInSet = new();
        [KouPluginFunction(ActivateKeyword = "pool", Name = "硬币池", NeedCoin = -1, Help = "最低有0.3%的概率获取池内所有硬币（与当天运势值也有关系）\n每次产生幸运儿后，池中随机产生300-1000枚硬币")]
        public object PlayCoinPool([KouPluginArgument(Name = "投放硬币数 最少5")]string? coinStr = null)
        {
            coinStr ??= "5";
            var coin = 0;
            bool wholeCoins = false;
            if (coinStr.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                wholeCoins = true;
                coin = CurKouUser.CoinFree;
                if (coin < 5) coin = 5;
            }
            else if (!TypeService.TryConvert(coinStr, out coin, CurCommand, s => s.Min = 5))
            {
                return "请输入正确的投放数量";
            }
            if (_hasTookPartInSet.Contains(CurKouUser)) return $"{CurKouUserNickname}今天已经参加过啦";
            if (!CurKouUser.ConsumeCoinFree(coin))
            {
                return FormatNotEnoughCoin(coin);
            }

            _hasTookPartInSet.Add(CurKouUser);
            var config = this.GlobalConfig();
            var times = Math.Log(coin - 2, 3).Ceiling();
            var luckValue = CurKouUser.LuckValue();
            var luckAppend = RandomTool.DistributeUsePowerFunction(luckValue, 0.005, 5);
            var chance = luckAppend + 0.003;
            chance *= chanceBonus;

            int i = 0;
            bool success = false;
            int atLastCoins;
            _coinPoolLock.EnterWriteLock();
            try
            {
                if (config.PoolTotalCoins == 0) config.PoolTotalCoins = RandomTool.GenerateRandomInt(300, 1000);
                config.PoolTotalCoins += coin;
                atLastCoins = config.PoolTotalCoins;
                for (; i < times; i++)
                {
                    if (chance.ProbablyTrue())
                    {
                        success = true;
                    }
                }
                if (success)
                {
                    config.SuccessPeopleCount++;
                    config.LastBonusUserID = CurKouUser.Id;
                    config.LastBonusTime = DateTime.Now;
                    config.LastBonusCoins = config.PoolTotalCoins;
                    config.PoolTotalCoins = RandomTool.GenerateRandomInt(100, 1000);
                }
                config.SaveChanges();
            }
            finally
            {
                _coinPoolLock.ExitWriteLock();
            }
            
            Reply($"{CurKouUserNickname}投入{(wholeCoins ?"全部身家":"")}{CurKouGlobalConfig.CoinFormat(coin)}，{"叮铃".Repeat(times.LimitInRange(10))}...");
            Thread.Sleep((RandomTool.GenerateRandomInt(1000,2000) * times).LimitInRange(5000));
            if (success)
            {
                CurKouUser.GainCoinFree(atLastCoins);
                return $"恭喜{CurKouUserNickname}暴富！！获得了池中所有的{CurKouGlobalConfig.CoinFormat(atLastCoins)}，" +
                       $"是第{config.SuccessPeopleCount}个搬空池子的人！{CurKouUserNickname}当前有{CurKouGlobalConfig.CoinFormat(CurKouUser.CoinFree)}";
            }

            return $"{CurKouUserNickname}失败了呢，下次再来吧\n{config.GetCoinPoolInfo(CurKouGlobalConfig)}";
        }


        [KouPluginFunction(
            ActivateKeyword = "选择",
            Name = "帮忙随机选择",
            Help = "会从给的几个选项中随机选择",
            SupportedParameters = new[] { nameof(Count), nameof(CanRepeat) })]
        public string DrawCustomLottery(
            [KouPluginArgument(
                Name = "自定义签",
                ArgumentAttributes = KouParameterAttribute.AllowDuplicate,
                SplitChar = " ")]List<string> lotList)
        {
            StringBuilder result = new StringBuilder();
            if (CanRepeat)
            {
                for (int i = 0; i < Count; i++)
                {
                    result.Append(lotList.RandomGetOne() + "、");
                }
            }
            else result.Append(lotList.RandomGet(Count).ToStringJoin("、"));

            string verb = prepList.RandomGetOne() + (KouStaticData.Verb.Any(s => result.ToString().StartsWith(s)) ?
                null : verbList.ProbablyDo(0.35)?.RandomGetOne());
            return string.Format(speakList.RandomGetOne(), result.ToString().TrimEnd('、'), verb)
                .ProbablyBe(rejectList.RandomGetOne(), 0.05);
        }
        /// <summary>
        /// 算出事情发生概率
        /// </summary>
        /// <param name="eventName"></param>
        /// <returns></returns>
        private string RateFortuneTelling(string eventName)
        {
            if (eventName.IsNullOrWhiteSpace() || eventName == "的") return null;
            if (eventName.SatisfyAny(s => s.StartsWith("俺"), s => s.StartsWith("我")))
            {
                eventName = eventName.Substring(1);
                eventName = "你".ProbablyBe("您", 0.31) + eventName;
            }
            var interval = new IntervalDoublePair(0, 1);
            var probability = interval.GenerateRandomDouble();
            if (CurMessage.ProbablyDo(0.1) != null)
            {
                CurMessage.ReplyMessage("嗯...让我算一算");
                Thread.Sleep((int)(probability * 3333));
            }

            return $"{kouThinkList.RandomGetOne()}{eventName}概率{maybeList.RandomGetOne()}有{probability:P}";
        }

        [KouPluginFunction(ActivateKeyword = "抽号码|roll", Name = "随机抽取号码",
            Help = "从给定的区间随机选取整数字（给单个数则是从1-给定数字的范围）（默认是1-100）", SupportedParameters = new[] { nameof(Count), nameof(CanRepeat) })]
        public string RandomNumber(string intervalOrStr = null)
        {
            if (!IntervalDoublePair.TryGetIntervalDoublePair(intervalOrStr, out var interval, true))
            {
                if (intervalOrStr?.EndsWith("概率") == true)
                    return RateFortuneTelling(intervalOrStr.Remove(intervalOrStr.Length - 2, 2));
                interval = new IntervalDoublePair(1, 100);//默认是1-100区间抽取号码
            }
            if (Math.Abs(interval.RightInterval.Value - interval.LeftInterval.Value) < 0.0001 && interval.LeftInterval.Value > 1)
            {
                interval.LeftInterval.Value = 1;//如果是输入了单个数的，就是从1-指定数
            }

            Count = Count.LimitInRange(1, 100);
            string result = "Kou抽出了：";
            if (CanRepeat)
            {
                for (int i = 0; i < Count; i++)
                {
                    result += interval.GenerateRandomInt() + "、";
                }
            }
            else
            {
                int minValue = interval.GetLeftIntervalNearestNumber();
                int maxValue = interval.GetRightIntervalNearestNumber();
                var generator = new LotteryGenerator(Count, minValue, maxValue);
                return result + generator.DrawLottery().ToStringJoin("、");
            }
            return result.TrimEnd('、');
        }
    }
}

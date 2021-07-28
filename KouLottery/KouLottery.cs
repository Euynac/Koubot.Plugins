using Koubot.SDK.Interface;
using Koubot.SDK.Models.System;
using Koubot.SDK.Protocol;
using Koubot.SDK.Protocol.Plugin;
using Koubot.SDK.Services.Interface;
using Koubot.Tool.KouData;
using Koubot.Tool.Math;
using Koubot.Tool.Random;
using Koubot.Tool.String;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Koubot.SDK.API;
using Koubot.Tool.Extensions;

namespace KouFunctionPlugin
{
    [KouPluginClass("lot|抽奖", "抽奖机",
        Author = "7zou",
        Authority = KouEnum.Authority.NormalUser,
        Introduction = "抽奖机",
        PluginType = KouEnum.PluginType.Function)]
    public class KouLottery : KouPlugin<KouLottery>, IWantKouMessage, IWantKouSession
    {
        [KouPluginParameter(ActivateKeyword = "count|c", Name = "抽签数量", Help = "范围在1-100",
            NumberMin = 1, NumberMax = 100)]
        public int Count { get; set; } = 1;

        [KouPluginParameter(ActivateKeyword = "可重复|r", Name = "可重复", Help = "指示能否重复中同一个签")]
        public bool CanRepeat { get; set; }

        [KouPluginFunction(Name = "帮助")]
        public override object Default(string str = null)
        {
            return ReturnHelp();
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
            "Kou认为","Kou觉得","我认为","我觉得"
        };
        private static readonly List<string> maybeList = new()
        {
            "大概","也许","兴许","或许","可能","很可能","大致","好像","大约","大抵","约","应该",""
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


        [KouPluginFunction(
            ActivateKeyword = "选择",
            Name = "帮忙随机选择",
            Help = "会从给的几个选项中随机选择",
            SupportedParameters = new[] { nameof(Count), nameof(CanRepeat) })]
        public string DrawCustomLottery(
            [KouPluginArgument(
                Name = "自定义签",
                ArgumentAttributes = KouEnum.KouParameterAttribute.AllowDuplicate,
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
            if (Message.ProbablyDo(0.1) != null)
            {
                Message.ReplyMessage("嗯...让我算一算");
                Thread.Sleep((int)(probability * 3333));
            }
            
            return $"{kouThinkList.RandomGetOne()}{eventName}概率{maybeList.RandomGetOne()}有{probability:P}";
        }

        [KouPluginFunction(ActivateKeyword = "抽号码|roll", Name = "随机抽取号码",
            Help = "从给定的区间随机选取整数字（给单个数则是从1-给定数字的范围）（默认是1-100）", SupportedParameters = new []{nameof(Count), nameof(CanRepeat)})]
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

        public KouMessage Message { get; set; }
        public IKouSessionService SessionService { get; set; }
    }
}

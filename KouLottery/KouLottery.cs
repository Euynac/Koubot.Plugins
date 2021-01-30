using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Koubot.SDK.Interface;
using Koubot.SDK.Models.Entities;
using Koubot.SDK.Models.System;
using Koubot.SDK.Protocol;
using Koubot.SDK.Protocol.Plugin;
using Koubot.SDK.Services.Interface;
using Koubot.Tool.Expand;
using Koubot.Tool.Math;
using Koubot.Tool.Random;
using Koubot.Tool.String;

namespace KouFunctionPlugin
{
    [KouPluginClass(ActivateName = "lot|抽奖",
        Author = "7zou",
        Authority = KouEnum.Authority.NormalUser,
        Introduction = "抽奖机",
        PluginType = KouEnum.PluginType.Function,
        Title = "抽奖机")]
    public class KouLottery : KouPlugin, IWantKouMessage, IWantKouSession
    {
        [KouPluginParameter(ActivateKeyword = "count|c", Name = "抽签数量", Help = "范围在1-100",
            NumberMin = 1, NumberMax = 100)]
        public int Count { get; set; } = 1;

        [KouPluginParameter(ActivateKeyword = "可重复|r", Name = "指示能否重复中同一个签")]
        public bool CanRepeat { get; set; }

        [KouPluginFunction(Name = "帮助")]
        public override object Default(string str = null)
        {
            return "新特性施工中";//帮助特性
            throw new NotImplementedException();
        }

        [KouPluginFunction(Name = "当前群自由抽签", ActivateKeyword = "抽奖会场",
            Help = "当前群所有回复1的人加入抽奖\n使用方法：[n（要抽取个数，默认一个）]")]
        public object CurrentGroup(int count = 1)
        {
            return "新特性施工中";//帮助特性
            var message = SessionService.AskGroup($"开始抽奖啦，抽取{count}个人，回复1加入抽签", setting =>
            {
                setting.Attribute = KouSessionSetting.SessionAttribute.CircularSession;
            });
            return null;
        }

        [KouPluginFunction(ActivateKeyword = "选择", Name = "帮忙随机选择",
            Help = "会从给的几个选项中随机选择", UsageInstruction = "[自定义签1][自定义签2]...[-c 数量（抽签数目）][-r （是否可重复）]")]
        public string DrawCustomLottery([KouPluginArgument(Name = "自定义签", ArgumentAttributes = KouEnum.KouParameterAttribute.AllowDuplicate
        , SplitChar = " ")]List<string> lotList)
        {
            string result = "Kou建议你选择：";
            if (CanRepeat)
            {
                for (int i = 0; i < Count; i++)
                {
                    result += lotList.RandomGetOne() + "、";
                }
            }
            else result += (lotList.RandomGetItems(Count)).ToIListString("、");

            return result.TrimEnd('、');
        }
        [KouPluginFunction(ActivateKeyword = "抽号码", Name = "随机抽取号码",
            Help = "从给定的范围随机选取整数字", UsageInstruction = "<区间范围>[-c 数量（抽取数目）]")]
        public string RandomNumber(IntervalDoublePair interval)
        {
            string result = "Kou抽出了：";
            for (int i = 0; i < Count; i++)
            {
                result += interval.GenerateRandomInt()+ "、";
            }
            return result.TrimEnd('、');
        }

        public KouMessage Message { get; set; }
        public IKouSessionService SessionService { get; set; }
    }
}

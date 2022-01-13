using Koubot.Shared.Protocol;
using Koubot.Tool.Extensions;
using System.Collections.Generic;
using Koubot.SDK.PluginInterface;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.KouEnum;

namespace KouGamePlugin
{
    [KouPluginClass("24", "24点",
        Introduction = "24点游戏",
        Author = "7zou",
        PluginType = PluginType.Game)]
    public class KouTwentyFour : KouPlugin<KouTwentyFour>
    {

        [KouPluginParameter(Name = "计算目标（默认24点）", ActivateKeyword = "t|target")]
        public int Target { get; set; } = 24;
        [KouPluginParameter(Name = "获取所有解组合", ActivateKeyword = "all")]
        public bool All { get; set; }

        [KouPluginFunction(Name = "使用给定的数尝试计算24点，输出一个解", ActivateKeyword = "cal")]
        public object TryCalUse(
            [KouPluginArgument(Name = "使用数字，比如1，2，3，4",
                SplitChar = ",， 、",
                ArgumentAttributes = KouParameterAttribute.AllowDuplicate)]
            List<int> useNumList)
        {
            if (useNumList.Count > 5) return "最多只能算5个数";
            TwentyFour a = new TwentyFour();
            string answerStr = null;
            List<string> answers = null;
            var success = All
                ? a.TryCalUse(Target, useNumList, out answers)
                : a.TryTest(Target, useNumList, out answerStr);
            if (!success) return $"我算了{a.CalCount}次，也没能算出答案呢...";
            if (answers != null) answerStr = answers.ToStringJoin('\n').TrimEnd('\n');
            answerStr = All ? $"总共有如下{answers?.Count}个答案：{answerStr}" : $"中的一个解：{answerStr}";

            return $"使用[{useNumList.ToStringJoin(',')}]计算{Target}点{answerStr}";
        }

        [KouPluginFunction(Name = "使用给定的数尝试计算24点测试是否有答案", ActivateKeyword = "test")]
        public object TryTest(
            [KouPluginArgument(Name = "使用的数字，比如1，2，3，4",
                SplitChar = ",， 、",
                ArgumentAttributes = KouParameterAttribute.AllowDuplicate)]
            List<int> useNumList)
        {
            if (useNumList.Count > 5) return "最多只能算5个数";
            TwentyFour a = new TwentyFour();
            if (!a.TryTest(Target, useNumList, out var answers))
            {
                return $"我算了{a.CalCount}次，也没能算出答案呢...";
            }

            return $"使用[{useNumList.ToStringJoin(',')}]计算{Target}点发现了答案噢";
        }

        [KouPluginFunction]
        public override object? Default(string? str = null)
        {
            return ReturnHelp();
        }


    }
}
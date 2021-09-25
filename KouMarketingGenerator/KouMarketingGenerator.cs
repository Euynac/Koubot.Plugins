using Koubot.Shared.Protocol;
using Koubot.Tool.Random;
using System.Collections.Generic;
using Koubot.SDK.PluginInterface;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.KouEnum;

namespace KouFunctionPlugin
{
    /// <summary>
    /// Kou营销号生成器
    /// </summary>
    [KouPluginClass("yingxiao", "营销号生成器",
        Introduction = "营销号生成器\n使用方法：<主体> <事件> <另一种说法>",
        Author = "7zou",
        PluginType = PluginType.Function)]
    public class KouMarketingGenerator : KouPlugin<KouMarketingGenerator>
    {
        private static readonly List<string> Ends = new List<string>()
        {
            "希望小编精心整理的这篇内容能够解决你的困惑。",
            "大家有什么想法呢，欢迎在评论区告诉小编一起讨论哦！"
        };

        [KouPluginFunction(Name = "营销号话术生成")]
        public string Default([KouPluginArgument(Name = "主体")] string main,
            [KouPluginArgument(Name = "事件")] string events,
            [KouPluginArgument(Name = "另一种说法")] string anotherEvent)
        {
            if (string.IsNullOrWhiteSpace(main) || string.IsNullOrWhiteSpace(events) || string.IsNullOrWhiteSpace(anotherEvent)) return "使用方法：<主体> <事件> <另一种说法>";
            return Generate(main, events, anotherEvent);
        }


        public string Generate(string main, string events, string anotherEvent)
        {
            string result = $"   {main}{events}是怎么回事呢？{main}相信大家都很熟悉，但是{main}{events}是怎么回事呢，下面就让小编带大家一起了解吧。\n" +
                $"   {main}{events}，其实就是{anotherEvent}，大家可能会很惊讶{main}怎么会{events}呢？但事实就是这样，小编也感到非常惊讶。\n" +
                $"   这就是关于{main}{events}的事情了，{Ends.RandomGetOne()}";
            return result;
        }

    }
}

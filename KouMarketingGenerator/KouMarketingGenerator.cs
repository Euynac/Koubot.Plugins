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
    [PluginClass("scq", "生成器",
        Introduction = "生成器",
        PluginType = PluginType.Game)]
    public class KouMarketingGenerator : KouPlugin<KouMarketingGenerator>
    {

        [PluginFunction(Name = "论文生成器", Help = "原创：https://github.com/Uahh/Slscq")]
        public object? Essay([PluginArgument(Name = "主题")] string subject, [PluginArgument(Name = "字数", Min = 50, Max = 5000)] int count = 200)
        {
            return new EssayGenerator().Generate(subject, count);
        }



        private static readonly List<string> Ends = new()
        {
            "希望小编精心整理的这篇内容能够解决你的困惑。",
            "大家有什么想法呢，欢迎在评论区告诉小编一起讨论哦！"
        };


        [PluginFunction(ActivateKeyword = "market", Name = "营销号生成器")]
        public object? Market([PluginArgument(Name = "主体")] string main,
            [PluginArgument(Name = "事件")] string events,
            [PluginArgument(Name = "另一种说法")] string anotherEvent)
        {
            if (string.IsNullOrWhiteSpace(main) || string.IsNullOrWhiteSpace(events) ||
                string.IsNullOrWhiteSpace(anotherEvent)) return ReturnHelp(true);
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xyz.Koubot.AI.SDK.General;
using Xyz.Koubot.AI.SDK.Interface;
using Xyz.Koubot.AI.SDK.Protocol;
using Xyz.Koubot.AI.SDK.Models.Sql.PlugIn;

namespace KouFunctionPlugin
{
    /// <summary>
    /// Kou营销号生成器
    /// </summary>
    public class KouMarketingGenerator : IKouPlugin
    {
        static List<string> ends = new List<string>() 
        {
            "希望小编精心整理的这篇内容能够解决你的困惑。",
            "大家有什么想法呢，欢迎在评论区告诉小编一起讨论哦！"
        };
        public ErrorCodes ErrorCode { get; set; }
        public string ExtraErrorMessage { get; set; }

        [KouPluginFunction(nameof(Default), Name = "营销号话术生成", Help = "<主体> <事件> <另一种说法>")]
        public string Default(string main, string events, string anotherEvent)
        {
            if (string.IsNullOrWhiteSpace(main) || string.IsNullOrWhiteSpace(events) || string.IsNullOrWhiteSpace(anotherEvent)) return "使用方法：<主体> <事件> <另一种说法>";
            return Generate(main, events, anotherEvent);
        }

        public string Default(string str = null)
        {
            return null;
        }

        public string Generate(string main, string events, string anotherEvent)
        {
            string result = $"   {main}{events}是怎么回事呢？{main}相信大家都很熟悉，但是{main}{events}是怎么回事呢，下面就让小编带大家一起了解吧。\n" +
                $"   {main}{events}，其实就是{anotherEvent}，大家可能会很惊讶{main}怎么会{events}呢？但事实就是这样，小编也感到非常惊讶。\n" +
                $"   这就是关于{main}{events}的事情了，{ends.RandomGetOne()}";
            return result;
        }

        public PlugInInfoModel GetPluginInfo()
        {
            PlugInInfoModel plugInInfoModel = new PlugInInfoModel
            {
                Plugin_reflection = nameof(KouMarketingGenerator),
                Introduction = "营销号生成器\n使用方法：<主体> <事件> <另一种说法>",
                Plugin_author = "7zou",
                Plugin_activate_name = "yingxiao",
                Plugin_zh_name = "营销号生成器",
                Plugin_type = PluginType.Function
            };
            return plugInInfoModel;
        }
    }
}

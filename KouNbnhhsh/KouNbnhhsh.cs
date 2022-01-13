using Koubot.SDK.Tool;
using Koubot.Shared.Protocol;
using Koubot.Tool.Extensions;
using Koubot.Tool.Random;
using Koubot.Tool.Web;
using Koubot.Tool.Web.RateLimiter;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Koubot.SDK.PluginInterface;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.KouEnum;

namespace KouFunctionPlugin
{
    /// <summary>
    /// 第三方api测试 能不能好好说话
    /// </summary>
    [KouPluginClass("nbnhhsh", "能不能好好说话",
        Introduction = "首字母缩写翻译工具；源项目地址https://github.com/itorr/nbnhhsh\n输入带首字母缩写的文字，返回结果（多个则随机）",
        Author = "7zou",
        PluginType = PluginType.Function)]
    public class KouNbnhhsh : KouPlugin<KouNbnhhsh>
    {
        [KouPluginParameter(ActivateKeyword = "all", Help = "默认功能中使用会将所有结果输出")]
        public bool All { get; set; }

        [KouPluginFunction(Name = "能不能好好说话的默认功能", Help = "输入带缩写的文字，返回一个结果（多个则随机）\n支持all参数")]
        public override object? Default(string? str = null)
        {
            if (str.IsNullOrWhiteSpace()) return "输入带首字母缩写的一段话";
            var root = CallAPI(str);
            if (root != null && !root.Result.IsNullOrEmptySet())
            {
                if (All)
                {
                    StringBuilder result = new StringBuilder();
                    foreach (var item in root.Result)
                    {
                        if (!item.Trans.IsNullOrEmptySet())
                        {
                            result.Append(item.Name + "：");
                            foreach (var word in item.Trans)
                            {
                                result.Append(word + " ");
                            }
                            result.Append("\n");
                        }
                    }
                    return result.ToString().Trim();
                }
                else
                {
                    foreach (var item in root.Result)
                    {
                        if (item.Trans.IsNullOrEmptySet()) continue;
                        Regex regex1 = new Regex(item.Name);
                        str = regex1.Replace(str, item.Trans.RandomGetOne(), 1);
                    }
                    return str.IsNullOrEmpty() ? "不懂" : str.Trim();
                }

            }
            return "不懂";
        }






        public Root CallAPI(string str)
        {
            if (str.IsNullOrWhiteSpace()) return null;
            string result;
            using (var limiter = new LeakyBucketRateLimiter(nameof(KouNbnhhsh), 2))
            {
                if (!limiter.CanRequest())
                {
                    this.InheritError(limiter, "发生在" + nameof(KouNbnhhsh) + "中的" + nameof(CallAPI));
                    return null;
                }
                result = WebHelper.HttpPost("https://lab.magiconch.com/api/nbnhhsh/guess/", "{\"text\":\"" + str + "\"}", WebContentType.Json);
            }

            result = "{\"result\":" + result + "}";
            Root root = JsonConvert.DeserializeObject<Root>(result);
            return root;
        }
        public class ResultItem
        {
            /// <summary>
            /// 原文
            /// </summary>
            public string Name { get; set; }
            /// <summary>
            /// 结果
            /// </summary>
            public List<string> Trans { get; set; }
        }

        public class Root
        {
            /// <summary>
            /// 
            /// </summary>
            public List<ResultItem> Result { get; set; }
        }
    }

}

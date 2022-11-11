using Koubot.Tool.Extensions;
using Koubot.Tool.Random;
using Koubot.Tool.Web;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Koubot.SDK.PluginInterface;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.KouEnum;

namespace KouFunctionPlugin
{
    /// <summary>
    /// 第三方api测试 能不能好好说话
    /// </summary>
    [PluginClass("nbnhhsh", "能不能好好说话",
        Introduction = "首字母缩写翻译工具；源项目地址https://github.com/itorr/nbnhhsh\n输入带首字母缩写的文字，返回结果（多个则随机）",
        Author = "7zou",
        PluginType = PluginType.Function)]
    public class KouNbnhhsh : KouPlugin<KouNbnhhsh>
    {
        [PluginParameter(Help = "默认功能中使用会将所有结果输出")]
        public bool All { get; set; }

        [PluginFunction(Name = "能不能好好说话的默认功能", Help = "输入带缩写的文字，返回一个结果（多个则随机）", SupportedParameters = new []{nameof(All)})]
        public override object? Default(string? str = null)
        {
            if (str.IsNullOrWhiteSpace()) return "输入带首字母缩写的一段话";
            var list = CallAPI(str);
            if (!list.IsNullOrEmptySet())
            {
                if (All)
                {
                    StringBuilder result = new StringBuilder();
                    foreach (var item in list)
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
                    foreach (var item in list)
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

        public List<ResultItem> CallAPI(string str)
        {
            if (str.IsNullOrWhiteSpace()) return null;
    
            var result = KouHttp.Create("https://lab.magiconch.com/api/nbnhhsh/guess/").SetQPS(2).SetJsonBody(new {text = str})
                .SendRequest(HttpMethods.POST).Body;
            var list = JsonSerializer.Deserialize<List<ResultItem>>(result, new JsonSerializerOptions(){PropertyNameCaseInsensitive = true});
            return list;
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
    }

}

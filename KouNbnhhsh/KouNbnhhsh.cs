using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xyz.Koubot.AI.SDK.General;
using Xyz.Koubot.AI.SDK.Interface;
using Xyz.Koubot.AI.SDK.Protocol;
using Xyz.Koubot.AI.SDK.Models.Sql.PlugIn;
using Xyz.Koubot.AI.SDK.Tool;
using Xyz.Koubot.AI.SDK.Tool.Web;

namespace KouFunctionPlugin
{
    /// <summary>
    /// 第三方api测试 能不能好好说话
    /// </summary>
    public class KouNbnhhsh : IKouPlugin
    {
        [KouPluginParameter(nameof(All), ActivateKeyword = "all", Help = "默认功能中使用会将所有结果输出", Attributes = KouParameterAttribute.Bool)]
        public bool All { get; set; }
        public ErrorCodes ErrorCode { get; set; }
        public string ExtraErrorMessage { get; set; }

        [KouPluginFunction(nameof(Default), Name = "能不能好好说话的默认功能", Help = "输入带缩写的文字，返回一个结果（多个则随机）\n支持all参数")]
        public string Default(string str = null)
        {
            if (str.IsNullOrWhiteSpace()) return "输入带首字母缩写的一段话";
            var root = CallAPI(str);
            if (root != null && !root.Result.IsEmpty())
            {
                if (All)
                {
                    StringBuilder result = new StringBuilder();
                    foreach (var item in root.Result)
                    {
                        if (!item.Trans.IsEmpty())
                        {
                            result.Append(item.Name + "：");
                            foreach (var word in item.Trans)
                            {
                                result.Append(word + " ");
                            }
                            result.Append("\n");
                        }
                    }
                    return result.ToString()?.Trim() ?? "不懂";
                }
                else
                {
                    foreach (var item in root.Result)
                    {
                        if (item.Trans.IsEmpty()) continue;
                        Regex regex1 = new Regex(item.Name);
                        str = regex1.Replace(str, item.Trans.RandomGetOne(), 1);
                    }
                    return str.IsNullOrEmpty() ? "不懂" : str.Trim();
                }

            }
            return "不懂";
        }


        public PlugInInfoModel GetPluginInfo()
        {
            PlugInInfoModel plugInInfoModel = new PlugInInfoModel
            {
                Plugin_reflection = nameof(KouNbnhhsh),
                Introduction = "首字母缩写翻译工具；源项目地址https://github.com/itorr/nbnhhsh\n输入带首字母缩写的文字，返回结果（多个则随机）",
                Plugin_author = "7zou",
                Plugin_activate_name = "nbnhhsh",
                Plugin_zh_name = "能不能好好说话",
                Plugin_type = PluginType.Function
            };
            return plugInInfoModel;
        }



        public Root CallAPI(string str)
        {
            if (str.IsNullOrWhiteSpace()) return null;
            ApiCallLimiter apiCallLimiter = new ApiCallLimiter(nameof(KouNbnhhsh), LimitingType.LeakyBucket, 2);
            if (!apiCallLimiter.RequestWithRetry())
            {
                ErrorService.InheritError(this, apiCallLimiter);
                ExtraErrorMessage += " 发生在" + nameof(KouNbnhhsh) + "中的" + nameof(CallAPI);
                return null;
            }
            var result = WebHelper.HttpPost("https://lab.magiconch.com/api/nbnhhsh/guess/", "{\"text\":\"" + str + "\"}", WebHelper.WebContentType.Json);
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

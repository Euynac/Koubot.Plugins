using System;
using System.Collections.Generic;
using System.Linq;
using Koubot.SDK.Protocol;
using Koubot.SDK.Protocol.Event;
using Koubot.SDK.Protocol.Plugin;
using Koubot.Tool.String;

namespace KouFunctionPlugin
{
    /// <summary>
    /// Kou字符串工具
    /// </summary>
    [KouPluginClass(
        "str", 
        "字符串工具", 
        Author = "7zou")]
    public class KouStringToolPlugin : KouPlugin
    {
        [KouPluginParameter(ActivateKeyword = "dsc", Name = "降序排序")]
        public bool Descending { get; set; }
        [KouPluginParameter(ActivateKeyword = "ignoreCase", Name = "忽略大小写")]
        public bool IgnoreCase { get; set; }
        [KouPluginFunction(ActivateKeyword = "sort", Name = "排序字符串",
            SupportedParameters = new[] {nameof(Descending), nameof(IgnoreCase)})]
        public object SortStrings([KouPluginArgument(Name = "字符串", SplitChar = "\r\n")]List<string> strList)
        {
            int factor = Descending ? -1 : 1;
            strList.Sort((s, s1) =>
                string.Compare(s, s1, IgnoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture) *
                factor);
            return strList.ToIListString("\n");
        }
        [KouPluginFunction(ActivateKeyword = "split", Name = "分割字符串",
            SupportedParameters = new[] {nameof(IgnoreCase)})]
        public object SplitString(string separator, string str)
        {
            return str.Split(separator.ToCharArray()).ToIListString("\n");
        }

        [KouPluginFunction(ActivateKeyword = "replace", Name = "字符串替换",
            SupportedParameters = new[] {nameof(IgnoreCase)})]
        public object StringReplace(string oldValue, string newValue, string str)
        {
            return str.Replace(oldValue, newValue, IgnoreCase, null);
        }
        [KouPluginFunction] 
        public override object Default(string str = null)
        {
            return new EventPluginHelp();
        }
    }
}

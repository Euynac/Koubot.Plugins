using Koubot.SDK.Tool;
using Koubot.Tool.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Koubot.Shared.Interface;
using Koubot.Shared.Protocol;
using Koubot.Shared.Protocol.Plugin;
using Koubot.Shared.Protocol.Result;

namespace KouFunctionPlugin
{
    /// <summary>
    /// Kou字符串工具
    /// </summary>
    [KouPluginClass(
        "str|grep",
        "字符串工具",
        Author = "7zou")]
    public class KouStringToolPlugin : KouPlugin<KouStringToolPlugin>
    {
        [KouPluginParameter(ActivateKeyword = "dsc", Name = "降序排序")]
        public bool Descending { get; set; }
        [KouPluginParameter(ActivateKeyword = "i", Name = "忽略大小写（ignoreCase）")]
        public bool IgnoreCase { get; set; }
        [KouPluginParameter(ActivateKeyword = "c", Name = "显示匹配的行数（count）")]
        public bool RowCount { get; set; }
        [KouPluginParameter(ActivateKeyword = "v", Name = "只显示不匹配的（Invert）")]
        public bool Invert { get; set; }

        [KouPluginFunction(ActivateKeyword = "sort", Name = "按行排序字符串",
            SupportedParameters = new[] { nameof(Descending), nameof(IgnoreCase) })]
        public object SortStrings([KouPluginArgument(Name = "字符串", SplitChar = "\r\n")] List<string> strList)
        {
            int factor = Descending ? -1 : 1;
            strList.Sort((s, s1) =>
                string.Compare(s, s1, IgnoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture) *
                factor);
            return strList.ToStringJoin("\n");
        }
        [KouPluginFunction(ActivateKeyword = "rmb", Name = "移除空格")]
        public object RemoveBlankSpace(string value)
        {
            return value.IsNullOrWhiteSpace() ? value : value.Replace(" ", "");
        }
        [KouPluginFunction(ActivateKeyword = "rm|remove", Name = "移除指定字符")]
        public object Remove(string strToRemove, string value)
        {
            if (strToRemove.IsNullOrWhiteSpace()) strToRemove = " ";
            if (strToRemove != " ")
            {
                var stringsNeedToRemove = strToRemove.Split(new[] { ' ', ',', '，', '、' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var str in stringsNeedToRemove)
                {
                    value = value.Replace(str, "");
                }
            }

            return value;
        }


        [KouPluginFunction(ActivateKeyword = "split|s", Name = "分割字符串",
            SupportedParameters = new[] { nameof(IgnoreCase) })]
        public object SplitString(string separator, string str)
        {
            return str.Split(separator.ToCharArray()).ToStringJoin("\n");
        }

        [KouPluginFunction(ActivateKeyword = "replace|r", Name = "字符串替换",
            SupportedParameters = new[] { nameof(IgnoreCase) })]
        public object StringReplace(string oldValue, string newValue, string str)
        {
            return str.Replace(oldValue, newValue, IgnoreCase, null);
        }
        [KouPluginFunction(ActivateKeyword = "count|c", Name = "字符数统计")]
        public int StringWordCount(string words)
        {
            return words.Length;
        }
        public override object Default(string str = null)
        {
            return new ResultPluginHelp();
        }

        #region grep
        private object Default(string pattern, string strToMatch)
        {
            if (strToMatch.IsNullOrEmpty()) return "要搜索的字符串为空";
            if (pattern.IsNullOrEmpty()) return "匹配模式为空";
            bool isUsingRegex = false;
            var list = strToMatch.Split('\n', '\r', StringSplitOptions.RemoveEmptyEntries);
            var regexPattern = GetRegexPattern(pattern);
            if (regexPattern != null)
            {
                isUsingRegex = true;
                pattern = regexPattern;
            }

            var result = GrepResult(false, list, IgnoreCase, Invert, pattern, isUsingRegex);
            if (RowCount) result = $"共{result.AllIndexOf("\n")?.Count ?? 0}行匹配\n" + result;
            return result;
        }
        [KouPluginFunction(
            Name = "grep文本搜索",
            Help = "grep模仿自Linux，Globally search a Regular Expression and Print",
            SupportedParameters = new[] { nameof(IgnoreCase), nameof(Invert), nameof(RowCount) })]
        public object Default(string pattern, object list)
        {
            if (list is string str) return Default(pattern, str);
            bool isUsingRegex = false;
            var regexPattern = GetRegexPattern(pattern);
            if (regexPattern != null)
            {
                isUsingRegex = true;
                pattern = regexPattern;
            }
            var result = GrepResult(true, (IEnumerable)list, IgnoreCase, Invert, pattern, isUsingRegex);
            if (RowCount) result = $"共{result.AllIndexOf("\n")?.Count ?? 0}行匹配\n" + result;
            return result;

        }

        /// <summary>
        /// 支持多种list，适配AutoModel类list转换为string
        /// </summary>
        /// <param name="enumerable"></param>
        /// <param name="isIKouFormattable"></param>
        /// <returns></returns>
        private static IEnumerable<string> YieldString(IEnumerable enumerable, bool isIKouFormattable = false)
        {
            var iterator = enumerable.GetEnumerator();
            while (iterator.MoveNext())
            {
                var item = iterator.Current;
                if (item == null) continue;
                yield return
                    isIKouFormattable ? ((IKouFormattable)item).ToString(FormatType.Brief) : item.ToString();
            }

        }

        private static string GrepResult(bool isIKouFormattable, IEnumerable enumerable, bool ignoreCase, bool invert, string pattern, bool isRegex)
        {
            StringBuilder resultBuilder = new StringBuilder();
            if (!isRegex)
            {
                StringComparison comparisonType =
                    ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
                if (!invert)
                {
                    foreach (var item in YieldString(enumerable, isIKouFormattable))
                    {
                        if (item.Contains(pattern, comparisonType))
                            resultBuilder.Append(item + "\n");
                    }
                }
                else
                {
                    foreach (var item in YieldString(enumerable, isIKouFormattable))
                    {
                        if (!item.Contains(pattern, comparisonType))
                            resultBuilder.Append(item + "\n");
                    }
                }

            }
            else
            {
                RegexOptions options = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
                Regex regex = new Regex(pattern, options);
                if (!invert)
                {
                    foreach (var item in YieldString(enumerable, isIKouFormattable))
                    {
                        if (regex.IsMatch(item))
                            resultBuilder.Append(item + "\n");
                    }
                }
                else
                {
                    foreach (var item in YieldString(enumerable, isIKouFormattable))
                    {
                        if (!regex.IsMatch(item))
                            resultBuilder.Append(item + "\n");
                    }
                }
            }
            return resultBuilder.ToString().TrimEnd('\n');
        }

        private string GetRegexPattern(string pattern)
        {
            if (pattern.IsKouRegex(out var regexPattern, out var error))
            {
                return error != null ? this.ReturnNullWithError(error, ErrorCodes.Core_InvalidRegexPattern) : regexPattern;
            }

            if (pattern.IsKouQuickRegex(out var quickPattern))
            {
                return quickPattern;
            }

            return null;
        }


        #endregion
    }
}

using Koubot.SDK.PluginExtension.Result;
using Koubot.SDK.Tool;
using Koubot.Shared.Interface;
using Koubot.Shared.Protocol;
using Koubot.Tool.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Koubot.SDK.PluginInterface;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.KouEnum;
using Koubot.Tool.Algorithm;
using Koubot.Tool.Random;

namespace KouFunctionPlugin
{
    /// <summary>
    /// Kou字符串工具
    /// </summary>
    [PluginClass(
        "str|grep",
        "字符串工具",
        Author = "7zou")]
    public class KouStringToolPlugin : KouPlugin<KouStringToolPlugin>
    {
        [PluginParameter(ActivateKeyword = "dsc", Name = "降序排序")]
        public bool Descending { get; set; }
        [PluginParameter(ActivateKeyword = "i", Name = "忽略大小写（ignoreCase）")]
        public bool IgnoreCase { get; set; }
        [PluginParameter(ActivateKeyword = "c", Name = "显示匹配的行数（count）")]
        public bool RowCount { get; set; }
        [PluginParameter(ActivateKeyword = "v", Name = "只显示不匹配的（Invert）")]
        public bool Invert { get; set; }

        [PluginParameter(ActivateKeyword = "rg", Name = "正则捕获组名")]
        public string? RegexGroupName { get; set; }

        [PluginFunction(Name = "分割去重")]
        public object? Distinct([PluginArgument(SplitChar = ",，、 \n")]List<string> list)
        {
            return list.Distinct().StringJoin(' ');
        }
        [PluginFunction(Name = "整体去重", Help = "当作单个字符去重")]
        public object? WholeDistinct(string str)
        {
            return new string(str.Distinct().ToArray());
        }

        [PluginFunction(ActivateKeyword = "sort", Name = "按行排序字符串",
            SupportedParameters = new[] { nameof(Descending), nameof(IgnoreCase) })]
        public object SortStrings([PluginArgument(Name = "字符串", SplitChar = "\r\n")] List<string> strList)
        {
            var factor = Descending ? -1 : 1;
            strList.Sort((s, s1) =>
                string.Compare(s, s1, IgnoreCase ? StringComparison.CurrentCultureIgnoreCase : StringComparison.CurrentCulture) *
                factor);
            return strList.StringJoin("\n");
        }
        [PluginFunction(ActivateKeyword = "rmb", Name = "移除空格")]
        public object RemoveBlankSpace(string value)
        {
            return value.IsNullOrWhiteSpace() ? value : value.Replace(" ", "");
        }
        [PluginFunction(ActivateKeyword = "rm|remove", Name = "移除指定字符")]
        public object Remove(string strToRemove, string value)
        {
            if (strToRemove.IsNullOrWhiteSpace()) strToRemove = " ";
            if (strToRemove != " ")
            {
                value = value.Replace(strToRemove, "");
            }

            return value;
        }

        [PluginFunction(ActivateKeyword = "title case", Name = "每个英文首字母大写")]
        public object? ToTitleCase(string str) => str.ToTitleCase();

        [PluginFunction(ActivateKeyword = "camel case split", Name = "驼峰字符串分割")]
        public object? CamelCaseSplit(string str) => str.CamelCaseSplit(true).StringJoin(' ');

        [PluginFunction(ActivateKeyword = "split|s", Name = "分割字符串",
            SupportedParameters = new[] { nameof(IgnoreCase) })]
        public object SplitString(string separator, string str)
        {
            separator = separator.Replace("\\n", "\n");
            return str.Split(separator.ToCharArray()).StringJoin("\n");
        }

        [PluginFunction(ActivateKeyword = "append start", Name = "在字符串最前面添加")]
        public object? StringAppendStart([PluginArgument(Name = "添加的内容")] string content, [PluginArgument(Name = "字符串列表", SplitChar = "\n ")] List<string> strList)
        {
            return strList.Select(p => content + p).StringJoin(" ");
        }
        [PluginFunction(ActivateKeyword = "append end", Name = "在字符串最后面添加")]
        public object? StringAppendEnd([PluginArgument(Name = "添加的内容")] string content, [PluginArgument(Name = "字符串列表", SplitChar = "\n ")] List<string> strList)
        {
            return strList.Select(p => p + content).StringJoin(" ");
        }
        [PluginFunction(ActivateKeyword = "join", Name = "字符串合并")]
        public object? StringJoin([PluginArgument(Name = "合并符")] string separator, [PluginArgument(Name = "字符串列表", SplitChar = "\n ")] List<string> strList)
        {
            return strList.StringJoin(separator);
        }

        [PluginFunction(ActivateKeyword = "append advance", Name = "高级添加")]
        public object? AdvanceAppend(
            [PluginArgument(Name = "0代表原始值")] string expression,
            [PluginArgument(Name = "字符串列表", SplitChar = "\n ")] List<string> strList)
        {
            var exp = expression.Replace("0", "$0");
            return strList.Select(p => p.RegexReplace(".+", exp)).StringJoin('\n');
        }

        [PluginFunction(ActivateKeyword = "replace|r", Name = "字符串替换",
            SupportedParameters = new[] { nameof(IgnoreCase) })]
        public object StringReplace(string oldValue, string newValue, string str)
        {
            return str.Replace(oldValue, newValue, IgnoreCase, null);
        }
        [PluginFunction(ActivateKeyword = "multi replace", Name = "批量字符串替换",
            SupportedParameters = new[] { nameof(IgnoreCase) })]
        public object MultiStringReplace(string oldValue, List<string> newValues, string str)
        {
            var list = new List<string>();

            foreach (var newValue in newValues)
            {
                var origin = str;
                list.Add((string)StringReplace(oldValue, newValue, origin));
            }

            return list.StringJoin('\n');
        }
        [PluginFunction(Name = "统计元素个数")]
        public object? CountItem(List<string> words)
        {
            return words.Count;
        }

        //[KouPluginFunction(ActivateKeyword = "word frequency", Name = "词频统计")]
        //public object? WordFrequency(string line)
        //{
        //    var seg = new JiebaSegmenter();
        //    var frequencies = new Counter<string>(seg.Cut(line));
        //    return frequencies.MostCommon().Select(p => $"{p.Key} {p.Value}").StringJoin(' ');
        //}


        [PluginFunction(ActivateKeyword = "count", Name = "统计子字符串出现次数")]
        public object? CountSpecificString(string subStr, string wholeStr)
        {
            return wholeStr.AllIndexOf(subStr).Count;
        }
        [PluginFunction(ActivateKeyword = "count word", Name = "字符数统计")]
        public int StringWordCount(string words)
        {
            return new StringInfo(words).LengthInTextElements;
        }
        [PluginFunction(ActivateKeyword = "repeat", Name ="重复")]
        public object RepeatStr(
            [PluginArgument(Name ="重复次数", Min = 1, Max = 1000, CustomRangeErrorReply ="重复范围1-1000次")] int times,
            [PluginArgument(Name ="要重复的词")] string str)
        {
            return str.Repeat(times);
        }

        [PluginFunction(Name = "编辑距离(Levenshtein Distance)")]
        public object? Distance(string str1, string str2)
        {
            return LevenshteinDistance.Calculate(str1, str2);
        }

        [PluginFunction(Name = "相似度", Help = "当前默认算法是Levenshtein distance")]
        public object? Similarity(string str1, string str2)
        {
            return LevenshteinDistance.Similarity(str1, str2).ToString("P");
        }

        [PluginFunction(ActivateKeyword = "matched", Name = "正则获取匹配项")]
        public object? GetMatched(string pattern, string source)
        {
            return RegexGroupName != null
                ? source.MatchedGroupValues(pattern, RegexGroupName).StringJoin(' ')
                : source.Matches(pattern).StringJoin(' ');
        }

        [PluginFunction(ActivateKeyword = "random string", Name = "生成随机字符串")]
        public object? GenerateRandomStr([PluginArgument(Name = "随机模板字符串")] string pattern,
            [PluginArgument(Name = "生成数量", Min = 1, Max = 1000)] int count = 1, [PluginArgument(Name = "分割符")] string separator = "\n")
        {
            var sb = new StringBuilder();
            var id = 1;
            while (count -- > 0)
            {
                sb.Append(RandomTool.GetString(pattern, true, id) + separator);
                id++;
            }

            return sb.ToString().TrimEndOnce(separator);
        }
        public override object? Default(string? str = null)
        {
            return new ResultPluginHelp();
        }

        #region grep
        private object Default(string pattern, string strToMatch)
        {
            if (strToMatch.IsNullOrEmpty()) return "要搜索的字符串为空";
            if (pattern.IsNullOrEmpty()) return "匹配模式为空";
            var isUsingRegex = false;
            var list = strToMatch.Split('\n', StringSplitOptions.RemoveEmptyEntries);
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
        [PluginFunction(
            Name = "grep文本搜索",
            Help = "grep模仿自Linux，Globally search a Regular Expression and Print",
            SupportedParameters = new[] { nameof(IgnoreCase), nameof(Invert), nameof(RowCount) })]
        public object Default(string pattern, object list)
        {
            if (list is string str) return Default(pattern, str);
            var isUsingRegex = false;
            var regexPattern = GetRegexPattern(pattern);
            if (regexPattern != null)
            {
                isUsingRegex = true;
                pattern = regexPattern;
            }
            var result = GrepResult(list.GetType().GetUnderlyingType().IsAssignableTo(typeof(IKouFormattable)), (IEnumerable)list, IgnoreCase, Invert, pattern, isUsingRegex);
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
            var resultBuilder = new StringBuilder();
            if (!isRegex)
            {
                var comparisonType =
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
                var options = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;
                var regex = new Regex(pattern, options);
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

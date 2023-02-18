using Koubot.SDK.API;
using Koubot.Tool.Extensions;
using Koubot.Tool.General;
using Koubot.Tool.Random;
using Koubot.Tool.Web;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Koubot.SDK.PluginInterface;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.KouEnum;
using Koubot.Tool.Interfaces;
using Koubot.Tool.String;
using ToolGood.Words;
using static Koubot.SDK.API.BaiduTranslateAPI;

namespace KouFunctionPlugin
{
    [PluginClass("trans|echo", "翻译器",
        Introduction = "提供多种翻译、转换功能",
        Author = "7zou",
        PluginType = PluginType.Function)]
    public class KouTranslator : KouPlugin<KouTranslator>
    {
        [PluginParameter(ActivateKeyword = "l", Name = "英文转小写", Help = "返回的结果中的英文全部转为大写小写")]
        public bool Lower { get; set; } = false;
        [PluginParameter(ActivateKeyword = "u", Name = "英文转大写", Help = "返回的结果中的英文全部转为大写")]
        public bool Upper { get; set; } = false;

        [PluginParameter(ActivateKeyword = "from", Help = "源语言", DefaultContent = "auto")]
        public string From { get; set; } = null;

        [PluginParameter(ActivateKeyword = "to", Help = "目标语言", DefaultContent = "zh")]
        public string To { get; set; } = null;

        [PluginParameter(ActivateKeyword = "count", Help = "润色次数", DefaultContent = "5")]
        public string Count { get; set; } = "5";
       
        [PluginParameter(ActivateKeyword = "带空格", Name = "说 话 带 空 格", Help = "返回的结果中字符间带空格")]
        public bool SpeakWithWhiteSpace { get; set; }

        [PluginParameter(ActivateKeyword = "逆序", Name = "逆序", Help = "逆序输出结果，默认从尾到头逆序，赋值row按行逆序",
            DefaultContent = "all")]
        public string Reverse { get; set; }
        [PluginParameter(ActivateKeyword = "简体", Name = "转简体")]
        public bool ToSimplifiedChinese { get; set; }
        [PluginParameter(ActivateKeyword = "繁体", Name = "转简体")]
        public bool ToTraditionalChinese { get; set; }
        [PluginParameter(ActivateKeyword = "拼音", Name = "转拼音", Help = "带声调赋值tone", DefaultContent = "")]
        public string ToPinyin { get; set; }
        [PluginParameter(ActivateKeyword = "首拼音", Name = "转首字母拼音")]
        public bool ToFirstPinyin { get; set; }


        private enum SupportsPronounce
        {
            [KouEnumName("日", "jp")]
            Japanese,
            [KouEnumName("英", "en")]
            English,
            [KouEnumName("法","fr")]
            French,
            [KouEnumName("德")]
            German,
            [KouEnumName("俄")]
            Russian,
            [KouEnumName("韩")]
            Korean,
            [KouEnumName("泰")]
            Thai,
        }

        private static readonly Dictionary<SupportsPronounce, Dictionary<string, string>> _pronounceDict;
        static KouTranslator()
        {
            var json = FileTool.ReadEmbeddedResource("pronounce.json");
            _pronounceDict = JsonSerializer.Deserialize<Dictionary<SupportsPronounce, Dictionary<string, string>>>(json!);
        }

        [PluginFunction(ActivateKeyword = "pronounce", Name = "发音转换", SupportedParameters = new []{nameof(To)}, 
            Help = "用外语说中文。当前支持日语、英文、法语、德语、俄语、韩语、泰语（使用-to）。https://github.com/Uahh/Fyzhq")]
        public object? PronounceConvert(string content)
        {
            var pinyinList = WordsHelper.GetPinyin(content, "|").ToLowerInvariant();
            var target = SupportsPronounce.Japanese;
            if (!To.IsNullOrWhiteSpace())
            {
                if (To.EndsWith("语"))
                {
                    To = To.TrimEnd('语');
                }
                if (!To.TryToKouEnum(out target)) return "当前仅支持日语、英文、法语、德语、俄语、韩语、泰语";
            }
            var dict = _pronounceDict[target];
            var sb = new StringBuilder();
            foreach (var pinyin in pinyinList.Split('|'))
            {
                sb.Append(dict.GetValueOrDefault(pinyin) ?? pinyin);
                if (target == SupportsPronounce.English)
                {
                    sb.Append(" ");
                }
            }

            return sb.ToString();
        }


        [PluginFunction(Help = "基本复述", SupportedParameters = new[] { nameof(Lower), nameof(From), nameof(To), nameof(Upper), nameof(Reverse), nameof(ToTraditionalChinese), nameof(ToPinyin), nameof(ToFirstPinyin), nameof(ToSimplifiedChinese), nameof(SpeakWithWhiteSpace) })]
        public override object? Default(string? str = null)
        {
            if (str.IsNullOrWhiteSpace()) return null;
            Language fromLanguage = Language.auto, toLanguage = Language.zh;
            var translateFlag = false; //指示是否需要翻译语言
            if (!string.IsNullOrWhiteSpace(From))
            {
                if (Enum.TryParse(From, out fromLanguage) ||
                    TryParseZhNameToLanguage(From, out fromLanguage))
                {
                    translateFlag = true;
                }
            }
            if (!string.IsNullOrWhiteSpace(To))
            {
                if (Enum.TryParse(To, out toLanguage) || TryParseZhNameToLanguage(To, out toLanguage)
                ) //测试是否是Language内的
                {
                    translateFlag = true;
                }
                else //其他功能
                {
                    To = To.ToLower();
                    switch (To)
                    {
                        case "jianti":
                        case "简体":
                            str = ToSimplified(str);
                            break;
                        case "fanti":
                        case "繁体":
                            str = ToTraditional(str);
                            break;

                        case "banjiao":
                        case "半角":
                            str = ToHalfWidth(str);
                            break;
                        case "quanjiao":
                        case "全角":
                            str = ToFullWidth(str);
                            break;

                        case "b64":
                        case "base64":
                            str = ToBase64(str);
                            break;
                        case "md5":
                            str = ToMD5(str);
                            break;
                        default:
                            str = "to参数支持翻译API中的Language类（使用翻译的“支持语种”功能查看）以及其他功能：繁体、简体、全角、半角、base64、md5";
                            break;
                    }
                }
            }

            if (translateFlag) //若是调用了翻译API
            {
                var translator = new BaiduTranslateAPI();
                str = translator.Translate(str, fromLanguage, toLanguage);
                if (str == null)
                {
                    this.InheritError(translator);
                    return null;
                }
            }

            return ResultPipe(str);
        }

        [PluginFunction(ActivateKeyword = "languages|支持语种", Help = "获取所有支持的翻译语种，两边都可以作为to参数使用")]
        public string SupportedLanguage()
        {
            var str = "";
            var count = 1;
            foreach (var (language, code) in SupportedLanguagesZh)
            {
                str += $"{count}.{language} - {code}\n";
                count++;
            }

            return str.TrimEnd();
        }

        [PluginFunction(ActivateKeyword = "detect|语种", Help = "检测输入文本的语种")]
        public string DetectLanguage(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return "没有输入要检测语种的文本";
            var translator = new BaiduTranslateAPI();
            var result = translator.DetectLanguage(str);
            if (result == null)
            {
                this.InheritError(translator);
                return null;
            }
            return ResultPipe(result);
        }

        [PluginFunction(ActivateKeyword = "润色|polish", Name = "润（sheng）色（cao）文章", Help = "<原文>[-count 润色次数]")]
        public string KouRandomTranslateToZh(string source)
        {
            if (int.TryParse(Count, out var count))
            {
                return ResultPipe(RandomTranslateToZh(source, count));
            }
            return "润色失败";
        }

        /// <summary>
        /// 随机翻译多次到中文
        /// </summary>
        /// <param name="source"></param>
        /// <param name="count">最多就20次</param>
        /// <returns></returns>
        public string RandomTranslateToZh(string source, int count)
        {
            if (source.IsNullOrWhiteSpace()) return source;
            if (count <= 0) count = 1;
            else if (count >= 20) count = 20;
            var translator = new BaiduTranslateAPI();
            for (var i = 0; i < count; i++)
            {
                source = translator.Translate(source, Language.auto, RandomTool.EnumRandomGetOne<Language>());
                if (source == null)
                {
                    this.InheritError(translator);
                    return null;
                }
            }
            var result = translator.Translate(source, Language.auto);//最后转为中文
            if (result == null)
            {
                this.InheritError(translator);
                return null;
            }
            return result;
        }

        [PluginFunction(ActivateKeyword = "base64|转base64", Help = "转base64")]
        public string ToBase64(string str = null)
        {
            return ResultPipe(WebTool.EncodeBase64(str));
        }
        [PluginFunction(ActivateKeyword = "解base64|解码base64", Help = "解码base64")]
        public string DecodeBase64(string str)
        {
            return ResultPipe(WebTool.DecodeBase64(str));
        }

        [PluginFunction(ActivateKeyword = "转繁体|fanti", Help = "转繁体")]
        public string ToTraditional(string str = null)
        {
            return ResultPipe(WordsHelper.ToTraditionalChinese(str));
        }

        [PluginFunction(ActivateKeyword = "转简体|jianti", Help = "转简体")]
        public string ToSimplified(string str = null)
        {
            return ResultPipe(WordsHelper.ToSimplifiedChinese(str));
        }

        [PluginFunction(ActivateKeyword = "转全角", Help = "转全角")]
        public string ToFullWidth(string str = null)
        {
            return ResultPipe(StringTool.ToFullWidth(str));
        }
        [PluginFunction(ActivateKeyword = "转半角", Help = "转半角")]
        public string ToHalfWidth(string str = null)
        {
            return ResultPipe(StringTool.ToHalfWidth(str));
        }
        [PluginFunction(ActivateKeyword = "转MD5|MD5|md5|转md5", Help = "计算MD5值")]
        public string ToMD5(string str = null)
        {
            return ResultPipe(WebTool.StringHash(str));
        }

        
        
        [PluginFunction(ActivateKeyword = "转人民币", Help = "将输入的数字转中文大写")]
        public string ToChineseRMB(double number)
        {
            return ResultPipe(WordsHelper.ToChineseRMB(number));
        }
        /// <summary>
        /// 结果管道
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public string ResultPipe(string result)
        {
            if (result.IsNullOrWhiteSpace()) return result;
            if (Reverse == "all") result = ReverseStr(result);
            else if (Reverse == "row") result = ReverseStr(result, true);
            if (WordsHelper.HasChinese(result))
            {
                if (ToTraditionalChinese) result = WordsHelper.ToTraditionalChinese(result);
                else if (ToSimplifiedChinese) result = WordsHelper.ToSimplifiedChinese(result);
                else if (ToPinyin != null)
                {
                    result =
                        WordsHelper.GetPinyin(result, ToPinyin.Equals("tone", StringComparison.OrdinalIgnoreCase));
                }
                else if (ToFirstPinyin) result = WordsHelper.GetFirstPinyin(result)?.ToLower();
            }
            if (Lower)
                result = result?.ToLower();
            else if (Upper)
                result = result?.ToUpper();
            if (SpeakWithWhiteSpace)
                result = ToSpeakWithWhiteSpace(result);
            return result;
        }
        /// <summary>
        /// 反转字符串
        /// </summary>
        /// <param name="input"></param>
        /// <param name="row"></param>
        /// <returns></returns>
        public string ReverseStr(string input, bool row = false)
        {
            if (input.IsNullOrWhiteSpace()) return input;
            if (row)
            {
                var stringBuilder = new StringBuilder();
                foreach (var s in input.Split('\n', '\r'))
                {
                    stringBuilder.Append(ReverseStr(s));
                    stringBuilder.Append("\n");
                }

                return stringBuilder.ToString();
            }

            var stringBuilder2 = new StringBuilder();
            for (var i = input.Length - 1; i >= 0; i--)
            {
                stringBuilder2.Append(input[i]);
            }

            return stringBuilder2.ToString();
        }
        /// <summary>
        /// 每个字符间都带空格
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public string ToSpeakWithWhiteSpace(string line)
        {
            if (line.IsNullOrEmpty()) return line;
            var stringBuilder = new StringBuilder();
            foreach (var chr in line)
            {
                stringBuilder.Append(chr.ToString() + ' ');
            }

            return stringBuilder.ToString().TrimEnd(' ');
        }
    }
}

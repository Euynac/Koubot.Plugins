using Koubot.SDK.API;
using Koubot.SDK.Interface;
using Koubot.SDK.Protocol.Plugin;
using Koubot.SDK.Services;
using Koubot.Tool.Expand;
using Koubot.Tool.General;
using Koubot.Tool.Random;
using Koubot.Tool.String;
using Koubot.Tool.Web;
using System;
using System.Text;
using static Koubot.SDK.API.BaiduTranslateAPI;
using static Koubot.SDK.Protocol.KouEnum;

namespace KouFunctionPlugin
{
    [KouPluginClass(
        Introduction = "提供多种翻译、转换功能",
        Author = "7zou",
        ActivateName = "trans|翻译|translate",
        Title = "翻译器",
        PluginType = PluginType.Function)]
    public class KouTranslator : KouPlugin, IKouError
    {
        //维持，你说一句机器人翻译一句
        public bool Sustain { get; set; }

        [KouPluginParameter(ActivateKeyword = "l", Name = "英文转小写", Help = "返回的结果中的英文全部转为大写小写")]
        public bool Lower { get; set; } = false;
        [KouPluginParameter(ActivateKeyword = "u", Name = "英文转大写", Help = "返回的结果中的英文全部转为大写")]
        public bool Upper { get; set; } = false;

        [KouPluginParameter(ActivateKeyword = "from", Help = "源语言", DefaultContent = "auto")]
        public string From { get; set; } = null;

        [KouPluginParameter(ActivateKeyword = "to", Help = "目标语言", DefaultContent = "zh")]
        public string To { get; set; } = null;

        [KouPluginParameter(ActivateKeyword = "count", Help = "润色次数", DefaultContent = "5")]
        public string Count { get; set; } = "5";
        [KouPluginParameter(ActivateKeyword = "type", Help = "转换类型(如：时间戳unix、JavaScript)")]
        public string Type { get; set; }
        [KouPluginParameter(ActivateKeyword = "带空格", Name = "说 话 带 空 格", Help = "返回的结果中字符间带空格")]
        public bool SpeakWithWhiteSpace { get; set; }


        [KouPluginFunction(Help = "默认翻译")]
        public override object Default(string str = null)
        {
            if (str.IsNullOrWhiteSpace()) return null;
            if (Lower)
            {
                str = str.ToLower();
            }
            Language fromLanguage = Language.auto, toLanguage = Language.zh;
            bool translateFlag = false; //指示是否需要翻译语言
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
                        case "时间戳":
                            str = ToUnixTime(str);
                            break;
                        default:
                            str = "to参数支持翻译API中的Language类（使用翻译的“支持语种”功能查看）以及其他功能：繁体、简体、全角、半角、base64、md5、时间戳";
                            break;
                    }
                }
            }

            if (translateFlag) //若是调用了翻译API
            {
                BaiduTranslateAPI translator = new BaiduTranslateAPI();
                str = translator.Translate(str, fromLanguage, toLanguage);
                if (str == null)
                {
                    this.InheritError(translator);
                    return null;
                }
            }

            return ResultPipe(str);
        }

        [KouPluginFunction(ActivateKeyword = "languages|支持语种", Help = "获取所有支持的翻译语种，两边都可以作为to参数使用")]
        public string SupportedLanguage()
        {
            string str = "";
            int count = 1;
            foreach (var (language, code) in SupportedLanguagesZh)
            {
                str += $"{count}.{language} - {code}\n";
                count++;
            }

            return str.TrimEnd();
        }

        [KouPluginFunction(ActivateKeyword = "detect|语种", Help = "检测输入文本的语种")]
        public string DetectLanguage(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return "没有输入要检测语种的文本";
            BaiduTranslateAPI translator = new BaiduTranslateAPI();
            var result = translator.DetectLanguage(str);
            if (result == null)
            {
                this.InheritError(translator);
                return null;
            }
            return ResultPipe(result);
        }

        [KouPluginFunction(ActivateKeyword = "润色|polish", Name = "润（sheng）色（cao）文章", Help = "<原文>[-count 润色次数]")]
        public string KouRandomTranslateToZh(string source)
        {
            if (int.TryParse(Count, out int count))
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
            BaiduTranslateAPI translator = new BaiduTranslateAPI();
            for (int i = 0; i < count; i++)
            {
                source = translator.Translate(source, Language.auto, RandomTool.EnumRandomGetOne<Language>());
                if (source == null)
                {
                    this.InheritError(translator);
                    return null;
                }
            }
            string result = translator.Translate(source, Language.auto);//最后转为中文
            if (result == null)
            {
                this.InheritError(translator);
                return null;
            }
            return result;
        }

        [KouPluginFunction(ActivateKeyword = "base64|转base64", Help = "转base64")]
        public string ToBase64(string str = null)
        {
            return ResultPipe(WebTool.EncodeBase64(str));
        }
        [KouPluginFunction(ActivateKeyword = "解base64|解码base64", Help = "解码base64")]
        public string DecodeBase64(string str)
        {
            return ResultPipe(WebTool.DecodeBase64(str));
        }

        [KouPluginFunction(ActivateKeyword = "转繁体|fanti", Help = "转繁体")]
        public string ToTraditional(string str = null)
        {
            return ResultPipe(StringTool.ToTraditional(str));
        }

        [KouPluginFunction(ActivateKeyword = "转简体|jianti", Help = "转简体")]
        public string ToSimplified(string str = null)
        {
            return ResultPipe(StringTool.ToSimplified(str));
        }

        [KouPluginFunction(ActivateKeyword = "转全角", Help = "转全角")]
        public string ToFullWidth(string str = null)
        {
            return ResultPipe(StringTool.ToFullWidth(str));
        }
        [KouPluginFunction(ActivateKeyword = "转半角", Help = "转半角")]
        public string ToHalfWidth(string str = null)
        {
            return ResultPipe(StringTool.ToHalfWidth(str));
        }
        [KouPluginFunction(ActivateKeyword = "转MD5|MD5|md5|转md5", Help = "计算MD5值")]
        public string ToMD5(string str = null)
        {
            return ResultPipe(WebTool.EncryptStringMD5(str));
        }

        [KouPluginFunction(ActivateKeyword = "转时间", Help = "将时间戳转换为时间形式\ntype参数支持js，默认unix时间戳")]
        public string ToDateTime(string str)
        {
            if (str.IsNullOrWhiteSpace()) return null;
            if (str.Equals("now", StringComparison.OrdinalIgnoreCase))
            {
                return DateTime.Now.ToString();
            }
            TimeStampType timeStampType = TimeStampType.Unix;
            if (!Type.IsNullOrWhiteSpace())
            {
                if (str.Equals("js", StringComparison.OrdinalIgnoreCase) || str.Equals("javascript", StringComparison.OrdinalIgnoreCase))
                    timeStampType = TimeStampType.Javascript;
            }
            return str.ToDateTime(timeStampType).ToString();
        }
        [KouPluginFunction(ActivateKeyword = "转时间戳", Help = "将日期转换为时间戳格式\ntype参数支持js，默认unix时间戳")]
        public string ToUnixTime(string str)
        {
            if (str.IsNullOrWhiteSpace()) return null;
            if (str.Equals("now", StringComparison.OrdinalIgnoreCase))
            {
                return DateTime.Now.ToTimeStamp().ToString();
            }
            TimeStampType timeStampType = TimeStampType.Unix;
            if (!Type.IsNullOrWhiteSpace())
            {
                if (str.Equals("js", StringComparison.OrdinalIgnoreCase) || str.Equals("javascript", StringComparison.OrdinalIgnoreCase))
                    timeStampType = TimeStampType.Javascript;
            }
            return str.ToUnixTimeStamp(timeStampType).ToString();
        }
        [KouPluginFunction(ActivateKeyword = "转秒数", Help = "将输入的时间转换为对应秒数")]
        public string ToTotalSecond(string time)
        {
            if (time.IsNullOrWhiteSpace()) return 0.ToString();
            TimeSpan timeSpan = new TimeSpan();
            if (time.TryGetTimeSpan(out TimeSpan timeSpanFormal, false)) timeSpan += timeSpanFormal;
            if (ZhNumber.IsContainZhNumber(time)) time = ZhNumber.ToArabicNumber(time);
            if (KouStringTool.TryGetTimeSpanFromStr(time, out TimeSpan timeSpanModern)) timeSpan += timeSpanModern;
            if (KouStringTool.TryGetTimeSpanFromAncientStr(time, out TimeSpan timeSpanAncient)) timeSpan += timeSpanAncient;
            return timeSpan.TotalSeconds.ToString();
        }

        /// <summary>
        /// 结果管道
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public string ResultPipe(string result)
        {
            if (result.IsNullOrWhiteSpace()) return result;
            if (Lower)
                result = result.ToLower();
            else if (Upper)
                result = result.ToUpper();
            if (SpeakWithWhiteSpace)
                result = ToSpeakWithWhiteSpace(result);
            return result;
        }
        /// <summary>
        /// 每个字符间都带空格
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public string ToSpeakWithWhiteSpace(string line)
        {
            if (line.IsNullOrEmpty()) return line;
            StringBuilder stringBuilder = new StringBuilder();
            foreach (var chr in line)
            {
                stringBuilder.Append(chr.ToString() + ' ');
            }

            return stringBuilder.ToString().TrimEnd(' ');
        }
    }
}

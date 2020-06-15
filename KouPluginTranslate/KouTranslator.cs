using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xyz.Koubot.AI.SDK.Interface;
using Xyz.Koubot.AI.SDK.Models.Sql.PlugIn;
using Xyz.Koubot.AI.SDK.Protocol;
using Xyz.Koubot.AI.SDK.Tool;
using Xyz.Koubot.AI.SDK.General;
using static Xyz.Koubot.AI.SDK.Tool.Web.API.BaiduTranslateAPI;
using Xyz.Koubot.AI.SDK.Tool.Web.API;
using static Xyz.Koubot.AI.SDK.General.SystemExpand;
using Xyz.Koubot.AI.SDK.Tool.KouMath;

namespace KouFunctionPlugin
{
    public class KouTranslator : IKouPlugin, IErrorAvailable
    {
        //维持，你说一句机器人翻译一句
        public bool Sustain { get; set; }

        [KouPluginParameter(nameof(Lower), ActivateKeyword = "l", Help = "转小写", Attributes = KouParameterAttribute.Bool)]
        public bool Lower { get; set; } = false;
        [KouPluginParameter(nameof(From), ActivateKeyword = "from", Help ="源语言", DefalutContent = "auto")]
        public string From { get; set; } = null;

        [KouPluginParameter(nameof(To), ActivateKeyword = "to", Help = "目标语言", DefalutContent = "zh")]
        public string To { get; set; } = null;

        [KouPluginParameter(nameof(Count), ActivateKeyword = "count", Help = "润色次数", DefalutContent = "5")]
        public string Count { get; set; } = "5";
        [KouPluginParameter(nameof(Type), ActivateKeyword = "type", Help = "转换类型(如：时间戳unix、JavaScript)")]
        public string Type { get; set; }

        public ErrorCodes ErrorCode { get; set; }
        public string ExtraErrorMessage { get; set; }

        public PlugInInfoModel GetPluginInfo()
        {
            PlugInInfoModel plugInInfoModel = new PlugInInfoModel
            {
                Plugin_reflection = nameof(KouTranslator),
                Introduction = "Translator",
                Plugin_author = "7zou",
                Plugin_activate_name = "trans|翻译|translate",
                Plugin_zh_name = "翻译器",
                Plugin_type = PluginType.Function,
            };
            return plugInInfoModel;
        }

        [KouPluginFunction(nameof(Default), ActivateKeyword = "d", Help = "默认翻译")]
        public string Default(string str = null)
        {
            try
            {
                if (Lower && !string.IsNullOrWhiteSpace(str))
                {
                    str = str.ToLower();
                }
                Language fromLanguage = Language.auto, toLanguage = Language.zh;
                bool translateFlag = false;//指示是否需要翻译语言
                if (!string.IsNullOrWhiteSpace(From))
                {
                    if (Enum.TryParse(From, out fromLanguage) || BaiduTranslateAPI.TryParseZhNameToLanguage(From, out fromLanguage))
                    {
                        translateFlag = true;
                    }
                }
                if (!string.IsNullOrWhiteSpace(To))
                {
                    if(Enum.TryParse(To, out toLanguage) || BaiduTranslateAPI.TryParseZhNameToLanguage(To, out toLanguage))//测试是否是Language内的
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
                                str = "to参数支持翻译API中的Language类以及其他功能：繁体、简体、全角、半角、base64、md5、时间戳";
                                break;
                        }
                    }
                    
                }
                if (translateFlag)//若是调用了翻译API
                {
                    BaiduTranslateAPI translator = new BaiduTranslateAPI();
                    str = translator.Translate(str, fromLanguage, toLanguage);
                    if(str == null)
                    {
                        ErrorService.InheritError(this, translator);
                        return null;
                    }
                }
                return str;
            }
            catch (Exception e)
            {
                ErrorCode = ErrorCodes.Plugin_FatalError;
                ExtraErrorMessage = e.Message;
                throw;
            }
        }
        [KouPluginFunction(nameof(DetectLanguage), ActivateKeyword = "detect|语种", Help = "检测输入文本的语种")]
        public string DetectLanguage(string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return "没有输入要检测语种的文本";
            BaiduTranslateAPI translator = new BaiduTranslateAPI();
            var result = translator.DetectLanguage(str);
            if (result == null)
            {
                ErrorService.InheritError(this, translator);
                return null;
            }
            else return result;
        }

        [KouPluginFunction(nameof(KouRandomTranslateToZh), ActivateKeyword = "润色|polish", Name = "润（sheng）色（cao）文章", Help = "<原文>[-count 润色次数]")]
        public string KouRandomTranslateToZh(string source)
        {
            if(int.TryParse(Count, out int count))
            {
                return RandomTranslateToZh(source, count);
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
                source = translator.Translate(source, Language.auto, RandomService.EnumRandomGetOne<Language>());
                if (source == null)
                {
                    ErrorService.InheritError(this, translator);
                    return null;
                }
            }
            string result = translator.Translate(source, Language.auto);//最后转为中文
            if (result == null)
            {
                ErrorService.InheritError(this, translator);
                return null;
            }
            return result;
        }

        [KouPluginFunction(nameof(ToBase64), ActivateKeyword = "base64|转base64" ,Help = "转base64")]
        public string ToBase64(string str = null)
        {
            return WebTool.EncodeBase64(str);
        }
        [KouPluginFunction(nameof(DecodeBase64), ActivateKeyword = "解base64|解码base64", Help = "转base64")]
        public string DecodeBase64(string str)
        {
            return WebTool.DecodeBase64(str);
        }

        [KouPluginFunction(nameof(ToTraditional), ActivateKeyword = "转繁体|fanti", Help = "转繁体")]
        public string ToTraditional(string str = null)
        {
            return StringTool.ToTraditional(str);
        }

        [KouPluginFunction(nameof(ToSimplified), ActivateKeyword = "转简体|jianti", Help = "转简体")]
        public string ToSimplified(string str = null)
        {
            return StringTool.ToSimplified(str);
        }

        [KouPluginFunction(nameof(ToFullWidth), ActivateKeyword = "转全角", Help = "转全角")]
        public string ToFullWidth(string str = null)
        {
            return StringTool.ToFullWidth(str);
        }
        [KouPluginFunction(nameof(ToHalfWidth), ActivateKeyword = "转半角", Help = "转半角")]
        public string ToHalfWidth(string str = null)
        {
            return StringTool.ToHalfWidth(str);
        }
        [KouPluginFunction(nameof(ToMD5), ActivateKeyword = "转MD5|MD5|md5|转md5", Help = "计算MD5值")]
        public string ToMD5(string str = null)
        {
            return WebTool.EncryptStringMD5(str);
        }
        [KouPluginFunction(nameof(ToZh), ActivateKeyword = "转中文", Help = "将某任何语言的文本转换为中文")]
        public string ToZh(string str)
        {
            BaiduTranslateAPI baiduTranslateAPI = new BaiduTranslateAPI();
            return baiduTranslateAPI.Translate(str);
        }
        [KouPluginFunction(nameof(ToJp), ActivateKeyword = "转日语|转日文", Help = "将某任何语言的文本转换为日文")]
        public string ToJp(string str)
        {
            BaiduTranslateAPI baiduTranslateAPI = new BaiduTranslateAPI();
            return baiduTranslateAPI.Translate(str, Language.auto, Language.jp);
        }

        [KouPluginFunction(nameof(ToDateTime), ActivateKeyword = "转时间", Help = "将时间戳转换为时间形式\ntype参数支持js，默认unix时间戳")]
        public string ToDateTime(string str)
        {
            if (str.IsNullOrWhiteSpace()) return null;
            if (str.Equals("now", StringComparison.OrdinalIgnoreCase))
            {
                return DateTime.Now.ToString();
            }
            TimeStampType timeStampType = TimeStampType.unix;
            if (!Type.IsNullOrWhiteSpace())
            {
                if (str.Equals("js", StringComparison.OrdinalIgnoreCase) || str.Equals("javascript", StringComparison.OrdinalIgnoreCase))
                    timeStampType = TimeStampType.javascript;
            }
            return str.ToDateTime(timeStampType).ToString();
        }
        [KouPluginFunction(nameof(ToUnixTime), ActivateKeyword = "转时间戳", Help = "将日期转换为时间戳格式\ntype参数支持js，默认unix时间戳")]
        public string ToUnixTime(string str)
        {
            if (str.IsNullOrWhiteSpace()) return null;
            if (str.Equals("now", StringComparison.OrdinalIgnoreCase))
            {
                return DateTime.Now.ToTimeStamp().ToString();
            }
            TimeStampType timeStampType = TimeStampType.unix;
            if (!Type.IsNullOrWhiteSpace())
            {
                if (str.Equals("js", StringComparison.OrdinalIgnoreCase) || str.Equals("javascript", StringComparison.OrdinalIgnoreCase))
                    timeStampType = TimeStampType.javascript;
            }
            return str.ToUnixTimeStamp(timeStampType).ToString();
        }
        [KouPluginFunction(nameof(ToTotalSecond), ActivateKeyword = "转秒数", Help = "将输入的时间转换为对应秒数")]
        public string ToTotalSecond(string time)
        {
            if (time.IsNullOrWhiteSpace()) return 0.ToString();
            TimeSpan timeSpan = new TimeSpan();
            if (time.TryGetTimeSpan(out TimeSpan timeSpanFormal, false)) timeSpan += timeSpanFormal;
            if (ZhNumber.IsContainZhNumber(time)) time = ZhNumber.ToArabicNumber(time);
            if (StringTool.TryGetTimeSpanFromStr(time, out TimeSpan timeSpanModern)) timeSpan += timeSpanModern;
            if (StringTool.TryGetTimeSpanFromAncientStr(time, out TimeSpan timeSpanAcicent)) timeSpan += timeSpanAcicent;
            return timeSpan.TotalSeconds.ToString();
        }
    }
}

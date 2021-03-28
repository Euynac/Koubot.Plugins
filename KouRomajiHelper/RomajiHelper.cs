using Koubot.SDK.Interface;
using Koubot.SDK.Models.Entities;
using Koubot.SDK.Protocol;
using Koubot.SDK.Services;
using Koubot.Tool.Expand;
using Koubot.Tool.Web;
using Koubot.Tool.Web.RateLimiter;
using KouFunctionPlugin.Romaji.Models;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml;

namespace KouRomajiHelper
{
    /// <summary>
    /// 内部RomajiHelper，低耦合
    /// </summary>
    public class RomajiHelper : IKouError
    {
        private static Dictionary<string, string> RomajiToZhDict;
        public readonly KouContext kouContext = new KouContext();

        public RomajiHelper()
        {
            if (RomajiToZhDict == null)
            {
                RomajiToZhDict = new Dictionary<string, string>();
                foreach (var pluginRomajiPair in kouContext.Set<PluginRomajiPair>().ToList())
                {
                    RomajiToZhDict.Add(pluginRomajiPair.RomajiKey, pluginRomajiPair.ZhValue);
                }
            }
        }
        public void Dispose()
        {
            kouContext.Dispose();
        }
        public ErrorCodes ErrorCode { get; set; }
        public string ErrorMsg { get; set; }

        /// <summary>
        /// 删除罗马音-谐音键值对
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool DeletePair(int id)
        {
            var pair = kouContext.Set<PluginRomajiPair>().SingleOrDefault(x => x.Id == id);
            if (pair == null) return false;
            kouContext.Remove(pair);
            RomajiToZhDict.Remove(pair.RomajiKey);
            return kouContext.SaveChanges() > 0;
        }


        /// <summary>
        /// 增加罗马音-谐音键值对，若本身存在返回ID
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool AddPair(string key, string value, out string sqlValue, out int id)
        {
            var pair = kouContext.Set<PluginRomajiPair>().SingleOrDefault(x => x.RomajiKey == key);
            if (pair == null)
            {
                PluginRomajiPair pluginRomajiPair = new PluginRomajiPair { RomajiKey = key, ZhValue = value };
                kouContext.Add(pluginRomajiPair);
                bool result = kouContext.SaveChanges() > 0;
                if (result) RomajiToZhDict.Add(key, value);
                id = pluginRomajiPair.Id;
                sqlValue = key;
                return result;
            }
            else
            {
                sqlValue = pair.RomajiKey;
                id = pair.Id;
                return false;
            }
        }


        /// <summary>
        /// 调用罗马音API
        /// </summary>
        /// <param name="japanese"></param>
        /// <returns></returns>
        public string CallAPI(string japanese)
        {
            if (japanese.IsNullOrWhiteSpace()) return null;
            string result;
            using (var limiter = new LeakyBucketRateLimiter(nameof(KouRomajiHelper), 1))
            {
                if (!limiter.CanRequest())
                {
                    this.InheritError(limiter, "发生在" + nameof(KouRomajiHelper) + "中的" + nameof(CallAPI));
                }
                string data = "mode=japanese&q=" + HttpUtility.UrlEncode(japanese);
                result = WebHelper.HttpPost("http://www.kawa.net/works/ajax/romanize/romanize.cgi ", data, WebHelper.WebContentType.General);
            }
            return result;
        }

        /// <summary>
        /// 处理API结果
        /// </summary>
        /// <param name="xmlResult"></param>
        /// <returns></returns>
        public List<List<KeyValuePair<string, string>>> ParseXml(string xmlResult)
        {
            if (xmlResult.IsNullOrWhiteSpace()) return null;
            List<List<KeyValuePair<string, string>>> romajiResults = new List<List<KeyValuePair<string, string>>>();
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xmlResult);
            XmlNode ul = xmlDocument.SelectSingleNode("ul");
            var liList = ul.ChildNodes;
            foreach (XmlNode li in liList)
            {
                List<KeyValuePair<string, string>> romajiLine = new List<KeyValuePair<string, string>>();//<日语，罗马音>
                foreach (XmlNode span in li)
                {

                    XmlElement xmlElement = (XmlElement)span;
                    string jap = xmlElement.InnerText;
                    string romaji = "";
                    if (xmlElement.HasAttribute("title"))
                    {
                        romaji = xmlElement.GetAttribute("title");
                    }
                    KeyValuePair<string, string> pair = new KeyValuePair<string, string>(jap, romaji);
                    romajiLine.Add(pair);
                }
                romajiResults.Add(romajiLine);
            }
            return romajiResults;
        }

        /// <summary>
        /// 罗马音转中文谐音
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public string ToZhHomophonic(string str)
        {
            List<string> romajiList = str.Split(' ').ToList();
            if (!romajiList.IsNullOrEmptySet())
            {
                string ret = "";
                foreach (var romaji in romajiList)
                {
                    if (RomajiToZhDict.ContainsKey(romaji))
                    {
                        ret += RomajiToZhDict[romaji] + " ";
                    }
                    else
                    {
                        ret += ParseLongRomaji(romaji) + " ";
                    }
                }
                return ret.Trim();
            }
            return null;
        }

        /// <summary>
        /// 转中文谐音
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public string ToZhHomophonic(List<List<KeyValuePair<string, string>>> result, bool needDetail = false)
        {
            string ret = "";
            foreach (var line in result)
            {
                foreach (var word in line)
                {
                    if (!word.Value.IsNullOrWhiteSpace()) //存在罗马音的
                    {
                        List<string> romajiList = word.Value.Split('/').ToList();//多音字的转成list
                        if (needDetail) ret += word.Key + "(";
                        foreach (var romaji in romajiList)
                        {
                            if (RomajiToZhDict.ContainsKey(romaji))
                            {
                                if (needDetail) ret += romaji;
                                ret += RomajiToZhDict[romaji] + "/";
                            }
                            else
                            {
                                ret += ParseLongRomaji(romaji) + "/";
                                continue;
                            }
                        }
                        if (ret.EndsWith("/"))
                        {
                            ret = ret.Remove(ret.Length - 1);
                        }
                        if (needDetail) ret += ")";
                        ret += " ";
                    }
                    else ret += word.Key;

                }
                ret = ret.Trim();
                ret += "\n";
            }
            return ret.Trim();
        }

        /// <summary>
        /// 仅转罗马音
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public string OnlyRomaji(List<List<KeyValuePair<string, string>>> result)
        {
            string ret = "";
            foreach (var line in result)
            {
                foreach (var word in line)
                {
                    if (word.Value.IsNullOrWhiteSpace())
                    {
                        ret += word.Key;
                    }
                    else ret += " " + word.Value;

                }
                ret = ret.Trim();
                ret += "\n";
            }
            return ret.Trim();
        }

        /// <summary>
        /// 转罗马音，并保留原日文
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        public string RomajiAndJapanese(List<List<KeyValuePair<string, string>>> result)
        {
            string ret = "";
            foreach (var line in result)
            {
                foreach (var word in line)
                {
                    if (word.Value.IsNullOrWhiteSpace())
                    {
                        ret += word.Key;
                    }
                    else ret += " " + $"{word.Key}({word.Value})";
                }
                ret = ret.Trim();
                ret += "\n";
            }
            return ret.Trim();
        }

        /// <summary>
        /// 将长罗马音分析为中文
        /// </summary>
        /// <param name="longRomaji"></param>
        /// <returns></returns>
        public string ParseLongRomaji(string longRomaji)
        {
            //List<string> initials = new List<string> //声母表
            //{
            //    "b","p","m","f","d","t","n","l","g","k","h","j","q","x","zh","ch","sh","r","z","c","s","y","w"
            //};
            string result = "";
            longRomaji = longRomaji.Replace("tt", "t");
            longRomaji = longRomaji.ToLower();
            List<string> romajiList = RomajiToZhDict.Keys.ToList();
            romajiList.Sort(CompareUseLengthDesc);
            while (longRomaji != string.Empty)
            {
                bool hasRomaji = false;
                foreach (var romaji in romajiList)
                {
                    if (longRomaji.StartsWith(romaji))
                    {
                        result += RomajiToZhDict[romaji];
                        longRomaji = longRomaji.Remove(0, romaji.Length);
                        hasRomaji = true;
                        break;
                    }
                }

                if (!hasRomaji && longRomaji.Length > 0)
                {
                    result += longRomaji.Substring(0, 1);
                    longRomaji = longRomaji.Remove(0, 1);
                }
            }
            return result;
        }

        /// <summary>
        /// 按照字符串长度降序
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        private int CompareUseLengthDesc(string x, string y)
        {
            return y.Length.CompareTo(x.Length);
        }

    }
}

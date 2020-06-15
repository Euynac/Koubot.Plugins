using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KouFunctionPlugin.Romaji.Models;
using System.IO;
using System.Reflection;
using System.Web;
using System.Xml;
using Xyz.Koubot.AI.SDK.General;
using Xyz.Koubot.AI.SDK.Interface;
using Xyz.Koubot.AI.SDK.General.Mysql;
using Xyz.Koubot.AI.SDK.Protocol;
using Xyz.Koubot.AI.SDK.Models.Sql.PlugIn;
using Xyz.Koubot.AI.SDK.Tool;
using Xyz.Koubot.AI.SDK.Tool.Web;
namespace KouRomajiHelper
{
    /// <summary>
    /// 内部RomajiHelper，低耦合
    /// </summary>
    public class RomajiHelper : IErrorAvailable
    {
        public static Dictionary<string, string> RomajiToZhDict { get; set; } = new Dictionary<string, string>();
        public static Dictionary<string, int> RomajiIDDict { get; set; } = new Dictionary<string, int>();
        public ErrorCodes ErrorCode { get; set; }
        public string ExtraErrorMessage { get; set; }

        /// <summary>
        /// 删除罗马音-谐音键值对
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool DeletePair(int id, string romaji)
        {
            RomajiModel romajiModel = new RomajiModel { Id = id };
            using (MysqlDataService mysqlDataService = new MysqlDataService())
            {
                int result = mysqlDataService.AlterModelIntoSql<RomajiModel>(RomajiModel.ROMAJI_PAIR, romajiModel, SqlOperation.DELETE);
                if (result <= 0) return false;
            }
            RomajiToZhDict.Remove(romaji);
            RomajiIDDict.Remove(romaji);
            return true;
        }


        /// <summary>
        /// 增加罗马音-谐音键值对，若本身存在返回ID
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool AddPair(string key, string value, out string sqlValue, out int id)
        {
            try
            {
                RomajiModel romajiModel = new RomajiModel
                {
                    Romaji_key = key,
                    Zh_value = value
                };

                using (MysqlDataService mysqlDataService = new MysqlDataService())
                {
                    //检查是否存在
                    var list = mysqlDataService.FetchModelListFromSql<RomajiModel>($"select * from {RomajiModel.ROMAJI_PAIR} where {nameof(RomajiModel.Romaji_key).ToLower()} = \"{key}\"");
                    if (list != null && list.Count > 0)
                    {
                        sqlValue = list[0].Zh_value;
                        id = list[0].Id;
                        return false;
                    }
                    //不存在再增加
                    int result = mysqlDataService.AlterModelIntoSql(RomajiModel.ROMAJI_PAIR, romajiModel, SqlOperation.INSERT);
                    sqlValue = null;
                    if (result <= 0)
                    {
                        id = -1;
                        return false;
                    }
                    RomajiToZhDict.AddOrReplace(key, value);
                    RomajiIDDict.AddOrReplace(key, result);
                    id = result;
                    return true;
                }
            }
            catch (Exception e)
            {
                throw new KouException(ErrorCodes.Plugin_FatalError, "RomajiHelper Addpair出错", e);
            }

        }
        static RomajiHelper()
        {
            LoadRomajiToZh();
        }


        /// <summary>
        /// 加载RomajiToZhList数据
        /// </summary>
        private static void LoadRomajiToZh()
        {
            MysqlDataService mysqlData = new MysqlDataService();
            var list = mysqlData.FetchModelListFromSql<RomajiModel>($"select * from {RomajiModel.ROMAJI_PAIR}");
            if (list != null)
            {
                foreach (var item in list)
                {
                    RomajiToZhDict.Add(item.Romaji_key, item.Zh_value);
                    RomajiIDDict.Add(item.Romaji_key, item.Id);
                }
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
            ApiCallLimiter apiCallLimiter = new ApiCallLimiter(nameof(KouRomajiHelper), LimitingType.LeakyBucket, 1);
            if (!apiCallLimiter.RequestWithRetry())
            {
                ErrorService.InheritError(this, apiCallLimiter);
                ExtraErrorMessage += " 发生在" + nameof(KouRomajiHelper) + "中的" + nameof(CallAPI);
                return null;
            }
            string data = "mode=japanese&q=" + HttpUtility.UrlEncode(japanese);
            var result = WebHelper.HttpPost("http://www.kawa.net/works/ajax/romanize/romanize.cgi ", data, WebHelper.WebContentType.General);
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
            if (romajiList != null)
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

using Koubot.SDK.Protocol.Plugin;
using Koubot.SDK.Services;
using Koubot.Tool.Expand;
using KouFunctionPlugin.Romaji.Models;
using KouRomajiHelper;
using System.Linq;
using static Koubot.SDK.Protocol.KouEnum;

namespace KouFunctionPlugin.Romaji
{
    /// <summary>
    /// Kou专用RomajiHelper
    /// </summary>
    [KouPluginClass(
        Introduction = "罗马音助手",
        Author = "7zou",
        ActivateName = "romaji",
        Title = "罗马音助手",
        PluginType = PluginType.Function)]
    public class KouRomajiHelper : KouPlugin
    {
        [KouPluginParameter(ActivateKeyword = "all", Help = "输出带原日文")]
        public bool All { get; set; }

        [KouPluginParameter(ActivateKeyword = "zh", Help = "输出转中文谐音")]
        public bool Zh { get; set; }

        private readonly RomajiHelper romajiHelper = new RomajiHelper();


        [KouPluginFunction(Name = "日语转罗马音", Help = "输入日文")]
        public override object Default(string str = null)
        {
            if (str.IsNullOrWhiteSpace()) return null;
            var result = romajiHelper.CallAPI(str);
            if (result == null)
            {
                this.InheritError(romajiHelper);
                return null;
            }
            var parsedResult = romajiHelper.ParseXml(result);
            if (Zh) return romajiHelper.ToZhHomophonic(parsedResult);
            if (All) return romajiHelper.RomajiAndJapanese(parsedResult);
            return romajiHelper.OnlyRomaji(parsedResult);
        }
        [KouPluginFunction(ActivateKeyword = "念|读|谐音", Name = "Kou念罗马音", Help = "给Kou罗马音让她念吧，不会的罗马音可以教教")]
        public string KouZhHomophonic(string str)
        {
            if (str.IsNullOrWhiteSpace()) return "说话呀 不然我怎么念嘛";
            return romajiHelper.ToZhHomophonic(str);
        }


        [KouPluginFunction(ActivateKeyword = "add|教|教教", Name = "教教Kou谐音", Help = "添加谐音到数据库 用法：<罗马音，谐音>")]
        public string KouAddPair(string key, string value)
        {
            if (key.IsNullOrWhiteSpace() || value.IsNullOrWhiteSpace()) return "好好教我嘛嘤嘤嘤";
            if (!key.IsMatch("^[a-z]+$"))
            {
                return "听不懂听不懂 我要小写的罗马音 不带空格的那种";
            }
            if (!romajiHelper.AddPair(key, value, out string sqlValue, out int id))
            {
                if (sqlValue != null)
                {
                    return $"我记得学过了，{key}念{sqlValue} (id{id})";
                }
                return "脑袋短路了，没学会诶嘿嘿";
            }
            return $"学会了，不要骗我噢，{key}是念{value}的吧 (id{id})";
        }

        [KouPluginFunction(ActivateKeyword = "delete|忘记|忘掉", Name = "叫Kou忘掉谐音", Help = "从数据库删除 用法：<ID或罗马音>")]
        public string KouDeletePair(string idStr)
        {
            if (!int.TryParse(idStr, out int id))
            {
                var pair = romajiHelper.kouContext.Set<PluginRomajiPair>().SingleOrDefault(x => x.RomajiKey == idStr);
                if (pair != null)
                {
                    id = pair.Id;
                }
            }
            if (id != 0)
            {
                var pair = romajiHelper.kouContext.Set<PluginRomajiPair>().SingleOrDefault(x => x.Id == id);
                if (romajiHelper.DeletePair(id))
                {
                    return $"我好像忘了{pair.RomajiKey}读{pair.ZhValue}";
                }
                return "我不记得有这个";
            }
            return "阿巴阿巴阿巴？";
        }

    }
}

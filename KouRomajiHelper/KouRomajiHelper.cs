
using KouFunctionPlugin.Romaji.Models;
using KouRomajiHelper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;
using Xyz.Koubot.AI.SDK.General;
using Xyz.Koubot.AI.SDK.Interface;
using Xyz.Koubot.AI.SDK.General.Mysql;
using Xyz.Koubot.AI.SDK.Protocol;
using Xyz.Koubot.AI.SDK.Models.Sql.PlugIn;
using Xyz.Koubot.AI.SDK.Tool;
using Xyz.Koubot.AI.SDK.Tool.Web;

namespace KouFunctionPlugin.Romaji
{
    /// <summary>
    /// Kou专用RomajiHelper
    /// </summary>
    public class KouRomajiHelper : IKouPlugin, IKouMysql
    {
        [KouPluginParameter(nameof(All), ActivateKeyword = "all", Help = "输出带原日文", Attributes = KouParameterAttribute.Bool)]
        public bool All { get; set; }

        [KouPluginParameter(nameof(Zh), ActivateKeyword = "zh", Help = "输出转中文谐音", Attributes = KouParameterAttribute.Bool)]
        public bool Zh { get; set; }

        private readonly RomajiHelper romajiHelper = new RomajiHelper();

        public ErrorCodes ErrorCode { get; set; }
        public string ExtraErrorMessage { get; set; }

        public PlugInInfoModel GetPluginInfo()
        {
            PlugInInfoModel plugInInfoModel = new PlugInInfoModel
            {
                Plugin_reflection = nameof(KouRomajiHelper),
                Introduction = "罗马音助手",
                Plugin_author = "7zou",
                Plugin_activate_name = "romaji",
                Plugin_zh_name = "罗马音助手",
                Plugin_type = PluginType.Function,
            };
            return plugInInfoModel;
        }

        [KouPluginFunction(nameof(Default), Name = "日语转罗马音", Help = "输入日文")]
        public string Default(string str = null)
        {
            if (str.IsNullOrWhiteSpace()) return null;
            var result = romajiHelper.CallAPI(str);
            if(result == null)
            {
                ErrorService.InheritError(this, romajiHelper);
                return null;
            }
            var parsedResult = romajiHelper.ParseXml(result);
            if (Zh) return romajiHelper.ToZhHomophonic(parsedResult);
            if (All) return romajiHelper.RomajiAndJapanese(parsedResult);
            return romajiHelper.OnlyRomaji(parsedResult);
        }
        [KouPluginFunction(nameof(KouZhHomophonic), ActivateKeyword ="念|读|谐音", Name = "Kou念罗马音", Help = "给Kou罗马音让她念吧，不会的罗马音可以教教")]
        public string KouZhHomophonic(string str)
        {
            if (str.IsNullOrWhiteSpace()) return "说话呀 不然我怎么念嘛";
            return romajiHelper.ToZhHomophonic(str);
        }


        [KouPluginFunction(nameof(KouAddPair), ActivateKeyword ="add|教|教教", Name = "教教Kou谐音", Help = "添加谐音到数据库 用法：<罗马音，谐音>")]
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

        [KouPluginFunction(nameof(KouDeletePair), ActivateKeyword = "delete|忘记|忘掉", Name ="叫Kou忘掉谐音", Help = "从数据库删除 用法：<ID或罗马音>")]
        public string KouDeletePair(string idStr)
        {
            int.TryParse(idStr, out int id);
            if (id == 0)
            {
                if (RomajiHelper.RomajiIDDict.ContainsKey(idStr))
                {
                    id = RomajiHelper.RomajiIDDict[idStr];
                }
            }
            if(id != 0)
            {
                using (MysqlDataService mysqlDataService = new MysqlDataService())
                {
                    var list = mysqlDataService.FetchModelListFromSql<RomajiModel>($"select * from {RomajiModel.ROMAJI_PAIR} where {nameof(RomajiModel.Id)} = {id}");
                    if (list != null && list.Count > 0)
                    {
                        if (romajiHelper.DeletePair(id, list[0].Romaji_key))
                        {
                            return $"忘了忘了，真的忘了{list[0].Romaji_key}读{list[0].Zh_value}";
                        }
                    }
                    
                }
                return "脑袋短路了，我不记得有这个";
            }
            return "阿巴阿巴阿巴？";



            
        }

      
        public string GetMysqlTableInstallStatement()
        {
            return FileTool.ReadEmbeddedResource("Models.install.sql");
        }

        public string GetMysqlTableUninstallStatement()
        {
            return FileTool.ReadEmbeddedResource("Models.uninstall.sql");
        }
    }
}

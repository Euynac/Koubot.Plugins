using KouGamePlugin.Arcaea.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Xyz.Koubot.AI.SDK.General;
using Xyz.Koubot.AI.SDK.Interface;
using Xyz.Koubot.AI.SDK.Models.Sql.PlugIn;
using Xyz.Koubot.AI.SDK.Models.System;
using Xyz.Koubot.AI.SDK.Protocol;

namespace KouGamePlugin.Arcaea
{
    /// <summary>
    /// KouArcaea插件
    /// </summary>
    public class KouArcaea : IKouPlugin, IWantKouMessage
    {
        public ErrorCodes ErrorCode { get; set; }
        public string ExtraErrorMessage { get; set; }
        public KouMessage kouMessage { get; set; }

        [KouPluginFunction(nameof(Default), Name = "获取当前用户最近一次成绩", Help = "默认功能，需要先绑定")]
        public string Default(string str = null)
        {
            return kouMessage.ToString(DetailLevel.Detail);
        }
        [KouPluginFunction(nameof(KouArcInfo), ActivateKeyword = "info", Help = "<歌曲名/别名> 查询歌曲信息，更先进的功能要使用arcinfo")]
        public string KouArcInfo(string name)
        {
            KouArcaeaInfo arcaeaInfo = new KouArcaeaInfo();
            return arcaeaInfo.Default(name);
        }
        [KouPluginFunction(nameof(KouBindArc), ActivateKeyword = "bind|绑定", Help = "绑定arc账号(暂时只支持ID不支持名字)")]
        public string KouBindArc(string arcID)
        {
            return "施工中";
        }
        [KouPluginFunction(nameof(KouCalConstant), ActivateKeyword = "cal|计算", Name = "计算单曲ptt", Help = "<定数/歌曲名[+难度类型]> <(int)分数>，计算出那个分数的ptt")]
        public string KouCalConstant(string nameOrConstant, int score)
        {
            if (score < 0 || score > 11000000) return "这个分数怎么有点奇怪呢";
            if(double.TryParse(nameOrConstant, out double constant))
            {
                return $"定数{constant}时，{score}分的ptt为{ArcaeaData.CalSongScorePtt(constant, score)}";
            }
            KouArcaeaInfo kouArcaeaInfo = new KouArcaeaInfo
            {
                SongName = nameOrConstant
            };
            var songs = kouArcaeaInfo.GetSatisfiedSong(ratingClass: ArcaeaSongModel.RatingClass.Future);
            string result = "";
            if (songs.Count == 0) return $"找不到是哪个歌";
            else if(songs.Count > 4)
            {
                result = "具体是指下面哪个歌呢？\n";
                for (int i = 0; i < songs.Count && i < 7; i++)
                {
                    result += songs[i].ToString(DetailLevel.Brief) + "\n";
                }
                if (songs.Count > 7) result += "还有" + (songs.Count - 7) + "个结果...";
            }
            else
            {
                foreach (var song in songs)
                {
                    result += $"{song.ToString(DetailLevel.Brief)} {score}分的ptt为{ArcaeaData.CalSongScorePtt(song.Chart_constant, score)}\n";
                }
            }
            return result.Trim();
        }

        public PlugInInfoModel GetPluginInfo()
        {
            return new PlugInInfoModel()
            {
                Plugin_reflection = nameof(KouArcaea),
                Introduction = "提供随机歌曲、计算ptt等功能",
                Plugin_author = "7zou",
                Plugin_activate_name = "arc",
                Plugin_zh_name = "Arcaea助手",
                Plugin_type = PluginType.Game
            };
        }
    }
}

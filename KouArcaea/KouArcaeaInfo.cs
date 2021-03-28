using Koubot.SDK.Interface;
using Koubot.SDK.Models.Entities;
using Koubot.SDK.Protocol;
using Koubot.SDK.Protocol.Plugin;
using Koubot.SDK.Tool;
using Koubot.Tool.Expand;
using Koubot.Tool.Model;
using Koubot.Tool.Random;
using Koubot.Tool.String;
using KouGamePlugin.Arcaea.Models;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using static Koubot.SDK.Protocol.KouEnum;

namespace KouGamePlugin.Arcaea
{
    /// <summary>
    /// KouArcaea歌曲数据类
    /// </summary>
    [KouPluginClass("arcinfo", "[obsolete]Arcaea歌曲数据服务",
        Introduction = "提供歌曲详细信息查询、随机歌曲功能，可限定条件",
        Author = "7zou",
        PluginType = PluginType.Game)]
    [Obsolete("已经升级到AutoModel")]
    public class KouArcaeaInfo : KouPlugin, IDisposable
    {
        private readonly KouContext _kouContext = new KouContext();
        #region Kou插件方法
        [KouPluginParameter(Name = "难度类型", ActivateKeyword = "type|class|难度类型", DefaultContent = "future", Help = "指定谱面难度类型")]
        public string RatingClass { get; set; }
        [KouPluginParameter(ActivateKeyword = "rating|难度", Name = "谱面难度", Help = "指定谱面难度(9、9+、10等)")]
        public string Rating { get; set; }
        [KouPluginParameter(ActivateKeyword = "designer|谱师", Name = "谱师", Help = "指定谱面的作者")]
        public string ChartDesigner { get; set; }
        [KouPluginParameter(ActivateKeyword = "artist|曲师", Name = "曲师", Help = "指定歌曲的作者")]
        public string SongArtist { get; set; }
        [KouPluginParameter(ActivateKeyword = "jacket|画师", Name = "画师", Help = "指定歌曲封面画师")]
        public string JacketDesigner { get; set; }
        [KouPluginParameter(ActivateKeyword = "name|歌名|曲名", Name = "曲名", Help = "指定歌曲的名字")]
        public string SongName { get; set; }
        [KouPluginParameter(ActivateKeyword = "const|定数", Name = "定数", Help = "指定谱面定数")]
        public string ChartConstant { get; set; }
        [KouPluginParameter(ActivateKeyword = "count", Name = "结果数量", Help = "获取数量，最多20个，详细最多7个")]
        public int Count { get; set; } = -1;
        [KouPluginParameter(ActivateKeyword = "all", Name = "详细", Help = "显示详细")]
        public bool All { get; set; }
        [KouPluginParameter(ActivateKeyword = "notes", Name = "总键数", Help = "指定键数")]
        public string NotesCount { get; set; }
        [KouPluginParameter(ActivateKeyword = "length|time", Name = "歌曲长度", Help = "指定歌曲长度")]
        public string SongLength { get; set; }
        [KouPluginParameter(ActivateKeyword = "bpm", Name = "BPM", Help = "指定歌曲bpm")]
        public string SongBPM { get; set; }

        /// <summary>
        /// 歌曲英文id，用于确定特定的歌曲
        /// </summary>
        public string SongEnID { get; set; }

        [KouPluginFunction(Name = "查询歌曲信息", Help = "<歌曲名/别名[+难度类型]> 默认功能，按照歌曲名查询")]
        public override object Default(string name = null)
        {
            if (_kouContext.Set<PluginArcaeaSong>().IsNullOrEmptySet()) return "曲库为空";//BUG 更新后无法进行判断是否为空
            if (SystemExpand.All(string.IsNullOrWhiteSpace, name, ChartConstant,
                SongName, SongArtist, ChartDesigner, RatingClass, Rating, NotesCount, JacketDesigner, SongLength,
                SongBPM)) return "嗯？";
            if (SongName.IsNullOrWhiteSpace())
            {
                SongName = name;
            }

            List<PluginArcaeaSong> satisfiedSongs = GetSatisfiedSong();
            if (satisfiedSongs.IsNullOrEmptySet()) return "找不到符合条件的歌曲";

            return satisfiedSongs.ToAutoPageSetString();
            //else if (satisfiedSongs.Count == 1)
            //{
            //    return satisfiedSongs.First().ToString(FormatType.Detail);
            //}
            //else
            //{
            //    if (Count == -1) Count = 10;
            //    Count = Count.LimitInRange(1, 20);
            //    string result = "";
            //    if (All)
            //    {
            //        Count = Count.LimitInRange(7);
            //        for (int i = 0; i < Count && i < satisfiedSongs.Count; i++)
            //        {
            //            result += $"-------{AutoFormatSong(satisfiedSongs[i])}\n";
            //        }
            //    }
            //    else
            //    {
            //        for (int i = 0; i < Count && i < satisfiedSongs.Count; i++)
            //        {
            //            result += $"{satisfiedSongs[i].ToString(FormatType.Brief)}\n";
            //        }
            //    }
            //    result += satisfiedSongs.Count - Count > 0 ? $"还有{satisfiedSongs.Count - Count}个结果..." : null;
            //    return result.Trim();
            //}
        }




        /// <summary>
        /// 随机选曲（可使用条件）
        /// </summary>
        /// <param name="ratingNumStr"></param>
        /// <returns></returns>
        [KouPluginFunction(ActivateKeyword = "random|随机", Name = "随机选曲", Help = "能够限定谱面难度、定数、难度类型的随机选曲")]
        public string KouRandomSong(string ratingNumStr = null)
        {
            try
            {
                if (_kouContext.Set<PluginArcaeaSong>().IsNullOrEmptySet()) return "曲库为空";

                //处理限定难度类型信息
                Regex regex = new Regex("(,|，)?(ftr|pst|prs|byd|byn|future|past|present|beyond|all)(,|，)?", RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
                if (!ratingNumStr.IsNullOrEmpty() && regex.IsMatch(ratingNumStr)) //若是包含难度信息则取出来
                {
                    RatingClass = ratingNumStr.Match("(ftr|pst|prs|byd|byn|future|past|present|beyond|all)", RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
                    ratingNumStr = regex.Replace(ratingNumStr, "", 1).Trim();
                }
                //处理限定难度信息
                MultiSelectionHelper.TryGetMultiSelections(ratingNumStr, out List<string> ratingList, @"^(11|10|[1-9])\+?$");
                PluginArcaeaSong.RatingClass ratingClass = PluginArcaeaSong.RatingClass.Random;
                //支持一个定数信息
                ChartConstant = ratingNumStr.Match(@"\d+\.\d", RegexOptions.None, true);

                if (Count == -1) Count = 1;
                Count = Count.LimitInRange(1, 20);
                var songlist = RandomGetSong(ratingList, ratingClass, Count);
                if (songlist.IsNullOrEmptySet()) return "找不到这样的歌曲";
                if (All)
                {
                    return songlist.First().ToString(FormatType.Detail);
                }
                string result = "";
                foreach (var song in songlist)
                {
                    result += song.ToString(FormatType.Brief) + "\n";
                }
                return result.Trim();
            }
            catch (Exception e)
            {
                throw new KouException(ErrorCodes.Plugin_FatalError, "随机选曲错误", e);
            }

        }



        #endregion

        #region 一般方法

        /// <summary>
        /// 获取满足当前条件的歌曲
        /// </summary>
        /// <param name="ratingList"></param>
        /// <param name="ratingClass"></param>
        /// <returns></returns>
        public List<PluginArcaeaSong> GetSatisfiedSong(List<string> ratingList = null, PluginArcaeaSong.RatingClass ratingClass = PluginArcaeaSong.RatingClass.Random)
        {
            if (_kouContext.Set<PluginArcaeaSong>().IsNullOrEmptySet()) return null;
            //处理限定难度信息
            if (ratingList.IsNullOrEmptySet()) MultiSelectionHelper.TryGetMultiSelections(Rating, out ratingList, @"^(11|10|[1-9])\+?$");
            //处理限定难度类型信息
            Regex regex = new Regex("(,|，)?(ftr|pst|prs|byd|byn|future|past|present|beyond|all)(,|，)?", RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
            if (!SongName.IsNullOrEmpty() && regex.IsMatch(SongName)) //若是歌名上包含难度信息则取出来
            {
                RatingClass = SongName.Match("(ftr|pst|prs|byd|byn|future|past|present|beyond|all)", RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
                SongName = regex.Replace(SongName, "", 1).Trim();
            }
            if (!RatingClass.IsNullOrEmpty())//RatingClass属性优先级比该函数中的ratingClss优先级大
            {
                if (!Enum.TryParse(RatingClass, out ratingClass))
                {
                    if (PluginArcaeaSong.RatingClassNameList.ContainsKey(RatingClass.ToLower()))
                    {
                        ratingClass = PluginArcaeaSong.RatingClassNameList[RatingClass.ToLower()];
                    }
                }
            }
            //设定难度类型默认值为FTR
            if (ratingList.IsNullOrEmptySet() && RatingClass.IsNullOrWhiteSpace() && NotesCount.IsNullOrWhiteSpace() && ChartConstant.IsNullOrWhiteSpace() && Rating.IsNullOrWhiteSpace()) //不指定难度类型则默认是future难度，后面是不默认ftr
            {
                ratingClass = PluginArcaeaSong.RatingClass.Future;
            }
            //处理歌名信息
            if (!SongName.IsNullOrWhiteSpace())
            {
                SongName = SongName.Trim().ToLower();
                string songEnID = _kouContext.Set<PluginArcaeaSongAnotherName>().FirstOrDefault(s => s.AnotherName == SongName)?.SongEnId;
                if (songEnID != null)
                {
                    SongEnID = songEnID;
                    SongName = null;
                }
            }


            //将需要用到的过滤器放到过滤容器里
            FilterContainer<PluginArcaeaSong> filterContainer = new FilterContainer<PluginArcaeaSong>();
            //使用默认ModelFilter
            filterContainer.AddAutoModelFilter(nameof(PluginArcaeaSong.SongTitle), SongName, FilterType.Default, SortType.Ascending);//增加Song_title字段的默认filter
            filterContainer.AddAutoModelFilter(nameof(PluginArcaeaSong.SongEnId), SongEnID, FilterType.Exact);
            if (ratingClass != PluginArcaeaSong.RatingClass.Random) filterContainer.AddAutoModelFilter(nameof(PluginArcaeaSong.ChartRatingClass), ratingClass, FilterType.SupportValueDefault);
            filterContainer.AddAutoModelFilter(nameof(PluginArcaeaSong.SongArtist), SongArtist, FilterType.Default, SortType.Ascending);
            filterContainer.AddAutoModelFilter(nameof(PluginArcaeaSong.ChartDesigner), ChartDesigner, FilterType.Default, SortType.Ascending);
            filterContainer.AddAutoModelFilter(nameof(PluginArcaeaSong.JacketDesigner), JacketDesigner, FilterType.Default, SortType.Ascending);
            filterContainer.AddAutoModelFilter(nameof(PluginArcaeaSong.ChartAllNotes), NotesCount, FilterType.Default, SortType.StringAuto);
            filterContainer.AddAutoModelFilter(nameof(PluginArcaeaSong.ChartConstant), ChartConstant, FilterType.Default, SortType.StringAuto);
            filterContainer.AddAutoModelFilter(nameof(PluginArcaeaSong.SongLength), SongLength, FilterType.Default, SortType.StringAuto);
            filterContainer.AddAutoModelFilter(nameof(PluginArcaeaSong.SongBpm), SongBPM, FilterType.Interval, SortType.StringAuto);
            filterContainer.ActivateAutoModelFilter();
            //加入自定义的filter
            if (!ratingList.IsNullOrEmptySet()) filterContainer.Add(FilterContainer<PluginArcaeaSong>.Convert(PluginArcaeaSong.RatingNumFilter), ratingList);


            //开始筛选
            var selectedList = _kouContext.Set<PluginArcaeaSong>().ToList();
            selectedList = selectedList.Where(song => filterContainer.StartFilter(song)).ToList();
            var sorter = filterContainer.GetModelSorter();//若有sorter则排序
            if (sorter != null) selectedList.Sort(sorter);
            return selectedList;
        }

        /// <summary>
        /// 随机获取一个歌曲（按照条件）
        /// </summary>
        /// <param name="ratingList">限定的谱面难度</param>
        /// <param name="ratingClass">限定的谱面难度类型</param>
        /// <returns></returns>
        public PluginArcaeaSong RandomGetOneSong(List<string> ratingList = null, PluginArcaeaSong.RatingClass ratingClass = PluginArcaeaSong.RatingClass.Future)
        {
            return RandomGetSong(ratingList, ratingClass, 1)?.First();
        }
        /// <summary>
        /// 随机获取歌曲（按照条件）
        /// </summary>
        /// <param name="ratingList">限定的谱面难度</param>
        /// <param name="ratingClass">限定的谱面难度类型</param>
        /// <param name="count">随机获取的歌曲数量</param>
        /// <returns></returns>
        public List<PluginArcaeaSong> RandomGetSong(List<string> ratingList = null, PluginArcaeaSong.RatingClass ratingClass = PluginArcaeaSong.RatingClass.Future, int count = 1)
        {
            return GetSatisfiedSong(ratingList, ratingClass)?.RandomGetItems(count)?.ToList();
        }






        #region 歌曲格式化
        /// <summary>
        /// 自动格式化歌曲信息
        /// </summary>
        /// <param name="song"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public string AutoFormatSong(PluginArcaeaSong song)
        {
            if (song == null) return null;
            string info = $"{song.SongTitle} [{song.ChartRatingClass} {song.ChartConstant}]\n" +
            (SongArtist.IsNullOrWhiteSpace() ? null : song.SongArtist?.Be($"曲师：{song.SongArtist}\n")) +
            (JacketDesigner.IsNullOrWhiteSpace() ? null : song.JacketDesigner?.Be($"画师：{song.JacketDesigner}\n")) +
            (SongBPM.IsNullOrWhiteSpace() ? null : song.SongBpm?.Be($"BPM：{song.SongBpm}\n")) +
            (SongLength.IsNullOrWhiteSpace() ? null : song.SongLength?.Be($"歌曲长度：{ song.SongLength}\n")) +
            //(song.Song_pack?.Be($"曲包：{song.Song_pack}\n")) +
            (ChartDesigner.IsNullOrWhiteSpace() ? null : song.ChartDesigner?.Be($"谱师：{song.ChartDesigner}\n")) +
            (NotesCount.IsNullOrWhiteSpace() ? null : song.ChartAllNotes?.Be($"note总数：{song.ChartAllNotes}\n"));
            return info.ToString().Trim();
        }

        #endregion

        #region 歌曲别名


        [KouPluginFunction(ActivateKeyword = "learn|教教", Name = "学新的歌曲别名", Help = "教kou一个歌曲的别名。使用方法： <歌曲名> <歌曲别名>")]
        public string KouLearnAnotherName(string songName, string songAnotherName)
        {
            string songEnID = _kouContext.Set<PluginArcaeaSongAnotherName>().Where(name => name.AnotherName == songName).FirstOrDefault()?.SongEnId;
            if (songEnID == null)
            {
                var songs = _kouContext.Set<PluginArcaeaSong>().Where(s => s.SongTitle.Contains(songName) && s.ChartRatingClass == PluginArcaeaSong.RatingClass.Future);
                if (songs.Count() == 0) return $"找不到是哪个歌叫做{SongName}";
                if (songs.Count() > 1) return $"具体是指哪首歌可以叫{songAnotherName}？\n{songs.ToSetString<PluginArcaeaSong>()}";
                songEnID = songs.First().SongEnId;
            }
            var allRatingSongs = _kouContext.Set<PluginArcaeaSong>().Where(s => s.SongEnId == songEnID);


            if (_kouContext.Set<PluginArcaeaSongAnotherName>().Any(x => x.AnotherName == songAnotherName && x.SongEnId == songEnID)) return $"我之前就知道能叫{songAnotherName}了";
            var learnedList = _kouContext.Set<PluginArcaeaSongAnotherName>().Where(x => x.SongEnId == songEnID).ToList();
            string anotherNames = "";
            if (learnedList.Count > 0)
            {
                foreach (var item in learnedList)
                {
                    anotherNames += item.AnotherName + $"({item.AnotherNameId})、";
                }
                anotherNames = anotherNames.TrimEnd('、');
            }

            PluginArcaeaSongAnotherName anotherNameModel = new PluginArcaeaSongAnotherName
            {
                AnotherName = songAnotherName,
                SongEnId = songEnID,
            };
            foreach (var item in allRatingSongs)
            {
                PluginArcaeaSong2anothername song2Anothername = new PluginArcaeaSong2anothername
                {
                    Song = item,
                    AnotherName = anotherNameModel,
                };
                _kouContext.Add(song2Anothername);
            }
            if (_kouContext.SaveChanges() > 0)
            {
                return $"学到许多，{allRatingSongs.First().SongTitle}可以叫做{songAnotherName}({anotherNameModel.AnotherNameId})" + (anotherNames.IsNullOrWhiteSpace() ? null : $"，我还知道它能叫做{anotherNames}！");
            }
            return "啊...不知道为什么没学会";

        }
        [KouPluginFunction(ActivateKeyword = "forget|忘记", Name = "忘记歌曲别名", Help = "叫kou忘掉一个歌曲的别名。使用方法： <歌曲别名|ID>")]
        public string KouForgetAnotherName(string NameOrID)
        {
            if (NameOrID.IsNullOrWhiteSpace()) return "这是叫我忘掉什么嘛";
            PluginArcaeaSongAnotherName songAnotherName;
            if (int.TryParse(NameOrID, out int ID))
            {
                songAnotherName = _kouContext.Set<PluginArcaeaSongAnotherName>().SingleOrDefault(x => x.AnotherNameId == ID);
            }
            else if (_kouContext.Set<PluginArcaeaSongAnotherName>().Any(x => x.AnotherName == NameOrID))
            {
                var names = _kouContext.Set<PluginArcaeaSongAnotherName>().Where(x => x.AnotherName == NameOrID);
                if (names.Count() > 1) return $"具体是删除哪个？\n{names.ToSetString<PluginArcaeaSongAnotherName>()}";
                songAnotherName = names.First();
            }
            else return "我没学过这个的吧";
            _kouContext.Remove(songAnotherName);
            return _kouContext.SaveChanges() > 0 ? "忘掉了" : "...不知道为什么忘不掉";
        }

        public void Dispose()
        {
            _kouContext.Dispose();
        }
        #endregion

        #endregion
    }
}

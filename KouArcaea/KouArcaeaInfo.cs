using KouGamePlugin.Arcaea.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xyz.Koubot.AI.SDK.General;
using Xyz.Koubot.AI.SDK.Interface;
using Xyz.Koubot.AI.SDK.General.Mysql;
using Xyz.Koubot.AI.SDK.Protocol;
using Xyz.Koubot.AI.SDK.Models.Sql.PlugIn;
using Xyz.Koubot.AI.SDK.Tool;
using Xyz.Koubot.AI.SDK.Tool.KouMath;
using Xyz.Koubot.AI.SDK.Tool.Model;
using System.Text.RegularExpressions;

namespace KouGamePlugin.Arcaea
{
    /// <summary>
    /// KouArcaea歌曲数据类
    /// </summary>
    public class KouArcaeaInfo : IKouPlugin, IKouMysql
    {
        #region Kou插件方法
        [KouPluginParameter(nameof(RatingClass),Name = "难度类型", ActivateKeyword = "type|class|难度类型", DefalutContent = "future", Help = "指定谱面难度类型")]
        public string RatingClass { get; set; }
        [KouPluginParameter(nameof(Rating), ActivateKeyword = "rating|难度", Name = "谱面难度", Help = "指定谱面难度(9、9+、10等)", Attributes = KouParameterAttribute.Multi)]
        public string Rating { get; set; }
        [KouPluginParameter(nameof(ChartDesigner), ActivateKeyword = "designer|谱师", Name = "谱师", Help = "指定谱面的作者")]
        public string ChartDesigner { get; set; }
        [KouPluginParameter(nameof(SongArtist), ActivateKeyword = "artist|曲师", Name = "曲师", Help = "指定歌曲的作者")]
        public string SongArtist { get; set; }
        [KouPluginParameter(nameof(JacketDesigner), ActivateKeyword = "jacket|画师", Name = "画师", Help = "指定歌曲封面画师")]
        public string JacketDesigner { get; set; }
        [KouPluginParameter(nameof(SongName), ActivateKeyword = "name|歌名|曲名", Name = "曲名", Help = "指定歌曲的名字")]
        public string SongName { get; set; }
        [KouPluginParameter(nameof(ChartConstant), ActivateKeyword = "const|定数", Name = "定数", Help = "指定谱面定数", Attributes = KouParameterAttribute.Range | KouParameterAttribute.Sort)]
        public string ChartConstant { get; set; }
        [KouPluginParameter(nameof(Count), ActivateKeyword = "count", Name = "结果数量", Help = "获取数量，最多20个，详细最多7个", Attributes = KouParameterAttribute.KouInt)]
        public int Count { get; set; } = -1;
        [KouPluginParameter(nameof(All), ActivateKeyword = "all", Name = "详细", Help = "显示详细", Attributes = KouParameterAttribute.Bool)]
        public bool All { get; set; }
        [KouPluginParameter(nameof(NotesCount), ActivateKeyword = "notes", Name = "总键数", Help = "指定键数", Attributes = KouParameterAttribute.Range | KouParameterAttribute.Sort)]
        public string NotesCount { get; set; }
        [KouPluginParameter(nameof(SongLength), ActivateKeyword = "length|time", Name = "歌曲长度", Help = "指定歌曲长度", Attributes = KouParameterAttribute.Sort | KouParameterAttribute.Range)]
        public string SongLength { get; set; }
        [KouPluginParameter(nameof(SongBPM), ActivateKeyword = "bpm", Name = "BPM", Help = "指定歌曲bpm", Attributes = KouParameterAttribute.Range | KouParameterAttribute.Sort)]
        public string SongBPM { get; set; }

        /// <summary>
        /// 歌曲英文id，用于确定特定的歌曲
        /// </summary>
        public string SongEnID { get; set; }
        public ErrorCodes ErrorCode { get; set; }
        public string ExtraErrorMessage { get; set; }

        [KouPluginFunction(nameof(Default), Name = "查询歌曲信息", Help = "<歌曲名/别名[+难度类型]> 默认功能，按照歌曲名查询")]
        public string Default(string name = null)
        {
            if (ArcaeaData.ArcaeaSongList.IsEmpty())
            {
                return "曲库为空";
            }
            if (name.IsNullOrWhiteSpace() 
                && ChartConstant.IsNullOrWhiteSpace() 
                && SongName.IsNullOrWhiteSpace() 
                && SongArtist.IsNullOrWhiteSpace() 
                && ChartDesigner.IsNullOrWhiteSpace() 
                && RatingClass.IsNullOrWhiteSpace()
                && Rating.IsNullOrWhiteSpace()
                && NotesCount.IsNullOrWhiteSpace()
                && JacketDesigner.IsNullOrWhiteSpace()
                && SongLength.IsNullOrWhiteSpace()
                && SongBPM.IsNullOrWhiteSpace()) return "嗯？";
            if (SongName.IsNullOrWhiteSpace())
            {
                SongName = name;
            }

            List<ArcaeaSongModel> songlist = GetSatisfiedSong();
            if (songlist.IsEmpty()) return "找不到符合条件的歌曲";
            else if(songlist.Count == 1)
            {
                return songlist.First().ToString(DetailFormat);
            }
            else
            {
                if (Count == -1) Count = 10;
                Count = Count.LimitInRange(1, 20);
                string result = "";
                if (All)
                {
                    Count = Count.LimitInRange(7);
                    for (int i = 0; i < Count && i < songlist.Count; i++)
                    {
                        result += $"-------{AutoFormatSong(songlist[i])}\n";
                    }
                }
                else
                {
                    for (int i = 0; i < Count && i < songlist.Count; i++)
                    {
                        result += $"{songlist[i].ToString(DetailLevel.Brief)}\n";
                    }
                }
                result += songlist.Count - Count > 0 ? $"还有{songlist.Count - Count}个结果..." : null;
                return result.Trim();
            }
        }




        /// <summary>
        /// 随机选曲（可使用条件）
        /// </summary>
        /// <param name="ratingNumStr"></param>
        /// <returns></returns>
        [KouPluginFunction(nameof(KouRandomSong), ActivateKeyword = "random|随机", Name = "随机选曲", Help = "能够限定谱面难度、定数、难度类型的随机选曲")]
        public string KouRandomSong(string ratingNumStr = null)
        {
            try
            {
                if (ArcaeaData.ArcaeaSongList.IsEmpty())
                {
                    return "曲库为空";
                }

                //处理限定难度类型信息
                Regex regex = new Regex("(,|，)?(ftr|pst|prs|byd|byn|future|past|present|beyond|all)(,|，)?", RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
                if (!ratingNumStr.IsNullOrEmpty() && regex.IsMatch(ratingNumStr)) //若是包含难度信息则取出来
                {
                    RatingClass = ratingNumStr.Match("(ftr|pst|prs|byd|byn|future|past|present|beyond|all)", RegexOptions.IgnoreCase | RegexOptions.RightToLeft);
                    ratingNumStr = regex.Replace(ratingNumStr, "", 1).Trim();
                }
                //处理限定难度信息
                MultiSelectionHelper.TryGetMultiSelections(ratingNumStr, out List<string> ratingList, @"^(11|10|[1-9])\+?$");
                ArcaeaSongModel.RatingClass ratingClass = ArcaeaSongModel.RatingClass.Random;
                //支持一个定数信息
                ChartConstant = ratingNumStr.Match(@"\d+\.\d", RegexOptions.None, true);

                if (Count == -1) Count = 1;
                Count = Count.LimitInRange(1, 20);
                var songlist = RandomGetSong(ratingList, ratingClass, Count);
                if (songlist.IsEmpty()) return "找不到这样的歌曲";
                if (All)
                {
                    return songlist.First().ToString(DetailFormat);
                }
                string result = "";
                foreach (var song in songlist)
                {
                    result += song.ToString(DetailLevel.Brief) + "\n";
                }
                return result.Trim();
            }
            catch (Exception e)
            {
                throw new KouException(ErrorCodes.Plugin_FatalError, "随机选曲错误", e);
            }

        }
        public string GetMysqlTableInstallStatement()
        {
            return FileTool.ReadEmbeddedResource("Models.install.sql");
        }

        public string GetMysqlTableUninstallStatement()
        {
            return FileTool.ReadEmbeddedResource("Models.uninstall.sql");
        }


        public PlugInInfoModel GetPluginInfo()
        {
            return new PlugInInfoModel()
            {
                Plugin_reflection = nameof(KouArcaeaInfo),
                Introduction = "提供歌曲详细信息查询、随机歌曲功能，可限定条件",
                Plugin_author = "7zou",
                Plugin_activate_name = "arcinfo",
                Plugin_zh_name = "Arcaea歌曲数据服务",
                Plugin_type = PluginType.Game
            };
        }

        #endregion

        #region 一般方法
        
        /// <summary>
        /// 获取满足当前条件的歌曲
        /// </summary>
        /// <param name="ratingList"></param>
        /// <param name="ratingClass"></param>
        /// <returns></returns>
        public List<ArcaeaSongModel> GetSatisfiedSong(List<string> ratingList = null, ArcaeaSongModel.RatingClass ratingClass = ArcaeaSongModel.RatingClass.Random)
        {
            if (ArcaeaData.ArcaeaSongList.IsEmpty())
            {
                return null;
            }
            //处理限定难度信息
            if (ratingList.IsEmpty()) MultiSelectionHelper.TryGetMultiSelections(Rating, out ratingList, @"^(11|10|[1-9])\+?$");
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
                    if (ArcaeaSongModel.RatingClassNameList.ContainsKey(RatingClass.ToLower()))
                    {
                        ratingClass = ArcaeaSongModel.RatingClassNameList[RatingClass.ToLower()];
                    }
                }
            }
            //设定难度类型默认值为FTR
            if (ratingList.IsEmpty() && RatingClass.IsNullOrWhiteSpace() && NotesCount.IsNullOrWhiteSpace() && ChartConstant.IsNullOrWhiteSpace() && Rating.IsNullOrWhiteSpace()) //不指定难度类型则默认是future难度，后面是不默认ftr
            {
                ratingClass = ArcaeaSongModel.RatingClass.Future;
            }
            //处理歌名信息
            if (!SongName.IsNullOrWhiteSpace())
            {
                SongName = SongName.Trim().ToLower();
                if (ArcaeaData.ArcaeaSongAnotherNameDict.TryGetValue(SongName, out string songEnID))
                {
                    SongEnID = songEnID;
                    SongName = "";
                }
            }


            //将需要用到的过滤器放到过滤容器里
            FilterContainer<ArcaeaSongModel> filterContainer = new FilterContainer<ArcaeaSongModel>();
            //使用默认ModelFilter
            filterContainer.AddAutoModelFilter(nameof(ArcaeaSongModel.Song_title), SongName, FilterType.Default, SortType.Ascending);//增加Song_title字段的默认filter
            filterContainer.AddAutoModelFilter(nameof(ArcaeaSongModel.Song_en_id), SongEnID, FilterType.Exact);
            if (ratingClass != ArcaeaSongModel.RatingClass.Random) filterContainer.AddAutoModelFilter(nameof(ArcaeaSongModel.Chart_rating_class), ratingClass, FilterType.SupportValueDefault);
            filterContainer.AddAutoModelFilter(nameof(ArcaeaSongModel.Song_artist), SongArtist, FilterType.Default, SortType.Ascending);
            filterContainer.AddAutoModelFilter(nameof(ArcaeaSongModel.Chart_designer), ChartDesigner, FilterType.Default, SortType.Ascending);
            filterContainer.AddAutoModelFilter(nameof(ArcaeaSongModel.Jacket_designer), JacketDesigner, FilterType.Default, SortType.Ascending);
            filterContainer.AddAutoModelFilter(nameof(ArcaeaSongModel.Chart_all_notes), NotesCount, FilterType.Default, SortType.StringAuto);
            filterContainer.AddAutoModelFilter(nameof(ArcaeaSongModel.Chart_constant), ChartConstant, FilterType.Default, SortType.StringAuto);
            filterContainer.AddAutoModelFilter(nameof(ArcaeaSongModel.Song_length), SongLength, FilterType.Default, SortType.StringAuto);
            filterContainer.AddAutoModelFilter(nameof(ArcaeaSongModel.Song_bpm), SongBPM, FilterType.Interval, SortType.StringAuto);
            filterContainer.ActivateAutoModelFilter();
            //加入自定义的filter
            if (!ratingList.IsEmpty()) filterContainer.Add(FilterContainer<ArcaeaSongModel>.Convert(ArcaeaSongModel.RatingNumFilter), ratingList);


            //开始筛选
            var selectedList = ArcaeaData.ArcaeaSongList.Where(song => filterContainer.StartFilter(song))?.ToList();
            var sorter = filterContainer.GetModelSorter();//若有sorter则排序
            if(sorter != null ) selectedList.Sort(sorter);
            return selectedList;
        }

        public delegate bool ModelFilter<in T>(T modelInstance, object value);
        /// <summary>
        /// 随机获取一个歌曲（按照条件）
        /// </summary>
        /// <param name="ratingList">限定的谱面难度</param>
        /// <param name="ratingClass">限定的谱面难度类型</param>
        /// <returns></returns>
        public ArcaeaSongModel RandomGetOneSong(List<string> ratingList = null, ArcaeaSongModel.RatingClass ratingClass = ArcaeaSongModel.RatingClass.Future)
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
        public List<ArcaeaSongModel> RandomGetSong(List<string> ratingList = null, ArcaeaSongModel.RatingClass ratingClass = ArcaeaSongModel.RatingClass.Future, int count = 1)
        {
            if (ArcaeaData.ArcaeaSongList.IsEmpty())
            {
                return null;
            }
            return GetSatisfiedSong(ratingList, ratingClass)?.RandomGetItems(count)?.ToList();
        }






        #region 歌曲格式化
        readonly Func<ArcaeaSongModel, string> DetailFormat = (x) =>
        {
            //获取所有歌曲别名
            var anotherNameList = ArcaeaData.GetSongAllAnotherName(x.Song_en_id);
            string allAnotherName = "";
            if (anotherNameList != null)
            {
                foreach (var name in anotherNameList)
                {
                    allAnotherName += name + "，";
                }
                allAnotherName = allAnotherName.TrimEnd('，');
            }
            return x.ToString(DetailLevel.Detail) + (allAnotherName.IsNullOrEmpty() ? null : $"别名：{allAnotherName}\n");
        };

        /// <summary>
        /// 自动格式化歌曲信息
        /// </summary>
        /// <param name="song"></param>
        /// <param name="detailLevel"></param>
        /// <returns></returns>
        public string AutoFormatSong(ArcaeaSongModel song)
        {
            if (song == null) return null;
            string info = $"{song.Song_title} [{song.Chart_rating_class} {song.Chart_constant}]\n" +
            (SongArtist.IsNullOrWhiteSpace() ? null : song.Song_artist.IsNullOrBe($"曲师：{song.Song_artist}\n")) +
            (JacketDesigner.IsNullOrWhiteSpace() ? null : song.Jacket_designer.IsNullOrBe($"画师：{song.Jacket_designer}\n")) +
            (SongBPM.IsNullOrWhiteSpace() ? null : song.Song_bpm.IsNullOrBe($"BPM：{song.Song_bpm}\n")) +
            (SongLength.IsNullOrWhiteSpace() ? null : song.Song_length.IsDefaultOrBe($"歌曲长度：{ song.Song_length}\n")) +
            //(song.Song_pack.IsNullOrBe($"曲包：{song.Song_pack}\n")) +
            (ChartDesigner.IsNullOrWhiteSpace() ? null : song.Chart_designer.IsNullOrBe($"谱师：{song.Chart_designer}\n")) +
            (NotesCount.IsNullOrWhiteSpace() ? null : song.Chart_all_notes.IsDefaultOrBe($"note总数：{song.Chart_all_notes}\n"));
            return info.ToString().Trim();
        }

        #endregion

        #region 歌曲别名


        [KouPluginFunction(nameof(KouLearnAnotherName), ActivateKeyword = "learn|教教", Name = "学新的歌曲别名", Help = "教kou一个歌曲的别名。使用方法： <歌曲名> <歌曲别名>")]
        public string KouLearnAnotherName(string songName, string songAnotherName)
        {
            if (!ArcaeaData.ArcaeaSongAnotherNameDict.TryGetValue(songName, out string songEnID))
            {
                SongName = songName;
                var songs = GetSatisfiedSong(ratingClass: ArcaeaSongModel.RatingClass.Future);
                if (songs.Count != 1) return $"找不到是哪个歌能叫{songAnotherName}诶";
                songEnID = songs.First().Song_en_id;
            }
            if (ArcaeaData.ArcaeaSongAnotherNameDict.ContainsKey(songAnotherName)) return $"我之前就知道能叫{songAnotherName}了";
            using (LearnableModel<ArcaeaSongAnotherNameModel> learning = new LearnableModel<ArcaeaSongAnotherNameModel>())
            {
                learning.HasLearned(new KeyValuePair<string, object>(nameof(ArcaeaSongAnotherNameModel.Song_en_id), songEnID), out List<ArcaeaSongAnotherNameModel> learnedList);
                string anotherNames = "";
                if (learnedList.Count > 0)
                {
                    foreach (var song in learnedList)
                    {
                        anotherNames += song.Another_name + $"({song.Another_name_id})、";
                    }
                    anotherNames = anotherNames.TrimEnd('、');
                }
                string name = "";
                foreach (var song in ArcaeaData.ArcaeaSongList)
                {
                    if (song.Song_en_id == songEnID)
                    {
                        name = song.Song_title;
                        break;
                    }
                }
                if (learning.Learn(new ArcaeaSongAnotherNameModel() { Song_en_id = songEnID, Another_name = songAnotherName }, out int ID))
                {
                    if (ID == 0)
                    {
                        if (!anotherNames.IsNullOrEmpty()) return $"啊...不知道为什么没学会，但我知道{name}它能叫做{anotherNames}";
                        return "啊...不知道为什么没学会";
                    }
                    ArcaeaData.AddAnotherName(songAnotherName, songEnID);
                    return $"学到许多，{name}可以叫做{songAnotherName}({ID})" + (anotherNames.IsNullOrWhiteSpace() ? null : $"，我还知道它能叫做{anotherNames}！");
                }
                return "啊...不知道为什么没学会";
            }
        }
        [KouPluginFunction(nameof(KouForgetAnotherName), ActivateKeyword = "forget|忘记", Name = "忘记歌曲别名", Help = "叫kou忘掉一个歌曲的别名。使用方法： <歌曲别名|ID>")]
        public static string KouForgetAnotherName(string NameOrID)
        {
            if (NameOrID.IsNullOrWhiteSpace()) return "这是叫我忘掉什么嘛";
            ArcaeaSongAnotherNameModel model = new ArcaeaSongAnotherNameModel();
            if (int.TryParse(NameOrID, out int ID))
            {
                model.Another_name_id = ID;
            }
            else if (ArcaeaData.ArcaeaSongAnotherNameDict.ContainsKey(NameOrID))
            {
                model.Another_name = NameOrID;
            }
            else return "我没学过这个的吧";

            using (LearnableModel<ArcaeaSongAnotherNameModel> forgetting = new LearnableModel<ArcaeaSongAnotherNameModel>())
            {
                List<ArcaeaSongAnotherNameModel> list = new List<ArcaeaSongAnotherNameModel>();
                if (model.Another_name.IsNullOrEmpty())
                {
                    forgetting.HasLearned(new KeyValuePair<string, object>(nameof(ArcaeaSongAnotherNameModel.Another_name_id), model.Another_name_id), out list);
                }
                else
                {
                    forgetting.HasLearned(new KeyValuePair<string, object>(nameof(ArcaeaSongAnotherNameModel.Another_name), model.Another_name), out list);
                }
                if (list.Count != 1) return "记忆出现了奇怪的问题";
                if (forgetting.Forget(model))
                {
                    ArcaeaData.DeleteAnotherName(list.First().Another_name);
                    return "好，我已经忘了";
                }
                return "...不知道为什么忘不掉";
            }


        }
        #endregion

        #endregion




        #region 筛选过滤器（已遗弃 纪念）
        //IEnumerable<ArcaeaSongModel> selectedList = ArcaeaSongList.Where(song =>
        //{
        //    return RatingClassFilter(song, ratingClass)
        //    && ChartConstFilter(song, ChartConstant)
        //    && RatingNumFilter(song, ratingList)
        //    && ChartDesignerFilter(song, ChartDesigner)
        //    && SongArtistFilter(song, SongArtist)
        //    && SongNameFilter(song, SongName)
        //    && SongEnIdFilter(song, SongEnID)
        //    && NotesCountFilter(song, NotesCount)
        //    && JacketDesignerFilter(song, JacketDesigner);
        //});
        ///// <summary>
        ///// 谱面总键数过滤器
        ///// </summary>
        //readonly Func<ArcaeaSongModel, string, bool> NotesCountFilter = (song, notesCount) => 
        //{
        //    if (notesCount.IsNullOrWhiteSpace()) return true;
        //    if (song.Chart_all_notes == default) return false;

        //    if(double.TryParse(notesCount, out double num))//直接等于
        //    {
        //        return song.Chart_all_notes == num;
        //    }
        //    else if(notesCount.GetInterval(out IntervalDouble left, out IntervalDouble right))//区间表示
        //    {
        //        if (song.Chart_all_notes >= left && song.Chart_all_notes <= right) return true;
        //    }
        //    return false;
        //};


        ///// <summary>
        ///// 歌曲英文ID过滤器
        ///// </summary>
        //readonly Func<ArcaeaSongModel, string, bool> SongEnIdFilter = (song, songEnID) =>
        //{
        //    return songEnID.IsNullOrWhiteSpace() ? true : song.Song_en_id.Equals(songEnID, StringComparison.OrdinalIgnoreCase);
        //};

        ///// <summary>
        ///// 歌曲名字过滤器
        ///// </summary>
        //readonly Func<ArcaeaSongModel, string, bool> SongNameFilter = (song, songname) =>
        //{
        //    return songname.IsNullOrWhiteSpace() ? true : song.Song_title.ToLower().Contains(songname.ToLower());
        //};

        ///// <summary>
        ///// 画师（封面设计者）过滤器
        ///// </summary>
        //readonly Func<ArcaeaSongModel, string, bool> JacketDesignerFilter = (song, jacketDesigner) =>
        //{
        //    return jacketDesigner.IsNullOrWhiteSpace() ? true : (song.Jacket_designer.IsNullOrWhiteSpace() ? false : song.Jacket_designer.ToLower().Contains(jacketDesigner.ToLower()));

        //};
        ///// <summary>
        ///// 曲师过滤器
        ///// </summary>
        //readonly Func<ArcaeaSongModel, string, bool> SongArtistFilter = (song, artist) =>
        //{
        //    return artist.IsNullOrWhiteSpace() ? true : (song.Song_artist.IsNullOrWhiteSpace() ? false : song.Song_artist.ToLower().Contains(artist.ToLower()));

        //};
        ///// <summary>
        ///// 谱师过滤器
        ///// </summary>
        //readonly Func<ArcaeaSongModel, string, bool> ChartDesignerFilter = (song, designer) =>
        //{
        //    return designer.IsNullOrWhiteSpace() ? true : (song.Chart_designer.IsNullOrWhiteSpace() ? false : song.Chart_designer.ToLower().Contains(designer.ToLower()));
        //};

        ///// <summary>
        ///// 谱面定数过滤器
        ///// </summary>
        //readonly Func<ArcaeaSongModel, string, bool> ChartConstFilter = (song, chartConst) =>
        //{
        //    if (chartConst.IsNullOrWhiteSpace()) return true;
        //    if (song.Chart_constant == default) return false;

        //    if (double.TryParse(chartConst, out double num))//直接等于
        //    {
        //        return song.Chart_constant == num;
        //    }
        //    else if (chartConst.GetInterval(out IntervalDouble left, out IntervalDouble right))//区间表示
        //    {
        //        if (song.Chart_constant >= left && song.Chart_constant <= right) return true;
        //    }
        //    return false;
        //};
        ///// <summary>
        ///// 谱面难度过滤器
        ///// </summary>
        //readonly Func<ArcaeaSongModel, List<string>, bool> RatingNumFilter = (song, ratingNumList) =>
        //{
        //    return (ratingNumList.IsEmpty()) ? true : ratingNumList.Contains(song.Chart_rating);
        //};
        ///// <summary>
        ///// 谱面难度类型过滤器
        ///// </summary>
        //readonly Func<ArcaeaSongModel, ArcaeaSongModel.RatingClass, bool> RatingClassFilter = (song, ratingClass) =>
        //{
        //    return (ratingClass == ArcaeaSongModel.RatingClass.random) ? true : song.Chart_rating_class.Equals(ratingClass.ToString(), StringComparison.OrdinalIgnoreCase);
        //};


        #endregion

    }
}

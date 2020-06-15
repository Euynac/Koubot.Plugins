using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xyz.Koubot.AI.SDK.General;
using Xyz.Koubot.AI.SDK.Interface;
using Xyz.Koubot.AI.SDK.Tool;

namespace KouGamePlugin.Arcaea.Models
{
    /// <summary>
    /// Arcaea的歌曲Model
    /// </summary>
    public class ArcaeaSongModel : IModelFormattable<ArcaeaSongModel>, ILearnableModel
    {
        /// <summary>
        /// 歌曲阵营
        /// </summary>
        public enum Side
        {
            Light,
            Conflict
        }
        /// <summary>
        /// 歌曲难度类型
        /// </summary>
        public enum RatingClass
        {
            Past,
            Present,
            Future,
            Beyond,
            Random
        }
        /// <summary>
        /// 难度别名
        /// </summary>
        public static Dictionary<string, RatingClass> RatingClassNameList = new Dictionary<string, RatingClass>()
        {
            { "pst", RatingClass.Past},
            { "prs", RatingClass.Present},
            { "ftr", RatingClass.Future},
            { "byd", RatingClass.Beyond },
            { "byn", RatingClass.Beyond },
            { "all", RatingClass.Random }
        };
        /// <summary>
        /// ArcaeaSong数据库表名
        /// </summary>
        public static string ARCAEA_SONG = "plugin_arcaea_song";

        /// <summary>
        /// KouArcaea歌曲ID
        /// </summary>
        public int Song_id { get; set; }
        /// <summary>
        /// 歌曲英文ID（参见songlist）
        /// </summary>
        public string Song_en_id { get; set; }
        /// <summary>
        /// 歌曲标题
        /// </summary>
        public string Song_title { get; set; }
        /// <summary>
        /// 歌曲作者
        /// </summary>
        public string Song_artist { get; set; }
        /// <summary>
        /// 歌曲BPM
        /// </summary>
        public string Song_bpm { get; set; }
        /// <summary>
        /// 歌曲基本BPM
        /// </summary>
        public double Song_bpm_base { get; set; }
        /// <summary>
        /// 歌曲曲包
        /// </summary>
        public string Song_pack { get; set; }
        /// <summary>
        /// 歌曲背景图片
        /// </summary>
        public string Song_bg_url { get; set; }
        /// <summary>
        /// 歌曲长度
        /// </summary>
        public TimeSpan Song_length { get; set; }
        /// <summary>
        /// Arcaea歌曲阵营
        /// </summary>
        public Side Song_side { get; set; }



        /// <summary>
        /// 谱面notes总数
        /// </summary>
        public int Chart_all_notes { get; set; }
        /// <summary>
        /// 谱面地键数
        /// </summary>
        public int Chart_floor_notes { get; set; }
        /// <summary>
        /// 谱面天键数
        /// </summary>
        public int Chart_sky_notes { get; set; }
        /// <summary>
        /// 谱面hold键数
        /// </summary>
        public int Chart_hold_notes { get; set; }
        /// <summary>
        /// 谱面arc键数
        /// </summary>
        public int Chart_arc_notes { get; set; }
        /// <summary>
        /// 谱面note密度（note/每秒）
        /// </summary>
        public double Notes_per_second { get; set; }
        /// <summary>
        /// 谱面难度类型（Future、Present、Past）
        /// </summary>
        public RatingClass Chart_rating_class { get; set; }
        /// <summary>
        /// 谱面难度数（如10+，10）
        /// </summary>
        public string Chart_rating { get; set; }
        /// <summary>
        /// 谱面定数
        /// </summary>
        public double Chart_constant { get; set; }
        /// <summary>
        /// 谱面作者
        /// </summary>
        public string Chart_designer { get; set; }
        
        /// <summary>
        /// 多指使用？
        /// </summary>
        public bool Plus_fingers { get; set; }
        /// <summary>
        /// 插画（歌曲封面）设计者
        /// </summary>
        public string Jacket_designer { get; set; }
        /// <summary>
        /// 封面图
        /// </summary>
        public string Jacket_url { get; set; }
        /// <summary>
        /// 封面重绘？
        /// </summary>
        public bool Jacket_override { get; set; }
        /// <summary>
        /// 隐藏直到解锁
        /// </summary>
        public bool Hidden_until_unlocked { get; set; }

        /// <summary>
        /// 解锁的地图（若存在）
        /// </summary>
        public string Unlock_in_world_mode { get; set; }
        /// <summary>
        /// 出现版本
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// 歌曲备注
        /// </summary>
        public string Remark { get; set; }
        /// <summary>
        /// 发行日期
        /// </summary>
        public long Date { get; set; }

        #region 筛选过滤器
        /// <summary>
        /// 谱面难度过滤器
        /// </summary>
        public static readonly Func<ArcaeaSongModel, List<string>, bool> RatingNumFilter = (song, ratingNumList) =>
        {
            return ratingNumList.IsEmpty() || ratingNumList.Contains(song.Chart_rating);
        };

        public string ToString(DetailLevel detailLevel)
        {
            return ToString(detailLevel.ToString(), null);
        }


        public string ToString(Func<ArcaeaSongModel, string> func)
        {
            return func.Invoke(this);
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrWhiteSpace(format)) format = nameof(DetailLevel.Brief);
            switch (format)
            {
                case nameof(DetailLevel.Brief):
                    return $"{Song_title} [{Chart_rating_class} {Chart_rating}({Chart_constant})]";
 
                case nameof(DetailLevel.Detail):
                    //格式化信息
                    return $"{Song_title} [{Chart_rating_class} {Chart_constant}]\n" +
                        Song_artist.IsNullOrBe($"曲师：{Song_artist}\n") +
                        Jacket_designer.IsNullOrBe($"画师：{Jacket_designer}\n") +
                        Song_bpm.IsNullOrBe($"BPM：{Song_bpm}\n") +
                        Song_length.IsDefaultOrBe($"歌曲长度：{ Song_length}\n") +
                        Song_pack.IsNullOrBe($"曲包：{Song_pack}\n") +
                        Chart_designer.IsNullOrBe($"谱师：{Chart_designer}\n") +
                        Chart_all_notes.IsDefaultOrBe($"note总数：{Chart_all_notes}\n地键：{Chart_floor_notes}\n天键：{Chart_sky_notes}\n蛇：{Chart_arc_notes}\n长条：{Chart_hold_notes}");
                default:
                    break;
            }
            return null;
        }

        public string GetTableName()
        {
            return ARCAEA_SONG;
        }
        #endregion
    }
}

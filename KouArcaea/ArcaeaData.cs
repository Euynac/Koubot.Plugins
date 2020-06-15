using KouGamePlugin.Arcaea.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xyz.Koubot.AI.SDK.General;
using Xyz.Koubot.AI.SDK.General.Mysql;

namespace KouGamePlugin.Arcaea
{
    /// <summary>
    /// Arcaea的数据模块
    /// </summary>
    public class ArcaeaData
    {
        /// <summary>
        /// Arcaea歌曲列表
        /// </summary>
        public static List<ArcaeaSongModel> ArcaeaSongList { get; set; }
        /// <summary>
        /// Arcaea歌曲别名列表[别名，曲英文id]
        /// </summary>
        public static Dictionary<string, string> ArcaeaSongAnotherNameDict { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 多线程锁
        /// </summary>
        private static string securityLock = "";

        /// <summary>
        /// 从数据库加载Arcaea所有信息
        /// </summary>
        public static void LoadDataFromSql()
        {
            MysqlDataService mysqlDataService = new MysqlDataService();
            ArcaeaSongList = mysqlDataService.FetchModelListFromSql<ArcaeaSongModel>($"select * from {ArcaeaSongModel.ARCAEA_SONG}");
            var nameList = mysqlDataService.FetchModelListFromSql<ArcaeaSongAnotherNameModel>($"select * from {ArcaeaSongAnotherNameModel.ARCAEA_SONG_ANOTHER_NAME}");
            if (nameList != null)
            {
                foreach (var name in nameList)
                {
                    ArcaeaSongAnotherNameDict.TryAdd(name.Another_name.ToLower(), name.Song_en_id);
                }
            }

        }

        static ArcaeaData()
        {
            LoadDataFromSql();
        }
        #region 歌曲别名
        /// <summary>
        /// 获取指定歌曲所有歌曲别名
        /// </summary>
        /// <param name="songEnID"></param>
        /// <returns></returns>
        public static List<string> GetSongAllAnotherName(string songEnID)
        {
            if (ArcaeaSongAnotherNameDict.ContainsValue(songEnID))
            {
                if (ArcaeaSongAnotherNameDict.TryGetAllKey(songEnID, out List<string> anotherNameList))
                {
                    return anotherNameList;
                }
            }
            return null;
        }
        /// <summary>
        /// 线程安全型增加，防止出错
        /// </summary>
        /// <param name="songAnotherName"></param>
        /// <param name="songEnID"></param>
        public static void AddAnotherName(string songAnotherName, string songEnID)
        {
            lock (securityLock)
            {
                ArcaeaSongAnotherNameDict.TryAdd(songAnotherName, songEnID);
            }
        }
        /// <summary>
        /// 线程安全型删除
        /// </summary>
        /// <param name="anotherName"></param>
        public static void DeleteAnotherName(string anotherName)
        {
            lock (securityLock)
            {
                ArcaeaSongAnotherNameDict.Remove(anotherName);
            }
        }

        #endregion

        /// <summary>
        /// 根据分数和谱面定数计算ptt
        /// </summary>
        /// <param name="constant"></param>
        /// <param name="score"></param>
        /// <returns></returns>
        public static double CalSongScorePtt(double constant, int score)
        {
            if (score >= 10000000) return constant + 2;
            else if (score > 9800000) return constant + 1 + (score - 9800000) / 200000.0;
            double value = constant + (score - 9500000) / 300000.0;
            return value < 0 ? 0 : value;
        }
    }
}

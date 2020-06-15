using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xyz.Koubot.AI.SDK.Interface;

namespace KouGamePlugin.Arcaea.Models
{
    /// <summary>
    /// Arcaea 歌曲别名Model
    /// </summary>
    public class ArcaeaSongAnotherNameModel : ILearnableModel
    {
        public static string ARCAEA_SONG_ANOTHER_NAME = "plugin_arcaea_song_another_name";
        /// <summary>
        /// 歌曲别名ID
        /// </summary>
        public int Another_name_id { get; set; }
        /// <summary>
        /// 歌曲别名
        /// </summary>
        public string Another_name { get; set; }
        /// <summary>
        /// 对应歌曲英文id
        /// </summary>
        public string Song_en_id { get; set; }

        public string GetTableName()
        {
            return ARCAEA_SONG_ANOTHER_NAME;
        }
    }
}

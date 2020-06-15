using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KouFunctionPlugin.Romaji.Models
{
    /// <summary>
    /// Romaji键值对
    /// </summary>
    public class RomajiModel
    {
        public static string ROMAJI_PAIR = "plugin_romaji_pair";
        public int Id { get; set; }
        public string Romaji_key { get; set; }
        public string Zh_value { get; set; }
    }
}

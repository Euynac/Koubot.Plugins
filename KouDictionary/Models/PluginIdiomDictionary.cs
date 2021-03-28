using Koubot.SDK.Protocol.AutoModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace KouFunctionPlugin.Models
{
    [Table("plugin_idiom_dict")]
    [KouAutoModelTable("idiom", new[] { nameof(KouDictionary) }, Name = "成语词典")]
    public partial class PluginIdiomDictionary
    {

        [Key]
        [Column("id")]
        [StringLength(255)]
        [KouAutoModelField(ActivateKeyword = "id")]
        public int Id { get; set; }
        [Key]
        [Column("word")]
        [StringLength(255)]
        [KouAutoModelField(ActivateKeyword = "word", FilterSetting = FilterType.Fuzzy)]
        public string Word { get; set; }

        [Column("derivation")]
        [StringLength(255)]
        [KouAutoModelField(ActivateKeyword = "来源", FilterSetting = FilterType.Fuzzy)]
        public string Derivation { get; set; }
        [Column("example")]
        [StringLength(255)]
        public string Example { get; set; }
        [Column("explanation")]
        [StringLength(255)]
        [KouAutoModelField(ActivateKeyword = "解释", FilterSetting = FilterType.Fuzzy)]
        public string Explanation { get; set; }
        [Column("pinyin")]
        [StringLength(255)]
        [KouAutoModelField(ActivateKeyword = "拼音")]
        public string Pinyin { get; set; }
        [Column("abbreviation")]
        [StringLength(255)]
        [KouAutoModelField(ActivateKeyword = "缩写|abbr")]
        public string Abbreviation { get; set; }
    }
}

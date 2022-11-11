using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.Enums;


namespace KouFunctionPlugin.Models
{
    [Table("plugin_idiom_dict")]
    [AutoTable("idiom", new[] { nameof(KouDictionary) }, Name = "成语词典")]
    public partial class IdiomDictionary
    {

        [Key]
        [Column("id")]
        [StringLength(255)]
        [AutoField(ActivateKeyword = "id")]
        public int Id { get; set; }
        [Key]
        [Column("word")]
        [StringLength(255)]
        [AutoField(
            ActivateKeyword = "word",
            FilterSetting = FilterType.Fuzzy,
            Features = AutoModelFieldFeatures.IsDefaultField)]
        public string Word { get; set; }

        [Column("derivation")]
        [StringLength(255)]
        [AutoField(ActivateKeyword = "来源", FilterSetting = FilterType.Fuzzy)]
        public string Derivation { get; set; }
        [Column("example")]
        [StringLength(255)]
        public string Example { get; set; }
        [Column("explanation")]
        [StringLength(255)]
        [AutoField(ActivateKeyword = "解释", FilterSetting = FilterType.Fuzzy)]
        public string Explanation { get; set; }
        [Column("pinyin")]
        [StringLength(255)]
        [AutoField(ActivateKeyword = "拼音")]
        public string Pinyin { get; set; }
        [Column("abbreviation")]
        [StringLength(255)]
        [AutoField(ActivateKeyword = "缩写|abbr")]
        public string Abbreviation { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Koubot.Shared.Protocol.AutoModel;


namespace KouFunctionPlugin.Models
{
    [Table("plugin_en_dict")]
    [KouAutoModelTable("en", new[] { nameof(KouDictionary) }, Name = "英文词典")]
    public partial class EnDictionary
    {
        [Key]
        [Column("word")]
        [StringLength(25)]
        [KouAutoModelField(
            ActivateKeyword = "word",
            FilterSetting = FilterType.IgnoreCase |
                            FilterType.DisableLike,
            Features = AutoModelFieldFeatures.IsDefaultField)]
        public string Word { get; set; }

        [Column("uk_pron")]
        [StringLength(100)]
        public string UkPron { get; set; }
        [Column("us_pron")]
        [StringLength(100)]
        public string UsPron { get; set; }
        [Column("population")]
        [StringLength(20)]
        [KouAutoModelField(ActivateKeyword = "词频")]
        public int Population { get; set; }

        [Column("definition")]
        [StringLength(200)]
        [KouAutoModelField(ActivateKeyword = "解释|define")]
        public string Definition { get; set; }
    }
}

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.Enums;


namespace KouGamePlugin.Maimai.Models
{
    [Table("plugin_maimai_map")]
    [KouAutoModelTable("map",
        new[] { nameof(KouMaimai) },
        Name = "DX地图", Help = "更新日期：2021/6/12 16:00")]
    public partial class ArcadeMap
    {
        [Key]
        [Column("id")]
        [KouAutoModelField(ActivateKeyword = "id", UnsupportedActions = AutoModelActions.CannotAlter)]
        public int LocationId { get; set; }
        [Column("machineCount")]
        [KouAutoModelField(ActivateKeyword = "c|数量")]
        public int MachineCount { get; set; }

        [Column("province")]
        [StringLength(20)]
        [KouAutoModelField(ActivateKeyword = "省份|p")]
        public string Province { get; set; }
        [Column("arcadeName")]
        [StringLength(200)]
        [KouAutoModelField(ActivateKeyword = "店名|name")]
        public string ArcadeName { get; set; }
        [Column("mall")]
        [KouAutoModelField(ActivateKeyword = "商城名")]
        [StringLength(200)]
        public string MallName { get; set; }

        [Column("address")]
        [KouAutoModelField(ActivateKeyword = "地址", Features = AutoModelFieldFeatures.IsDefaultField)]
        public string Address { get; set; }
    }
}

using Koubot.SDK.Interface;
using Koubot.SDK.Protocol.AutoModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Koubot.SDK.Models.System;

// ReSharper disable once CheckNamespace
namespace KouFunctionPlugin.Romaji.Models
{
    [KouAutoModelTable("list", new[] { nameof(KouRomajiHelper) }, Name = "罗马音-中文谐音表")]
    [Table("plugin_romaji_pair")]
    public partial class RomajiPair : KouFullAutoModel<RomajiPair>
    {
        [Column("id")]
        [KouAutoModelField(IsPrimaryKey = true,
            UnsupportedActions = AutoModelActions.Add | AutoModelActions.Modify)]
        public int Id { get; set; }
        [Column("romaji_key")]
        [StringLength(20)]
        [KouAutoModelField(ActivateKeyword = "romaji|罗马音", Name = "罗马音名",
            Features = AutoModelFieldFeatures.RequiredAdd, CandidateKey = MultiCandidateKey.FirstCandidateKey)]
        public string RomajiKey { get; set; }
        [Column("zh_value")]
        [StringLength(20)]
        [KouAutoModelField(ActivateKeyword = "zh|中文", Name = "中文谐音",
            Features = AutoModelFieldFeatures.RequiredAdd)]
        public string ZhValue { get; set; }

        public override string ToString(FormatType formatType, object supplement = null, KouCommand command = null)
        {
            return $"{Id}.{RomajiKey} - {ZhValue}";
        }


        public override Action<EntityTypeBuilder<RomajiPair>> ModelSetup()
        {
            return entity =>
            {
                entity.HasIndex(e => e.RomajiKey)
                    .IsUnique();
            };
        }
    }
}

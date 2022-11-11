using System;
using System.Collections.Generic;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Koubot.Shared.Models;


namespace KouGamePlugin.Maimai.Models
{
    [Table("plugin_maimai_map")]
    [AutoTable("map",
        new[] { nameof(KouMaimai) },
        Name = "DX地图")]
    public partial class ArcadeMap
    {
        [Key]
        [Column("id")]
        [AutoField(ActivateKeyword = "id", UnsupportedActions = AutoModelActions.CannotAlter)]
        public int LocationId { get; set; }
        [Column("machineCount")]
        [AutoField(ActivateKeyword = "数量")]
        public int MachineCount { get; set; }

        [Column("province")]
        [StringLength(20)]
        [AutoField(ActivateKeyword = "省份")]
        public string Province { get; set; }
        [Column("arcadeName")]
        [StringLength(200)]
        [AutoField(ActivateKeyword = "店名")]
        public string ArcadeName { get; set; }
        [Column("mall")]
        [AutoField(ActivateKeyword = "商城名")]
        [StringLength(200)]
        public string MallName { get; set; }
        [Column("address")]
        [AutoField(ActivateKeyword = "地址", Features = AutoModelFieldFeatures.IsDefaultField)]
        public string Address { get; set; }
        [AutoField(ActivateKeyword = "别名")]
        public List<string>? Aliases { get; set; }
        /// <summary>
        /// 已被关闭（指已不在官方列表中出现）
        /// </summary>
        public bool IsClosed { get; set; }
        [Column("PeopleCount")]
        public List<CardRecord>? PeopleCount { get; set; }
        [AutoField(ActivateKeyword = "说明")]
        public string? Remark { get; set; }
        [AutoField(ActivateKeyword = "币价")]
        public double? Fee { get; set; }
        [AutoField(ActivateKeyword = "交通")]
        public string? Route { get; set; }
        public List<string>? Photos { get; set; }
        public class CardRecord
        {
            private UserAccount _user;
            public DateTime ModifyAt { get; set; }
            public int UserID { get; set; }
            public int AlterCount { get; set; }

            [JsonIgnore]
            public UserAccount User
            {
                get
                {
                    if (_user == null)
                    {
                        _user = UserAccount.SingleOrDefault(p => p.Id == UserID);
                    }

                    return _user;
                }
                set => _user = value;
            }

            public CardRecord()
            {
                
            }
            public CardRecord(UserAccount user, int alterCount)
            {
                UserID = user.Id;
                User = user;
                AlterCount = alterCount;
                ModifyAt = DateTime.Now;
            }

            public override string ToString()
            {
                
                var action = AlterCount >= 0 ? "加" : "减";
                return $"{ModifyAt:T} {User?.Nickname} {action}了{AlterCount}卡";
            }
        }
    }
}

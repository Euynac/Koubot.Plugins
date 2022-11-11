using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Koubot.Shared.Models;
using Koubot.Tool.Extensions;

namespace KouFunctionPlugin;

public class LotteryConfig : PluginGlobalConfig
{
    public class PoolRankItem
    {
        public int BonusUserID { get; set; }
        [JsonIgnore] public UserAccount? BonusUser => UserAccount.SingleOrDefault(p => p.Id == BonusUserID);
        public int BonusCoin { get; set; }
        public DateTime BonusTime { get; set; }

        public override string ToString()
        {
            return $"{BonusUser?.Nickname ?? "???"}\t{BonusCoin}枚\t{BonusTime}";
        }
    }

    public int PoolTotalCoins { get; set; }
    public DateTime LastBonusTime { get; set; }
    public int LastBonusCoins { get; set; }
    public int? LastBonusUserID { get; set; }
    public int SuccessPeopleCount { get; set; }
    [JsonIgnore]
    public UserAccount? LastBonusUser => UserAccount.SingleOrDefault(p => p.Id == LastBonusUserID);
    public List<PoolRankItem> BonusRecords { get; set; }
    public string GetStatus(KouGlobalConfig config)
    {
        return
            $"当前奖池：{config.CoinFormat(PoolTotalCoins)}\n" +
            $"上次{LastBonusUser?.Nickname}在{LastBonusTime}搬空了池子，拿到了{config.CoinFormat(LastBonusCoins)}，是第{SuccessPeopleCount}个搬空池子的人";
    }
    public string GetCoinPoolInfo(KouGlobalConfig config)
    {
        return $"当前奖池：{config.CoinFormat(PoolTotalCoins)}";
    }

    public string? BonusStatus()
    {
        if (BonusRecords.IsNullOrEmptySet()) return null;
        return BonusRecords.OrderByDescending(p=>p.BonusCoin).Select((p, i) => $"#{i+1}.{p}").StringJoin('\n');
    }

    public int? RecordBonus(UserAccount user, int bonus)
    {
        BonusRecords ??= new List<PoolRankItem>();
        if(BonusRecords.Count >= 10 && BonusRecords.Min(p=>p.BonusCoin) > bonus) return null;
        var rank = BonusRecords.Count(p=>p.BonusCoin > bonus);
        BonusRecords.Add(new PoolRankItem()
        {
            BonusCoin = bonus,
            BonusTime = DateTime.Now,
            BonusUserID = user.Id
        });
        return rank + 1;
    }
}
using System;
using System.Text.Json.Serialization;
using Koubot.Shared.Models;

namespace KouFunctionPlugin;

public class LotteryConfig : PluginGlobalConfig
{
    public int PoolTotalCoins { get; set; }
    public DateTime LastBonusTime { get; set; }
    public int LastBonusCoins { get; set; }
    public int? LastBonusUserID { get; set; }
    public int SuccessPeopleCount { get; set; }
    [JsonIgnore]
    public UserAccount? LastBonusUser => UserAccount.SingleOrDefault(p => p.Id == LastBonusUserID);

    public string GetCoinPoolInfo(KouGlobalConfig config)
    {
        return $"当前奖池：{config.CoinFormat(PoolTotalCoins)}";
    }
}
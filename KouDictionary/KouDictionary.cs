using System;
using Koubot.SDK.PluginExtension;
using Koubot.SDK.PluginInterface;
using Koubot.Shared.Protocol;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.KouEnum;
using KouDictionary;

namespace KouFunctionPlugin
{
    [PluginClass("dict", "词典",
        Introduction = "词典",
        PluginType = PluginType.Function)]
    public class KouDictionary : KouPlugin<KouDictionary>
    {

        static KouDictionary()
        {
            PluginEventList.FetchGroupGameInfo += (sender) =>
            {
                if (sender.CurGroup?.HasInstallPlugin(GetPluginMetadataStatic().Info) is true)
                {
                    var info = GetPluginMetadataStatic().Info;
                    var func = info.GetFunctionInfo(nameof(EnglishWordSolitaire));
                    return new PluginEventList.GameInfo()
                    {
                        GameCommand = $"{KouCommand.GetPluginRoute(sender.CurKouGlobalConfig, info, nameof(EnglishWordSolitaire))} --help",
                        Introduce = func?.FunctionHelp ?? "??",
                        GameName = func?.FunctionName ?? "??",
                    };
                }

                return null;
            };
            PluginEventList.FetchGroupGameInfo += (sender) =>
            {
                if (sender.CurGroup?.HasInstallPlugin(GetPluginMetadataStatic().Info) is true)
                {
                    var info = GetPluginMetadataStatic().Info;
                    var func = info.GetFunctionInfo(nameof(ChineseIdiomSolitaire));
                    return new PluginEventList.GameInfo()
                    {
                        GameCommand = $"{KouCommand.GetPluginRoute(sender.CurKouGlobalConfig, info, nameof(ChineseIdiomSolitaire))} --help",
                        Introduce = func?.FunctionHelp ?? "??",
                        GameName = func?.FunctionName ?? "??",
                    };
                }

                return null;
            };
            PluginEventList.FetchGroupGameInfo += (sender) =>
            {
                if (sender.CurGroup?.HasInstallPlugin(GetPluginMetadataStatic().Info) is true)
                {
                    var info = GetPluginMetadataStatic().Info;
                    var func = info.GetFunctionInfo(nameof(TypeContest));
                    return new PluginEventList.GameInfo()
                    {
                        GameCommand = $"{KouCommand.GetPluginRoute(sender.CurKouGlobalConfig, info, nameof(TypeContest))} --help",
                        Introduce = func?.FunctionHelp ?? "??",
                        GameName = func?.FunctionName ?? "??",
                    };
                }

                return null;
            };
        }
        [PluginFunction(Help = "现有英文字典(en)、成语字典(idiom)，使用/dict.en help查看英文表详情")]
        public override object? Default(string? str = null)
        {
            return ReturnHelp(true);
        }
        [PluginFunction(Name = "手速竞技", ActivateKeyword = "type", 
            Help = SpeedTyperGameRoom.Help
            , OnlyUsefulInGroup = true, NeedCoin = 10, CanEarnCoin = true)]
        public object? TypeContest([PluginArgument(Name = "入场费(最低10)", Min = 10)] int? fee = null)
        {
            fee ??= 10;
            if (!CurKouUser.ConsumeCoinFree(fee.Value)) return FormatNotEnoughCoin(fee.Value, CurUserName);
            var room = new SpeedTyperGameRoom("手速竞技", CurUser, CurGroup, fee)
            {
                LastTime = CurCommand.CustomTimeSpan ?? new TimeSpan(0,5,0)
            };
            ConnectRoom(
                $"{CurUserName}消耗{CurKouGlobalConfig.CoinFormat(fee.Value)}创建了游戏房间：手速竞技，后续收到的入场费({CurKouGlobalConfig.CoinFormat(fee.Value)})将累计在奖池中，按排名发放奖励",
                room);
            return null;
        }
        [PluginFunction(Name = "单词接龙", ActivateKeyword = "单词接龙", 
            Help = EnglishWordSolitaireRoom.Help, OnlyUsefulInGroup = true, NeedCoin = 10, CanEarnCoin = true)]
        public object? EnglishWordSolitaire([PluginArgument(Name = "入场费(最低10)", Min = 10)] int? fee = null)
        {
            fee ??= 10;
            if (!CurKouUser.ConsumeCoinFree(fee.Value)) return FormatNotEnoughCoin(fee.Value, CurUserName);
            var room = new EnglishWordSolitaireRoom("单词接龙", CurUser, CurGroup, fee)
            {
                LastTime = CurCommand.CustomTimeSpan ?? new TimeSpan(0,10,0)
            };
            ConnectRoom(
                $"{CurUserName}消耗{CurKouGlobalConfig.CoinFormat(fee.Value)}创建了游戏房间：单词接龙，后续收到的入场费({CurKouGlobalConfig.CoinFormat(fee.Value)})将累计在奖池中，按排名发放奖励",
                room);
            return null;
        }
        [PluginFunction(Name = "成语接龙", ActivateKeyword = "成语接龙", 
            Help = ChineseIdiomSolitaireRoom.Help, OnlyUsefulInGroup = true, NeedCoin = 10, CanEarnCoin = true)]
        public object? ChineseIdiomSolitaire([PluginArgument(Name = "入场费(最低10)", Min = 10)] int? fee = null)
        {
            fee ??= 10;
            if (!CurKouUser.ConsumeCoinFree(fee.Value)) return FormatNotEnoughCoin(fee.Value, CurUserName);
            var room = new ChineseIdiomSolitaireRoom("成语接龙", CurUser, CurGroup, fee)
            {
                LastTime = CurCommand.CustomTimeSpan ?? new TimeSpan(0,10,0)
            };
            ConnectRoom(
                $"{CurUserName}消耗{CurKouGlobalConfig.CoinFormat(fee.Value)}创建了游戏房间：成语接龙，后续收到的入场费({CurKouGlobalConfig.CoinFormat(fee.Value)})将累计在奖池中，按排名发放奖励",
                room);
            return null;
        }
    }
}

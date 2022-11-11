using Koubot.SDK.API;
using Koubot.SDK.PluginInterface;
using Koubot.SDK.System;
using Koubot.SDK.Tool;
using Koubot.Shared.Interface;
using Koubot.Shared.Models;
using Koubot.Shared.Protocol;
using Koubot.Shared.Protocol.Event;
using Koubot.Tool.Extensions;
using Koubot.Tool.Random;
using System;
using System.Threading;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.KouEnum;

namespace KouFunctionPlugin
{
    /// <summary>
    /// Koubot Coin Plugin
    /// </summary>
    [PluginClass("hb", "红包",
        Introduction = "发各种红包",
        Author = "7zou",
        PluginType = PluginType.System)]
    public class KouRedPacketPlugin : KouPlugin<KouRedPacketPlugin>, IWantTargetGroup
    {
        #region 红包相关
        [PluginParameter(
            Name = "竞速",
            ActivateKeyword = "v",
            Help = "指示红包是竞速红包，会在10-30秒内随机发送红包，并显示抢红包的速度，如果是拼手气则还会变成越快越有利")]
        public bool IsCompeteInVelocity { get; set; }

        [PluginParameter(
            Help = "指示红包是均分发送，注意此时总金额变为每个红包金额",
            ActivateKeyword = "same",
            Name = "红包均分")]
        public bool IsIdentical { get; set; }
        [PluginParameter(
            Help = "红包有效期，默认五分钟。有效设定时间：（30s-30min）",
            ActivateKeyword = "t",
            Name = "设定有效时间")]
        public TimeSpan? Duration { get; set; }
        [PluginParameter(
            Help = "红包备注，收到红包的都会得到这个备注",
            ActivateKeyword = "remark",
            Name = "设定红包备注")]
        public string Remark { get; set; }
        [PluginEventHandler]
        public override KouEventHandlerResult OnReceiveGroupMessage(GroupMessageEventArgs e)
        {
            var red = KouRedPacket.TryGetGroupRedPacket(e.FromGroup, e.GroupMessage.Content);
            if (red == null) return null;
            if (red.Open(e.FromUser, out int coinsGot))
            {
                string speedAppend =
                    red.IsCompeteInVelocity ? $"（耗时{(DateTime.Now - red.StartTime).TotalSeconds:0.##}秒）" : null;
                speedAppend += red.Remark?.Be($"\n红包留言：{red.Remark}");
                return $"{e.FromUser.Name}打开{(red.FromUser.KouUser == e.FromUser.KouUser ? "自己" : red.FromUser.Name)}的红包获得了{e.GroupMessage.GetBotSetting().CoinFormat(coinsGot)}！{speedAppend}";
            }

            return null;
        }

        [PluginFunction(ActivateKeyword = "op|open|o", Name = "打开口令红包")]
        public object OpenPasswordRedPacket(string password)
        {
            var red = KouRedPacket.TryGetGlobalRedPacket(password);
            if (red == null) return "找不到对应的红包诶...";
            string speedAppend =
                red.IsCompeteInVelocity ? $"（耗时{(DateTime.Now - red.StartTime).TotalSeconds:0.##}秒）" : null;
            speedAppend += red.Remark?.Be($"\n红包留言：{red.Remark}");
            if (red.TestIfHaveOpened(CurUser)) return $"{CurUser.Name}已经抢过该红包啦";
            if (!red.Open(CurUser, out var coinsGot)) return "可惜，没有抢到这个红包呢";
            return $"{CurUser.Name}打开{(red.FromUser.KouUser == CurUser.KouUser ? "自己" : red.FromUser.Name)}的红包获得了{CurKouGlobalConfig.CoinFormat(coinsGot)}！{speedAppend}";
        }

        private void RedPacketEndAction(KouRedPacket r)
        {
            string reply;
            int exp = r.TotalCoins - r.RemainCoins - r.FromUserGet;
            r.FromUser.KouUser.GainExp(exp);
            if (r.RemainCount == 0)
            {
                reply = $"{CurUser.Name}的红包抢完啦！\n[{CurUser.Name}获得了{exp}点经验]";
            }
            else
            {
                reply = $"{CurUser.Name}的口令红包\"{r.Password}\"到期，" +
                        $"共{r.TotalCount - r.RemainCount}人领取，" +
                        $"剩余{CurKouGlobalConfig.CoinFormat(r.RemainCoins)}\n" +
                        $"[获得了{exp}点经验]";
            }

            CurUser.SendMessage(TargetGroup ?? CurGroup, reply);
        }


        [PluginFunction(ActivateKeyword = "group|g", Name = "发群组口令红包", Help = "接下来该群组内回复包含该口令的语句即会领取红包",
            SupportedParameters = new[]{nameof(IsIdentical)
                , nameof(IsCompeteInVelocity),
                nameof(Duration), nameof(Remark)},
            OnlyUsefulInGroup = true)]
        public object SendGroupPasswordRedPacket(
            [PluginArgument(Name = "总金额", Min = 1, EnableDefaultRangeError = true)] int total,
            [PluginArgument(Name = "数量", Min = 1, EnableDefaultRangeError = true)] int quantity,
            [PluginArgument(Name = "口令(默认生成6位数字)")] string password = null)
        {
            if (password == "") password = null;
            if (!ValidateAvailableTime()) return ConveyMessage;
            KouRedPacket redPacket = new KouRedPacket(CurUser, total, quantity, IsIdentical, password, Duration)
            {
                EndAction = RedPacketEndAction,
                IsCompeteInVelocity = IsCompeteInVelocity,
                Remark = Remark
            };

            if (redPacket.TotalCoins / redPacket.TotalCount == 0) return $"每人至少需要有{CurKouGlobalConfig.CoinFormat(1)}";
            var rest = CurKouUser.CoinFree - redPacket.TotalCoins;
            if (rest >= 0)
            {
                string ask =
                    $"发送总额为{CurKouGlobalConfig.CoinFormat(redPacket.TotalCoins)}的{quantity}个群组{IsCompeteInVelocity.BeIfTrue("竞速")}红包{password?.Be($"(口令为\"{password}\")")}" +
                    $"(成功后余额为{CurKouGlobalConfig.CoinFormat(rest)})\n" +
                    $"输入\"y\"确认";
                if (!SessionService.AskConfirm(ask)) return null;
                if (IsCompeteInVelocity)
                {
                    CurUser.SendMessage(CurGroup, "将随机在5~30秒内发送竞速红包");
                    Thread.Sleep(RandomTool.GetInt(5000, 30000));
                }
                if (!redPacket.Sent(TargetGroup ?? CurGroup))
                {
                    return "发红包失败" + redPacket.ErrorMsg?.Be($"，{redPacket.ErrorMsg}");
                }

                return $"发送红包成功！{redPacket.ToString(FormatType.Customize1)}";
            }

            return this.ReturnNullWithError(null, ErrorCodes.Core_BankNotEnoughMoney);
        }
        [PluginFunction]
        public override object? Default(string? str = null)
        {
            return ReturnHelp();
        }



        private bool ValidateAvailableTime()
        {
            if (Duration != null)
            {
                if (Duration.Value > new TimeSpan(0, 30, 0))
                    return ReturnConveyError("当前红包最长有效期不可超过30分钟");
                if (Duration.Value < new TimeSpan(0, 0, 30))
                    return ReturnConveyError("红包有效期最短不能少于30秒");
            }

            return true;
        }

        [PluginFunction(ActivateKeyword = "sent|s", Name = "发口令红包",
            SupportedParameters = new[]{nameof(IsIdentical),
                nameof(IsCompeteInVelocity),
                nameof(Duration), nameof(Remark)})]
        public object SendPasswordRedPacket(
            [PluginArgument(Name = "总金额", Min = 1, EnableDefaultRangeError = true)] int total,
            [PluginArgument(Name = "数量", Min = 1, EnableDefaultRangeError = true)] int quantity,
            [PluginArgument(Name = "口令(默认生成6位数字)")] string password = null)
        {
            if (password == "") password = null;
            if (!ValidateAvailableTime()) return ConveyMessage;
            KouRedPacket redPacket = new KouRedPacket(CurUser, total, quantity, IsIdentical, password, Duration)
            {
                EndAction = RedPacketEndAction,
                IsCompeteInVelocity = IsCompeteInVelocity,
                Remark = Remark
            };

            if (redPacket.TotalCoins / redPacket.TotalCount == 0) return $"每人至少需要有{CurKouGlobalConfig.CoinFormat(1)}";
            var rest = CurKouUser.CoinFree - redPacket.TotalCoins;
            if (rest >= 0)
            {
                string ask =
                    $"发送总额为{CurKouGlobalConfig.CoinFormat(redPacket.TotalCoins)}的{quantity}个{IsCompeteInVelocity.BeIfTrue("竞速")}红包{password?.Be($"(口令为\"{password}\")")}" +
                    $"(成功后余额为{CurKouGlobalConfig.CoinFormat(rest)})\n" +
                    $"输入\"y\"确认";
                if (!SessionService.AskConfirm(ask)) return null;
                if (IsCompeteInVelocity)
                {
                    CurUser.SendMessage(CurGroup, "将随机在5~30秒内发送竞速红包");
                    Thread.Sleep(RandomTool.GetInt(5000, 30000));
                }
                if (!redPacket.Sent())
                {
                    return "发红包失败" + redPacket.ErrorMsg?.Be($"，{redPacket.ErrorMsg}");
                }

                return $"发送红包成功！{redPacket.ToString(FormatType.Customize1)}";
            }

            return this.ReturnNullWithError(null, ErrorCodes.Core_BankNotEnoughMoney);
        }


        #endregion

        public PlatformGroup? TargetGroup { get; set; }
    }
}

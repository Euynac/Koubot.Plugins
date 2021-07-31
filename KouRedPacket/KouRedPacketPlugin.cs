using System;
using System.Threading;
using Koubot.SDK.API;
using Koubot.SDK.Interface;
using Koubot.SDK.Models.Entities;
using Koubot.SDK.Models.System;
using Koubot.SDK.Protocol;
using Koubot.SDK.Protocol.Event;
using Koubot.SDK.Protocol.Plugin;
using Koubot.SDK.Services;
using Koubot.SDK.Services.Interface;
using Koubot.SDK.Tool;
using Koubot.Tool.Extensions;
using Koubot.Tool.Random;

namespace KouFunctionPlugin
{
     /// <summary>
    /// Koubot Coin Plugin
    /// </summary>
    [KouPluginClass("hb", "红包",
        Introduction = "发各种红包",
        Author = "7zou",
        PluginType = KouEnum.PluginType.System)]
    public class KouRedPacketPlugin: KouPlugin<KouRedPacketPlugin>, IWantKouUser, IWantKouPlatformUser, IWantKouGlobalConfig, IWantKouSession, IWantKouPlatformGroup, IWantTargetGroup
    {
        #region 红包相关
        [KouPluginParameter(
            Name = "竞速",
            ActivateKeyword = "v",
            Help = "指示红包是竞速红包，会在10-30秒内随机发送红包，并显示抢红包的速度，如果是拼手气则还会变成越快越有利")]
        public bool IsCompeteInVelocity { get; set; }

        [KouPluginParameter(
            Help = "指示红包是均分发送，注意此时总金额变为每个红包金额", 
            ActivateKeyword = "same", 
            Name = "红包均分")]
        public bool IsIdentical { get; set; }
        [KouPluginParameter(
            Help = "红包有效期，默认五分钟。有效设定时间：（30s-30min）",
            ActivateKeyword = "t", 
            Name = "设定有效时间")]
        public TimeSpan? Duration { get; set; }
        [KouPluginParameter(
            Help = "红包备注，收到红包的都会得到这个备注",
            ActivateKeyword = "remark", 
            Name = "设定红包备注")]
        public string Remark { get; set; }
        [KouPluginEventHandler]
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

        [KouPluginFunction(ActivateKeyword = "op|open|o", Name = "打开口令红包")]
        public object OpenPasswordRedPacket(string password)
        {
            var red = KouRedPacket.TryGetGlobalRedPacket(password);
            if (red == null) return "找不到对应的红包诶...";
            string speedAppend =
                red.IsCompeteInVelocity ? $"（耗时{(DateTime.Now - red.StartTime).TotalSeconds:0.##}秒）" : null;
            speedAppend += red.Remark?.Be($"\n红包留言：{red.Remark}");
            if (red.TestIfHaveOpened(CurrentPlatformUser)) return $"{CurrentPlatformUser.Name}已经抢过该红包啦";
            if (!red.Open(CurrentPlatformUser, out var coinsGot)) return "可惜，没有抢到这个红包呢";
            return $"{CurrentPlatformUser.Name}打开{(red.FromUser.KouUser == CurrentPlatformUser.KouUser ? "自己" : red.FromUser.Name)}的红包获得了{CurrentKouGlobalConfig.CoinFormat(coinsGot)}！{speedAppend}";
        }

        private void RedPacketEndAction(KouRedPacket r)
        {
            string reply;
            int exp = r.TotalCoins - r.RemainCoins - r.FromUserGet;
            r.FromUser.KouUser.GainExp(exp);
            if (r.RemainCount == 0)
            {
                reply = $"{CurrentPlatformUser.Name}的红包抢完啦！\n[{CurrentPlatformUser.Name}获得了{exp}点经验]";
            }
            else
            {
                reply = $"{CurrentPlatformUser.Name}的口令红包\"{r.Password}\"到期，" +
                        $"共{r.TotalCount - r.RemainCount}人领取，" +
                        $"剩余{CurrentKouGlobalConfig.CoinFormat(r.RemainCoins)}\n" +
                        $"[获得了{exp}点经验]";
            }

            CurrentPlatformUser.SendMessage(TargetGroup ?? CurrentPlatformGroup, reply);
        }


        [KouPluginFunction(ActivateKeyword = "group|g", Name = "发群组口令红包", Help = "接下来该群组内回复包含该口令的语句即会领取红包",
            SupportedParameters = new []{nameof(IsIdentical)
                , nameof(IsCompeteInVelocity),
                nameof(Duration), nameof(Remark)},
            OnlyUsefulInGroup = true)]
        public object SendGroupPasswordRedPacket(
            [KouPluginArgument(Name = "总金额", NumberMin = 1, DefaultNumberRangeError = true)]int total,
            [KouPluginArgument(Name = "数量", NumberMin = 1, DefaultNumberRangeError = true)] int quantity,
            [KouPluginArgument(Name = "口令(默认生成6位数字)")] string password = null)
        {
            if (password == "") password = null;
            if (!ValidateAvailableTime()) return ConveyMessage;
            KouRedPacket redPacket = new KouRedPacket(CurrentPlatformUser, total, quantity, IsIdentical, password, Duration)
            {
                EndAction = RedPacketEndAction,
                IsCompeteInVelocity = IsCompeteInVelocity,
                Remark = Remark
            };

            if (redPacket.TotalCoins / redPacket.TotalCount == 0) return $"每人至少需要有{CurrentKouGlobalConfig.CoinFormat(1)}";
            var rest = CurrentUser.CoinFree - redPacket.TotalCoins;
            if (rest >= 0)
            {
                string ask =
                    $"发送总额为{CurrentKouGlobalConfig.CoinFormat(redPacket.TotalCoins)}的{quantity}个群组{IsCompeteInVelocity.BeIfTrue("竞速")}红包{password?.Be($"(口令为\"{password}\")")}" +
                    $"(成功后余额为{CurrentKouGlobalConfig.CoinFormat(rest)})\n" +
                    $"输入\"y\"确认";
                if (!SessionService.AskConfirm(ask)) return null;
                if (IsCompeteInVelocity)
                {
                    CurrentPlatformUser.SendMessage(CurrentPlatformGroup, "将随机在5~30秒内发送竞速红包");
                    Thread.Sleep(RandomTool.GenerateRandomInt(5000, 30000));
                }
                if (!redPacket.Sent(TargetGroup ?? CurrentPlatformGroup))
                {
                    return "发红包失败" + redPacket.ErrorMsg?.Be($"，{redPacket.ErrorMsg}");
                }
                
                return $"发送红包成功！{redPacket.ToString(FormatType.Customize1)}";
            }

            return this.ReturnNullWithError(null, ErrorCodes.Core_BankNotEnoughMoney);
        }
        [KouPluginFunction]
        public override object Default(string str = null)
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

        [KouPluginFunction(ActivateKeyword = "sent|s", Name = "发口令红包",
            SupportedParameters = new []{nameof(IsIdentical),
                nameof(IsCompeteInVelocity),
                nameof(Duration), nameof(Remark)})]
        public object SendPasswordRedPacket(
            [KouPluginArgument(Name = "总金额", NumberMin = 1, DefaultNumberRangeError = true)]int total,
            [KouPluginArgument(Name = "数量", NumberMin = 1, DefaultNumberRangeError = true)] int quantity,
            [KouPluginArgument(Name = "口令(默认生成6位数字)")] string password = null)
        {
            if (password == "") password = null;
            if (!ValidateAvailableTime()) return ConveyMessage;
            KouRedPacket redPacket = new KouRedPacket(CurrentPlatformUser, total, quantity, IsIdentical, password, Duration)
            {
                EndAction = RedPacketEndAction,
                IsCompeteInVelocity = IsCompeteInVelocity,
                Remark = Remark
            };

            if (redPacket.TotalCoins / redPacket.TotalCount == 0) return $"每人至少需要有{CurrentKouGlobalConfig.CoinFormat(1)}";
            var rest = CurrentUser.CoinFree - redPacket.TotalCoins;
            if (rest >= 0)
            {
                string ask =
                    $"发送总额为{CurrentKouGlobalConfig.CoinFormat(redPacket.TotalCoins)}的{quantity}个{IsCompeteInVelocity.BeIfTrue("竞速")}红包{password?.Be($"(口令为\"{password}\")")}" +
                    $"(成功后余额为{CurrentKouGlobalConfig.CoinFormat(rest)})\n" +
                    $"输入\"y\"确认";
                if (!SessionService.AskConfirm(ask)) return null;
                if (IsCompeteInVelocity)
                {
                    CurrentPlatformUser.SendMessage(CurrentPlatformGroup, "将随机在5~30秒内发送竞速红包");
                    Thread.Sleep(RandomTool.GenerateRandomInt(5000, 30000));
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
        

        public UserAccount CurrentUser { get; set; }
        public PlatformUser CurrentPlatformUser { get; set; }
        public KouGlobalConfig CurrentKouGlobalConfig { get; set; }
        public IKouSessionService SessionService { get; set; }
        public PlatformGroup CurrentPlatformGroup { get; set; }
        public PlatformGroup TargetGroup { get; set; }
    }
}

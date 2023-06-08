using System;
using System.Threading;
using Koubot.SDK.PluginInterface;
using Koubot.Shared.Protocol.Attribute;
using Koubot.Shared.Protocol.KouEnum;
using Koubot.Tool.Extensions;
using Koubot.Tool.General;
using KouGamePlugin.Maimai.Models;

namespace KouGamePlugin.Maimai;

public partial class KouMaimai
{
    #region Diving-Fish

    private bool HasBindError(MaiUserConfig config, out string reply)
    {
        if (config.Username.IsNullOrEmpty())
        {
            reply = $"{CurUser.Name}暂未绑定Diving-Fish账号呢，私聊Kou使用/mai bind 用户名 密码绑定";
            return true;
        }

        if ((DateTime.Now - config.TokenRefreshTime).Days >= 29) //29天刷新一次Token
        {
            var api = new DivingFishApi(config);
            if (!api.Login())
            {
                reply = $"刷新Token时，{CurUser.Name}登录失败了呢，Diving-Fish说：{api.ErrorMsg}";
                return true;
            }

            config.LoginTokenValue = api.TokenValue;
            config.TokenRefreshTime = DateTime.Now;
            config.SaveChanges();
        }

        reply = "";
        return false;
    }

    [PluginFunction(ActivateKeyword = "刷新", Name = "重新获取所有成绩", NeedCoin = 10)]
    public object RefreshRecords()
    {
        var config = this.UserConfig();
        if (HasBindError(config, out var reply)) return reply;
        var api = new DivingFishApi(config);
        if (!CurKouUser.HasEnoughFreeCoin(10))
        {
            return FormatNotEnoughCoin(10);
        }

        if (CDOfFunctionKouUserIsIn(_refreshCD, new TimeSpan(0, 1, 0), out var remaining))
        {
            return FormatIsInCD(remaining);
        }

        Reply($"正在刷新中...请稍后\n[{FormatConsumeFreeCoin(10)}]");

        if (!api.FetchUserRecords(CurKouUser))
        {
            CDOfUserFunctionReset(_refreshCD);
            return $"获取成绩失败：{api.ErrorMsg}";
        }

        CurKouUser.ConsumeCoinFree(10);
        config.LastGetRecordsTime = DateTime.Now;
        config.SaveChanges();
        return $"{config.Nickname}记录刷新成功！";
    }

    private const string _refreshCD = "RefreshRecords";

    #endregion

    #region 数据采集

    [PluginFunction(Name = "更新谱面统计数据", Authority = Authority.BotManager)]
    public object UpdateChartStatus()
    {
        var statusData = DivingFishApi.GetChartStatusList();
        return $"影响到{statusData?.SaveToDb()}条记录";
    }

    [PluginFunction(Name = "更新谱面数据", Authority = Authority.BotManager)]
    public object UpdateChartInfos()
    {
        var statusData = DivingFishApi.GetChartInfoList();
        return $"影响到{statusData?.SaveToDb()}条记录";
    }

    #endregion

    #region Diving-Fish

    private void AutoCheckIfNeedRefresh()
    {
        var globalConfig = this.GlobalConfig();
        if(!globalConfig.EnableAutoRefresh) return;
        var config = this.UserConfig();
        if(HasBindError(config, out var reply)) return;
        var api = new DivingFishApi(CurUser.PlatformUserId);
        var p = api.GetProfile();
        if(p == null) return;
        if (p.rating != config.OfficialRating)
        {
            Reply("已自动检测到成绩更新（Rating变化）");
            Reply(RefreshRecords() as string, 100);
        }
        //if (p.user_data?.playCount > config.PlayCount)
        //{
        //    Reply("已自动检测到成绩更新");
        //    Reply(RefreshRecords() as string, 100);
        //}
        p.FillInfo(config);
        config.SaveChanges();
    }

    [PluginFunction(ActivateKeyword = "info", Name = "获取用户信息")]
    public object UserProfile()
    {
        var config = this.UserConfig();
        if (HasBindError(config, out var reply)) return reply;
        AutoCheckIfNeedRefresh();
        return config.ToUserProfileString(CurKouUser);
    }

    [PluginFunction(ActivateKeyword = "bind",
        Name = "绑定查分账号", Help = "绑定Diving-Fish账号，需要私发Kou用户名密码", CanUseProxy = false)]
    public object BindAccount(
        [PluginArgument(Name = "用户名")] string username,
        [PluginArgument(Name = "密码")] string password)
    {
        var api = new DivingFishApi(username, password, null, CurUser.PlatformUserId);
        if (!api.Login())
        {
            return $"{CurUser.Name}登录失败了呢，Diving-Fish说：{api.ErrorMsg}";
        }

        if (api.GetProfile() is not { } profile)
        {
            return $"{CurUser.Name}登录成功，但获取用户信息失败：{api.ErrorMsg}";
        }

        if (CDOfFunctionKouUserIsIn(_refreshCD, new TimeSpan(0, 0, 1, 0), out var remaining))
        {
            return FormatIsInCD(remaining);
        }

        var config = this.UserConfig();
        var isFirstTime = config.Username == null;
        config.Username = username;
        config.Password = password;
        config.LoginTokenValue = api.TokenValue;
        config.TokenRefreshTime = DateTime.Now;
        config.LastGetRecordsTime = DateTime.Now;
        profile.FillInfo(config);
        config.SaveChanges();
        var bindSuccessFormat = $"{CurUser.Name}绑定成功！";
        var profileAppend = $"\n{config.ToUserProfileString(CurKouUser)}";
        if (isFirstTime)
        {
            Reply(bindSuccessFormat + "初次绑定，正在获取所有成绩..." + profileAppend);
            if (!api.FetchUserRecords(CurKouUser))
            {
                CDOfUserFunctionReset(_refreshCD);
                return $"获取成绩失败：{api.ErrorMsg}";
            }

            return $"{config.Nickname}记录刷新成功！";
        }

        return bindSuccessFormat + profileAppend;
    }

    #endregion


    [PluginFunction(Name = "自动刷新功能开关", Authority = Authority.BotManager)]
    public object? ConfigAuto()
    {
        var config = this.GlobalConfig();
        config.EnableAutoRefresh = !config.EnableAutoRefresh;
        config.SaveChanges();
        return $"成绩自动刷新功能已{(config.EnableAutoRefresh ? "开启" : "关闭")}";
    }
}
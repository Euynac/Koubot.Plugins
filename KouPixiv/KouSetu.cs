using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Koubot.SDK.Interface;
using Koubot.SDK.Models.Entities;
using Koubot.SDK.Protocol;
using Koubot.SDK.Protocol.Plugin;
using Koubot.Tool.Extensions;
using Koubot.Tool.General;
using Koubot.Tool.Random;

// ReSharper disable once CheckNamespace
namespace KouFunctionPlugin.Pixiv
{
    [KouPluginClass("setu", "涩图",
        Introduction = "随机涩图",
        Author = "7zou",
        PluginType = KouEnum.PluginType.Function)]
    public class KouSetu : KouPlugin<KouSetu>, IWantKouPlatformGroup
    {
        private static readonly KouColdDown<PlatformGroup> _cd = new();

        static KouSetu()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    FetchWorkInfos();
                    Thread.Sleep(10000);
                }
            });
        }

        private static void FetchWorkInfos()
        {
            SetuAPI api = new();
            var response = api.Call(100);
            if (response == null) return;
            SaveToDatabase(response);
        }
        
        [KouPluginFunction(Name = "随机一张涩图", NeedCoin = 6, OnlyUsefulInGroup = true)]
        public override object Default(string str = null)
        {
            if (_cd.IsInCd(CurrentPlatformGroup, new TimeSpan(0, 0, 10), out var remain))
                return $"还在冷却中呢，剩余{remain.Seconds:0.##}秒";
            List<PixivWork>? list = null;
            lock (_saveLock)
            {
                list = PixivWork.GetAutoModelCache();
            }
            if (list == null) return "获取失败，暂时没有作品收录";
            var img = list.Where(p => !p.R18).ToList().RandomGetOne();
            if (img == null) return "似乎没有这样的作品";
            var url = img.GetUrl();
            return url;
            
        }

        private static readonly object _saveLock = new(); 
        private static void SaveToDatabase(ResponseDto.Root root)
        {
            if(root.Data.IsNullOrEmptySet()) return;
            lock (_saveLock)
            {
                using var context = new KouContext();
                foreach (var item in root.Data)
                {
                    if (PixivWork.HasExisted(p => p.Pid == item.Pid && p.P == item.P)) continue;
                    PixivWork.Add(item.ToModel(context), out _, context);
                }
            }
        }

        public PlatformGroup CurrentPlatformGroup { get; set; }
    }
}
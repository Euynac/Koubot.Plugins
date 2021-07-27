using System.Linq;
using Koubot.SDK.Protocol;
using Koubot.SDK.Protocol.Plugin;
using Koubot.Tool.Extensions;

// ReSharper disable once CheckNamespace
namespace KouFunctionPlugin.Pixiv
{
    [KouPluginClass("setu", "涩图",
        Introduction = "随机涩图",
        Author = "7zou",
        PluginType = KouEnum.PluginType.Function)]
    public class KouSetu : KouPlugin<KouSetu>
    {
        [KouPluginFunction(Name = "随机一张涩图", NeedCoin = 6)]
        public override object Default(string str = null)
        {
            SetuAPI api = new SetuAPI();
            var response = api.Call();
            if (response == null) return "获取失败呢";
            SaveToDatabase(response);
            return response.Data.First().Urls.First();
        }

        private static readonly object _saveLock = new(); 
        private void SaveToDatabase(ResponseDto.Root root)
        {
            if(root.Data.IsNullOrEmptySet()) return;
            lock (_saveLock)
            {
                foreach (var item in root.Data)
                {
                    if (PixivWork.HasExisted(p => p.Pid == item.Pid)) continue;
                    PixivWork.Add(item.ToModel(), out _);
                }
            }
        }
        
    }
}
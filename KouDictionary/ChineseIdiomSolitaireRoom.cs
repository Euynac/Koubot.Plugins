using System;
using System.Linq;
using Koubot.SDK.Models.Entities;
using Koubot.SDK.System.Session;
using Koubot.Shared.Models;
using Koubot.Tool.Extensions;
using Koubot.Tool.General;
using KouFunctionPlugin.Models;

namespace KouFunctionPlugin;

public class ChineseSolitaireAchievement : IComparable<ChineseSolitaireAchievement>
{
    public int TryTimes { get; set; }
    public int SuccessTimes { get; set; }
    public TimeSpan TotalConsumeTime { get; set; } = new();

    public int CompareTo(ChineseSolitaireAchievement other)
    {
        return this.CompareToObjDesc(SuccessTimes, other.SuccessTimes, out var result)?
            .CompareToObjAsc(TryTimes, other.TryTimes, out result)?
            .CompareToObjAsc(TotalConsumeTime, other.TotalConsumeTime, out result) == null ? 0 : result;
    }
}
public class ChineseIdiomSolitaireRoom : KouGameRoom<ChineseSolitaireAchievement>
{
    public IdiomDictionary? CurrentIdiom { get; set; }
    public ChineseIdiomSolitaireRoom(string roomName, PlatformUser ownerUser, PlatformGroup roomGroup) : base(roomName, ownerUser, roomGroup)
    {
    }

    private string ParsePinyin(string pinyin)
    {
        pinyin = pinyin.RegexReplace("[āàáǎ]", "a");
        pinyin = pinyin.RegexReplace("[èéěē]", "e");
        pinyin = pinyin.RegexReplace("[ìíīǐ]", "i");
        pinyin = pinyin.RegexReplace("[ōòóǒ]", "o");
        return pinyin.RegexReplace("[ūǔùúüǖǘǚǜ]", "u");
    }
    public override RoomReaction Say(PlatformUser speaker, string line)
    {
        if (line == "开始")
        {
            if (CurrentIdiom != null) return $"成语接龙已经开始啦，现在请接【{CurrentIdiom}】";
            CurrentIdiom = IdiomDictionary.RandomGetOne();
            if (CurrentIdiom == null) return "成语数据缺失";
            return $"成语接龙开始啦，{(RewordPool != 0).BeIfTrue($"当前奖池{RewordPool}枚硬币，")}请接【{CurrentIdiom.Word}】";
        }

        if (CurrentIdiom != null && line.Length > 3)
        {
            using var context = new KouContext();
            var idiom = IdiomDictionary.SingleOrDefault(p => p.Word == line, context);
            if (idiom == null)
            {
                return "不知道这个成语";
            }

            var pinyin = ParsePinyin(idiom.Pinyin);
            if(pinyin.Split(' ').First() == ParsePinyin(CurrentIdiom.Pinyin).Split(' ').Last())
            {
                CurrentIdiom = idiom;
                return $"当前：{CurrentIdiom.Word}";
            }

            return "接不上噢";
        }

        return false;
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Koubot.SDK.Models.Entities;
using Koubot.SDK.System.Session;
using Koubot.Shared.Models;
using Koubot.Shared.Protocol;
using Koubot.Tool.Extensions;
using Koubot.Tool.General;
using Koubot.Tool.Random;
using KouFunctionPlugin.Models;
using Microsoft.EntityFrameworkCore;
using Mysqlx.Expr;

namespace KouFunctionPlugin;

public class ChineseSolitaireAchievement : IGameRoomLeaderBoardInfo<ChineseSolitaireAchievement>
{
    public int TryTimes { get; set; }
    public int SuccessTimes { get; set; }
    public TimeSpan TotalConsumeTime { get; set; }
    public double AverageCorrectRate => (double)SuccessTimes / (TryTimes.BeNullIfDefault() ?? 1);
    public TimeSpan CorrectAverageTime => new(TotalConsumeTime.Ticks / (SuccessTimes.BeNullIfDefault() ?? 1));
    public int CompareTo(ChineseSolitaireAchievement other)
    {
        return this.CompareToObjDesc(SuccessTimes, other.SuccessTimes, out var result)?
            .CompareToObjAsc(TryTimes, other.TryTimes, out result)?
            .CompareToObjAsc(TotalConsumeTime, other.TotalConsumeTime, out result) == null ? result : 0;
    }

    public KouMessageTableElement GetUserLeaderBoardInfo(PlatformUser user)
    {
        var element = new KouMessageTableElement("参赛人员", "回答数", "正确数", "正确率", "正确平均耗时");
        element.AddRow(user.Name ?? "??", TryTimes.ToString(), SuccessTimes.ToString(), AverageCorrectRate.ToString("P"),CorrectAverageTime.ToZhFormatString());
        return element;
    }
}

public class IdiomCache
{
    public string Word { get; set; }
    public string Pinyin { get; set; }
}

public class ChineseIdiomSolitaireRoom : KouGameRoom<ChineseSolitaireAchievement>
{
    public const string Help = "同音成语接龙。每十轮奖池增益10%，开始游戏后，默认10分钟后结算成绩，玩家不可重复使用已用过的词。";
    public IdiomCache? CurrentIdiom { get; set; }
    private readonly object _lock = new();
    public HashSet<string> UserPreviousUseWord { get; set; } = new();

    private static readonly Lazy<List<IdiomCache>> _idiomDictionary = new(() =>
    {
        using var context = new KouContext();
        return context.Set<IdiomDictionary>().AsNoTracking()
            .Select(p => new IdiomCache() {Word = p.Word, Pinyin = ParsePinyin(p.Pinyin)}).ToList();
    }, true);

    public ChineseIdiomSolitaireRoom(string roomName, PlatformUser ownerUser, PlatformGroup roomGroup, int? fee = 10) : base(roomName, ownerUser, roomGroup, fee)
    {
        RoomHelp = Help;
        UserTakePartInEvent += achievement =>
        {
            achievement.TryTimes++;
        };
        UserCorrectEvent += achievement =>
        {
            achievement.SuccessTimes++;
            achievement.TotalConsumeTime = achievement.TotalConsumeTime.Add(DateTime.Now - CurRoundStartTime);
        };

    }

    private static string ParsePinyin(string pinyin)
    {
        pinyin = pinyin.RegexReplace("[āàáǎ]", "a");
        pinyin = pinyin.RegexReplace("[èéěē]", "e");
        pinyin = pinyin.RegexReplace("[ìíīǐ]", "i");
        pinyin = pinyin.RegexReplace("[ōòóǒ]", "o");
        return pinyin.RegexReplace("[ūǔùúüǖǘǚǜ]", "u");
    }

    
    private IdiomDictionary GetInfoFromDb(IdiomCache cache,KouContext context)
    {
        return IdiomDictionary.SingleOrDefault(p => p.Word == cache.Word, context)!;
    }

    private IdiomCache? RandomGetMatchIdiom(IdiomCache idiom)
    {
        var lastPinyin = ParsePinyin(idiom.Pinyin).Split(" ").Last();
        return _idiomDictionary.Value.Where(p =>
            p.Pinyin.Split(' ').First() == lastPinyin).ToList().RandomGetOne();
    }
    private IdiomCache? RandomGetMatchIdiom(IdiomDictionary idiom)
    {
        return RandomGetMatchIdiom(new IdiomCache()
        {
            Pinyin = idiom.Pinyin,
            Word = idiom.Word,
        });
    }
    private void RandomNewIdiom()
    {
        CurrentIdiom = _idiomDictionary.Value.RandomGetOne();
    }

    private string GetCurIdiomDesc(KouContext context)
    {
        if (CurrentIdiom == null) return "【出错，当前无成语】";
        var exp = IdiomDictionary.SingleOrDefault(p => p.Word == CurrentIdiom.Word, context)?.Explanation;
        return $"【{CurrentIdiom.Word}】{exp?.Be($"，意思是：{exp}")}";
    }

    private string RoundReword()
    {
        if (RoundCount % 10 == 0)
        {
            RewordPool = (RewordPool * 1.1).Ceiling();
            return $"\n另外，当前已经第{RoundCount}轮，奖池增益10%，目前：{CurKouGlobalConfig?.CoinFormat(RewordPool)}";
        }

        return "";
    }


    public override RoomReaction PromptGetPrompt(PlatformUser speaker, string content)
    {
        if (CurrentIdiom != null)
        {
            var s = RandomGetMatchIdiom(CurrentIdiom);
            if(s == null) return "无法获取到提示";
            using var context = new KouContext();
            var info = GetInfoFromDb(s, context);
            RewordPool = (RewordPool*0.99).Ceiling();
            return
                $"提示：{info.Word.First()}开头的，意思是{info.Explanation}\n当前奖池已减少1%，目前：{CurKouGlobalConfig?.CoinFormat(RewordPool)}";
        }

        return "还未开始游戏";
    }

    public override RoomReaction DescHasStartedGame() => $"成语接龙已经开始啦，现在请接【{CurrentIdiom}】";

    public override RoomReaction PromptStartGame(PlatformUser speaker, string content)
    {
        RandomNewIdiom();
        if (CurrentIdiom == null) return "成语数据缺失";
        RecordNewRound();
        StartAutoClose();
        using var context = new KouContext();
        return $"成语接龙开始啦，可以同音不同调，参与需要前面加空格，例如【 一个顶俩】。{(RewordPool != 0).BeIfTrue($"当前奖池{RewordPool}枚硬币，")}我先来一个：{GetCurIdiomDesc(context)}";
    }

    public override RoomReaction Say(PlatformUser speaker, string line)
    {
        if (CurrentIdiom == null)
        {
            return DescNotStartGame();
        }
        if (CurrentIdiom != null && line.Length > 3)
        {
            RecordUserTakePartIn(speaker);
            if (UserPreviousUseWord.Contains(line)) return $"之前已经使用过这个成语咯，换一个把！当前【{CurrentIdiom.Word}】";
            lock (_lock)
            {
                using var context = new KouContext();
                var idiom = IdiomDictionary.SingleOrDefault(p => p.Word == line, context);
                if (idiom == null)
                {
                    return $"恕我孤陋寡闻，不认识这个成语。当前【{CurrentIdiom.Word}】";
                }

                UserPreviousUseWord.Add(line);
                var pinyin = ParsePinyin(idiom.Pinyin);
                if(pinyin.Split(' ').First() == CurrentIdiom.Pinyin.Split(' ').Last())
                {
                    RecordLastRoundWinner(speaker);
                    CurrentIdiom =
                        RandomGetMatchIdiom(idiom);
                    if (CurrentIdiom == null)
                    {
                        RewordPool += 1000;
                        RandomNewIdiom();
                        return $"我居然接不上，你太厉害了，奖池已增加1000个硬币！我再出一个新的：{GetCurIdiomDesc(context)}{RoundReword()}";
                    }
                    RecordNewRound();
                    return $"{line}{idiom.Explanation?.Be($"，意思是{idiom.Explanation}")}\n嗯...不错，那我接{GetCurIdiomDesc(context)}{RoundReword()}";
                }
                return $"虽然是成语，但是需要尾字相同，或者读音相同，声调不同也可以。当前【{CurrentIdiom.Word}】";
            }
        }

     

        return false;
    }
}
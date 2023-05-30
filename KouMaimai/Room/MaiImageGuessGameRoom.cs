using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Koubot.SDK.System;
using Koubot.SDK.System.Session;
using Koubot.Shared.Models;
using Koubot.Shared.Protocol;
using Koubot.Tool.Extensions;
using Koubot.Tool.General;
using Koubot.Tool.Random;
using KouGamePlugin.Maimai.Models;
using SixLabors.ImageSharp.Processing;


namespace KouMaimai.Room;

public class MaiImageGuessLeaderBoard : IGameRoomLeaderBoardInfo<MaiImageGuessLeaderBoard>
{
    public int TryTimes { get; set; }
    public int SuccessTimes { get; set; }
    public TimeSpan TotalConsumeTime { get; set; } = new();
    public TimeSpan CorrectAverageTime => new(TotalConsumeTime.Ticks / (SuccessTimes.BeNullIfDefault() ?? 1));
    public int CompareTo(MaiImageGuessLeaderBoard other)
    {
        return this.CompareToObjDesc(SuccessTimes, other.SuccessTimes, out var result)?
            .CompareToObjAsc(TotalConsumeTime, other.TotalConsumeTime, out result) == null ? result : 0;
    }

    public KouMessageTableElement GetUserLeaderBoardInfo(PlatformUser user)
    {
        var element = new KouMessageTableElement("参赛人员", "尝试数", "抢占数", "平均耗时");
        element.AddRow(user.Name ?? "??",TryTimes.ToString(), SuccessTimes.ToString(), CorrectAverageTime.ToZhFormatString());
        return element;
    }
}

public class MaiImageGuessGameRoom : KouGameRoom<MaiImageGuessLeaderBoard>
{
    public const string Help = "maimai歌曲猜图，每轮不定时放出随机裁切过的歌曲图片，玩家需要猜出图片对应的歌曲名称或别名，最先猜对的计分。开始游戏后，默认10分钟后结算成绩。每10轮难度升级（包含缩小、翻转、旋转、灰度等），并增益15%奖池。";
    public static Lazy<HashSet<int>> NotExistSet { get; set; } = new(() =>
    {
        var list = new List<int>();
        foreach (var (url, id) in SongChart.GetCache()!.Select(p => (p.BasicInfo.JacketUrl, p.ChartId)))
        {
            if (url.IsNullOrWhiteSpace()) continue;
            var u = new KouImage(url, new SongChart());
            if (!u.LocalExists())
            {
                list.Add(id);
            }
        }

        return list.ToHashSet();
    }, true);
    public KouMessage CurImage { get; set; }
    public SongInfo CurAnswer { get; set; }
    public MaiImageGuessGameRoom(string roomName, PlatformUser ownerUser, PlatformGroup roomGroup, int? fee) : base(roomName, ownerUser, roomGroup, fee)
    {
        RoomHelp = Help;
        UserCorrectEvent += achievement =>
        {
            CurRoundHasEnd = true;
            achievement.SuccessTimes++;
            achievement.TotalConsumeTime = achievement.TotalConsumeTime.Add(DateTime.Now - CurRoundStartTime);
        };
        UserTakePartInEvent += a =>
        {
            a.TryTimes++;
        };
    }
   
    public override RoomReaction PromptGetPrompt(PlatformUser speaker, string content)
    {
        CurRoundPromptCount++;
        var promptRank = CurRoundPromptCount * 2;
        var promptList = new List<string?>
        {
            CurAnswer.SongArtist?.BeIfNotWhiteSpace($"\n曲师：{CurAnswer.SongArtist}"),
            CurAnswer.SongGenre?.BeIfNotWhiteSpace($"\n分类：{CurAnswer.SongGenre}"),
            CurAnswer.SongBpm?.BeIfNotWhiteSpace($"\nBPM：{CurAnswer.SongBpm}"),
            CurAnswer.Remark?.BeIfNotWhiteSpace($"\n备注：{CurAnswer.Remark}")
        };
        promptList = promptList.Where(p => p.IsNotNullOrWhiteSpace()).ToList();
        if (promptList.Count < promptRank)
        {
            var exceed = promptRank - promptList.Count;
            promptList.Add($"\n曲名包含：{CurAnswer.SongTitle.Take(exceed).ToArray().ConvertToString()}");
        }
        
        return $"消耗奖池1%，奖池剩余：{CurKouGlobalConfig?.CoinFormat(RewordPool)}\n" +
               $"当前可公开的情报：{promptList.RandomGet(promptRank).StringJoin("")}";
    }

    public override RoomReaction PromptGiveUp(PlatformUser speaker, string content)
    {
        CurRoundHasEnd = true;
        var previous = CurAnswer;
        NewRound();
        return
            $"已消耗奖池2%放弃该轮，当前{CurKouGlobalConfig?.CoinFormat(RewordPool)}，答案公布：{previous.SongTitle}\n{new KouImage(previous.JacketUrl, new SongChart()).ToKouResourceString()}";
    }

    public override RoomReaction FinishGame()
    {
        if (!CurRoundHasEnd)
        {
            RoomBroadcast(
                $"答案公布：{CurAnswer.SongTitle}\\n{new KouImage(CurAnswer.JacketUrl, new SongChart()).ToKouResourceString()}");
        }
        return base.FinishGame();
    }

    public override RoomReaction PromptStartGame(PlatformUser speaker, string content)
    {
        NewRound();
        if (CurImage == null)
        {
            CurAnswer = null;
            return $"{CurAnswer?.SongTitle}数据库缺失或图片不存在，开始失败";
        }
        StartAutoClose();
        return "游戏开始啦，参与游戏开头需要加空格，最先答对歌曲名称或别名的计一分，将随机一小段时间内放出图片";
    }

    public override void NewRound(bool isRenew = false)
    {
        var hash = NotExistSet.Value;
        var info = SongChart.GetCache()!.Where(p => !hash.Contains(p.ChartId)).ToList().RandomGetOne()?.BasicInfo;
        if (info == null) return;
        CurAnswer = info;
        CurImage = null;
        var image = new KouImage(info.JacketUrl, new SongChart());
        if (!image.LocalExists())
        {
            KouLog.QuickAdd($"{info.SongTitle}图片文件不存在", KouLog.LogLevel.Error);
            return;
        }

        using var mutated = image.StartMutate();
        var rank = (5 - RoundCount / 10).LimitInRange(1, 5);
        mutated!.RandomCrop(rank / 10.0);
        if (RoundCount >= 10 && 0.5.ProbablyTrue())
        {
            mutated.Flip(FlipMode.Horizontal);
        }

        if (RoundCount >= 20 && 0.5.ProbablyTrue())
        {
            mutated.Rotate(new[] {90, 180, 270}.RandomGetOne());
        }

        if (RoundCount >= 30 && 0.5.ProbablyTrue())
        {
            mutated.Flip(FlipMode.Vertical);
        }

        if (RoundCount >= 40)
        {
            mutated.FilterGreyscale();
        }

        CurImage = mutated.SaveTemporarily();
        KouTaskDelayer.DelayInvoke(RandomTool.GetInt(5000, 15000), () =>
        {
            if (HasClosed) return;
            RecordNewRound(isRenew);
            if (!isRenew)
            {
                if (RoundCount % 10 == 0)
                {
                    RewordPool = (RewordPool * 1.15).Ceiling();
                    RoomBroadcast(
                        $"当前已到第{RoundCount}轮，难度已提升为{(RoundCount / 10 + 1).LimitInRange(5)}级，奖池增益15%，当前奖池{CurKouGlobalConfig?.CoinFormat(RewordPool)}");
                    Thread.Sleep(1000);
                }
            }


            RoomBroadcast(CurImage);
        });
    }

    public int TestAnswer(string aliasOrName)
    {
        var list = SongChart.Find(p =>
            p.BasicInfo.Aliases.Any(a => a.Alias.Equals(aliasOrName, StringComparison.OrdinalIgnoreCase)));
        if (list.Count == 1 && list[0].BasicInfo.Equals(CurAnswer)) return 1;
        var idList = SongChart.Find(p => p.OfficialId.ToString() == aliasOrName ||
                                         p.ChartId.ToString() == aliasOrName ||
                                         p.BasicInfo.Aliases.Any(a =>
                                             a.Alias.Contains(aliasOrName, StringComparison.OrdinalIgnoreCase)) ||
                                         p.BasicInfo.SongTitle.Contains(aliasOrName,
                                             StringComparison.OrdinalIgnoreCase) ||
                                         p.BasicInfo.SongTitleKaNa.Contains(aliasOrName,
                                             StringComparison.OrdinalIgnoreCase))
            .Select(p => p.BasicInfo.SongId).Distinct().ToList();
        return idList.Contains(CurAnswer.SongId) ? idList.Count : 0;
    }

    public override RoomReaction PromptSayWhenRoundStart(PlatformUser speaker, string content)
    {
        var distance = TestAnswer(content);
        if (distance == 1 || content == CurAnswer.SongTitle)
        {
            var previous = CurAnswer;
            RecordLastRoundWinner(speaker);
            NewRound();
            if (CurAnswer != null && CurImage == null) return $"{CurAnswer.SongTitle}的歌曲图片不存在，请联系bot管理员核查";

            var cost = DateTime.Now - CurRoundStartTime;
            return
                $"{speaker.Name}{(cost.TotalSeconds < 4).IIf("仅花", "耗费")}{cost.ToZhFormatString()}拿下一分{(cost.TotalSeconds < 2).BeIfTrue((cost.TotalSeconds < 1).IIf("，一定是开了挂吧？？", "，什么神仙"))}\n" +
                $"该曲目是{previous.SongTitle}\n{new KouImage(previous.JacketUrl, new SongChart()).ToKouResourceString()}";
        }

        var s = StringTool.Similarity(content, CurAnswer.SongTitle);
        return distance <= 0 ? $"{speaker.Name}不对噢，和名称相似度{s:P}" : $"{speaker.Name}的回答，找到有其他{distance}个答案，名称相似度{s:P}，再详细点呢";
    }

    public override RoomReaction Say(PlatformUser speaker, string line)
    {
        return false;
    }
}
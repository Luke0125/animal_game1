using System;
using System.Collections.Generic;
using System.Linq;

namespace FedAndFound.Core
{
    /// <summary>한 판 동안 쌓이는 기록. 업적 판정용 (§15 "선택과 결과를 업적으로 기록").</summary>
    public sealed class RunStats
    {
        public int Purified, Defeated, BossPurified, Devoured, SentHome, SoloWins, StagesCleared;
        public bool TutorialCleared;
        public readonly HashSet<string> Tags = new HashSet<string>(); // 이벤트 선택 등 (예: "event:gave_fruit")
    }

    public sealed class AchievementData
    {
        public string Id, Name, Title, Desc;
        public bool Hidden;                     // 달성 전에는 조건을 숨김 (이벤트 업적)
        public Func<RunState, bool> Check;
    }

    /// <summary>업적 15종 — 달성하면 칭호를 얻는다. 판정만 여기서 하고, 저장(영구 기록)·알림은 Game 쪽(AchievementStore)이 맡는다.</summary>
    public static class Achievements
    {
        public static readonly List<AchievementData> All = new List<AchievementData>
        {
            A("tutorial", "첫 발걸음", "견습 여행자", "0스테이지(튜토리얼)를 클리어", r => r.Stats.TutorialCleared),
            A("stage1", "연구소 탈출", "탈출자", "1스테이지를 클리어", r => r.Stats.StagesCleared >= 1),
            A("victory", "집으로", "귀향자", "3스테이지까지 클리어해 게임을 끝낸다", r => r.Phase == RunPhase.Victory),
            A("pacifist_home", "모두를 지켜서", "수호자", "한 번도 포식하지 않고 게임 클리어", r => r.Phase == RunPhase.Victory && r.Stats.Devoured == 0),
            A("devour_win", "살아남기 위해", "굶주린 생존자", "포식을 2번 하고 게임 클리어", r => r.Phase == RunPhase.Victory && r.Stats.Devoured >= 2),
            A("purify10", "정화의 손길", "정화사", "한 판에서 저주받은 동물 10마리를 정화", r => r.Stats.Purified >= 10),
            A("defeat15", "숲의 사냥꾼", "사냥꾼", "한 판에서 15마리를 물리친다", r => r.Stats.Defeated >= 15),
            A("boss_purify", "저주의 근원", "치유자", "보스의 저주를 풀어 준다", r => r.Stats.BossPurified >= 1),
            A("solo_win", "최후의 하나", "홀로 선 자", "동료가 모두 쓰러진 뒤 혼자서 전투를 이긴다", r => r.Stats.SoloWins >= 1),
            A("gems3", "원석 수집가", "원석 수집가", "한 판에서 원석 3종을 모두 만든다", r => r.Gems.Values.All(g => g > 0)),
            A("ev_kind", "다정한 이웃", "다정한 이웃", "다친 새끼 동물에게 열매를 나눠 준다", r => r.Stats.Tags.Contains("event:gave_fruit"), hidden: true),
            A("ev_fox", "여우의 친구", "여우의 친구", "굶주린 새끼 여우에게 고기를 나눠 준다", r => r.Stats.Tags.Contains("event:fed_fox"), hidden: true),
            A("ev_crate", "무모한 탐험", "무모한 탐험가", "수상한 식량 창고를 뒤져 본다", r => r.Stats.Tags.Contains("event:crate"), hidden: true),
            A("ev_spring", "샘을 되살리다", "샘지기", "오염된 샘의 정화에 성공한다", r => r.Stats.Tags.Contains("event:spring_ok"), hidden: true),
            A("ev_crow", "까마귀의 단골", "까마귀의 단골", "떠돌이 까마귀 상인과 거래한다", r => r.Stats.Tags.Contains("event:crow_trade"), hidden: true),
        };

        static AchievementData A(string id, string name, string title, string desc, Func<RunState, bool> check, bool hidden = false) =>
            new AchievementData { Id = id, Name = name, Title = title, Desc = desc, Check = check, Hidden = hidden };

        public static AchievementData Get(string id) => All.FirstOrDefault(a => a.Id == id);

        /// <summary>지금 런 상태로 조건을 만족하는 업적 id들.</summary>
        public static IEnumerable<string> Satisfied(RunState run) => All.Where(a => a.Check(run)).Select(a => a.Id);
    }
}

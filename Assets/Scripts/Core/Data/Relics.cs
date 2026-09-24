using System.Collections.Generic;

namespace FedAndFound.Core
{
    public enum RelicKind { Passive, TerrainPassive, Active, Consumable }

    public enum RelicId
    {
        BrokenTooth, GlowingSeed, SharedPantry, PurifyBell, BloodAmulet, ClearCrystal, SealedClaw,
        ThriftCharm, HeatedFang, ClearingSpring, YetiFur, FennecEar, SwampMoss, FourLeafClover,
        BeastClaw, PurifyIncense, GuardShell, LastEmber,
    }

    public sealed class RelicData
    {
        public RelicId Id; public string Name; public RelicKind Kind; public Terrain? Terrain; public string Desc;
    }

    public static class RelicDb
    {
        public static readonly Dictionary<RelicId, RelicData> All = new Dictionary<RelicId, RelicData>();

        static RelicDb()
        {
            P(RelicId.BrokenTooth, "부서진 이빨", "적을 물리치면 일정 확률로 추가 고기");
            P(RelicId.GlowingSeed, "빛나는 씨앗", "정화 성공 시 일정 확률로 추가 열매");
            P(RelicId.SharedPantry, "공동 식량통", "음식 사용 시 일정 확률로 소모되지 않음");
            P(RelicId.PurifyBell, "정화의 방울", "정화 실패 시 다음 정화 확률 증가(성공 시 초기화)");
            P(RelicId.BloodAmulet, "피 묻은 부적", "물리칠 때마다 이번 스테이지 ATK 소폭 증가");
            P(RelicId.ClearCrystal, "맑은 수정", "정화 성공마다 이번 스테이지 PURIFY 소폭 증가");
            P(RelicId.SealedClaw, "봉인된 발톱", "스킬 사용 불가, ATK·PURIFY 증가");
            P(RelicId.ThriftCharm, "절약의 부적", "스킬 배고픔 소모 감소");
            P(RelicId.HeatedFang, "달아오른 송곳니", "턴 수에 비례해 ATK 증가(상한)");
            P(RelicId.ClearingSpring, "맑아지는 샘물", "턴 수에 비례해 PURIFY 증가(상한)");
            T(RelicId.YetiFur, "설인의 털", Terrain.SnowMountain, "모든 동물 DEF 증가");
            T(RelicId.FennecEar, "사막여우의 귀", Terrain.Desert, "모든 동물 SPD 증가");
            T(RelicId.SwampMoss, "늪지의 이끼", Terrain.Swamp, "매 라운드 종료 시 HP 소량 회복");
            T(RelicId.FourLeafClover, "들판의 네잎클로버", Terrain.Plains, "전투 보상 음식 추가 확률 증가");
            Add(RelicId.BeastClaw, "맹수의 발톱", RelicKind.Active, null, "이번 기본 공격 피해 증가");
            Add(RelicId.PurifyIncense, "정화의 향로", RelicKind.Active, null, "이번 정화 확률 증가");
            Add(RelicId.GuardShell, "수호의 껍질", RelicKind.Active, null, "다음에 받는 적 공격 피해 감소");
            Add(RelicId.LastEmber, "마지막 불씨", RelicKind.Consumable, null, "혼자 남은 동물이 쓰러질 피해를 받으면 HP 1로 생존");
        }

        static void P(RelicId id, string n, string d) => Add(id, n, RelicKind.Passive, null, d);
        static void T(RelicId id, string n, Terrain t, string d) => Add(id, n, RelicKind.TerrainPassive, t, d);
        static void Add(RelicId id, string n, RelicKind k, Terrain? t, string d) =>
            All[id] = new RelicData { Id = id, Name = n, Kind = k, Terrain = t, Desc = d };

        public static RelicData Get(RelicId id) => All[id];
    }
}

using System.Collections.Generic;

namespace FedAndFound.Core
{
    public enum Diet { Herbivore, Carnivore, Omnivore }
    public enum Size { Small = 0, Medium = 1, Large = 2 }
    public enum Terrain { Swamp, SnowMountain, Plains, Desert }
    public enum FoodType { Meat, Fruit }

    public enum SkillKind { Active, Sustain, Charge, Conditional, Passive }

    public enum SkillId
    {
        Soothe,       // 토끼 달래기
        Shell,        // 거북 등딱지
        Swerve,       // 가젤 급선회
        Charge,       // 코뿔소 돌진
        Frenzy,       // 사자 광폭 (패시브)
        Ambush,       // 표범 매복
        VenomBite,    // 뱀 독 물기
        Tenacity,     // 오소리 악바리
        Wits,         // 여우 눈치
        Spines,       // 고슴도치 가시 세우기
        Mimic,        // 원숭이 모방
        Rampage,      // 멧돼지 저돌
        Graze,        // 사슴 풀 뜯기

        // 홀로서기(전투에서 혼자 남았을 때 바뀌는 스킬) — 실제 동물 습성에서 따옴 (6단계)
        Burrow,       // 토끼 토끼굴
        Pronk,        // 가젤 프롱킹
        HideCharge,   // 코뿔소 가죽 갑옷 돌진
        LoneHunt,     // 표범 고독한 사냥꾼
        Shedding,     // 뱀 허물 벗기
        Trick,        // 여우 꾀
        StoneThrow,   // 원숭이 돌 던지기
        MudBath,      // 멧돼지 진흙 목욕
    }

    public enum SkillTarget { None, Enemy, Ally }

    public sealed class SkillData
    {
        public SkillId Id; public string Name; public SkillKind Kind; public int Cost; public SkillTarget Target;
        public int MaxUsesPerBattle; // 0 = 무제한
        public string Desc;          // UI 설명 (홀로서기 스킬 위주로 채움)
        public SkillData(SkillId id, string name, SkillKind kind, int cost, SkillTarget target, int maxUses = 0, string desc = null)
        { Id = id; Name = name; Kind = kind; Cost = cost; Target = target; MaxUsesPerBattle = maxUses; Desc = desc; }
    }

    public sealed class SpeciesData
    {
        public string Id, Name; public Diet Diet; public Size Size;
        public int SpeedRank;                 // 1 = 가장 빠름 (§4.3)
        public int Hp, Atk, Def, Purify;       // Purify = 정화 효율(%p)
        public SkillData Skill;
        /// <summary>홀로서기: 전투에서 살아 있는 아군이 자기뿐일 때의 스킬. 유지형·패시브 종은 같은 스킬이 강화/변형된다(이름·설명만 다름).</summary>
        public SkillData SoloSkill;
        public string SoloTrait;              // 근거가 된 실제 습성 (UI 한 줄)
        public bool CanEat(FoodType f) =>
            Diet == Diet.Omnivore || (Diet == Diet.Carnivore ? f == FoodType.Meat : f == FoodType.Fruit);
    }

    /// <summary>12종 + 사슴(손님/대안). 수치는 GDD에 없어 제안값으로 채웠다.</summary>
    public static class SpeciesDb
    {
        public static readonly Dictionary<string, SpeciesData> All = new Dictionary<string, SpeciesData>();
        public static readonly List<SpeciesData> Roster = new List<SpeciesData>(); // 사슴 제외 12종
        public const string GuestId = "deer";

        static SpeciesDb()
        {
            // id, 이름, 식성, 몸집, 속도순위, HP, ATK, DEF, 정화, 스킬
            Add("rabbit", "토끼", Diet.Herbivore, Size.Small, 3, 70, 9, 6, 14, new SkillData(SkillId.Soothe, "달래기", SkillKind.Active, 5, SkillTarget.None));
            Add("turtle", "거북", Diet.Herbivore, Size.Medium, 11, 120, 7, 16, 8, new SkillData(SkillId.Shell, "등딱지", SkillKind.Sustain, Balance.SustainCostPerTurn, SkillTarget.None));
            Add("gazelle", "가젤", Diet.Herbivore, Size.Medium, 1, 80, 11, 6, 10, new SkillData(SkillId.Swerve, "급선회", SkillKind.Active, 5, SkillTarget.None));
            Add("rhino", "코뿔소", Diet.Herbivore, Size.Large, 12, 140, 15, 14, 4, new SkillData(SkillId.Charge, "돌진", SkillKind.Active, 15, SkillTarget.Enemy));
            Add("lion", "사자", Diet.Carnivore, Size.Large, 2, 110, 15, 8, 3, new SkillData(SkillId.Frenzy, "광폭", SkillKind.Passive, 0, SkillTarget.None));
            Add("leopard", "표범", Diet.Carnivore, Size.Medium, 4, 90, 15, 7, 5, new SkillData(SkillId.Ambush, "매복", SkillKind.Charge, 5, SkillTarget.None, 2));
            Add("snake", "뱀", Diet.Carnivore, Size.Small, 8, 70, 12, 6, 7, new SkillData(SkillId.VenomBite, "독 물기", SkillKind.Active, 10, SkillTarget.Enemy));
            Add("badger", "오소리", Diet.Carnivore, Size.Medium, 9, 115, 10, 13, 6, new SkillData(SkillId.Tenacity, "악바리", SkillKind.Sustain, Balance.SustainCostPerTurn, SkillTarget.None));
            Add("fox", "여우", Diet.Omnivore, Size.Small, 5, 75, 10, 7, 11, new SkillData(SkillId.Wits, "눈치", SkillKind.Active, 5, SkillTarget.Ally));
            Add("hedgehog", "고슴도치", Diet.Omnivore, Size.Small, 10, 85, 8, 14, 9, new SkillData(SkillId.Spines, "가시 세우기", SkillKind.Sustain, Balance.SustainCostPerTurn, SkillTarget.None));
            Add("monkey", "원숭이", Diet.Omnivore, Size.Small, 7, 80, 10, 8, 10, new SkillData(SkillId.Mimic, "모방", SkillKind.Active, 10, SkillTarget.None));
            Add("boar", "멧돼지", Diet.Omnivore, Size.Medium, 6, 115, 13, 10, 5, new SkillData(SkillId.Rampage, "저돌", SkillKind.Active, 10, SkillTarget.Enemy));
            // ===== 기본 스킬 설명 (§5.2, UI 툴팁용) =====
            Desc("rabbit", "이번 라운드 파티 전체 정화 확률 +8%p");
            Desc("turtle", "켜 두는 동안 받는 피해 -32% (매 라운드 배고픔 4)");
            Desc("gazelle", "다음 차례까지 자신에 대한 공격 회피 40%");
            Desc("rhino", "방어를 무시하는 강한 단일 공격 (계수 2.2)");
            Desc("lion", "패시브: 배고플수록 공격력 최대 +80%, 대신 배고플수록 동료를 잘못 물 확률(최대 20%)");
            Desc("leopard", "이번 차례는 쉬고, 다음 첫 공격 피해 ×2.6 (전투당 2회)");
            Desc("snake", "단일 공격(계수 1.0) + 3턴 동안 독 피해");
            Desc("badger", "켜 두는 동안 적 공격의 40%를 자신에게 끌어오고 받는 피해 -15% (매 라운드 배고픔 4)");
            Desc("fox", "동료 1마리의 행동 순서를 2칸 앞당김");
            Desc("hedgehog", "켜 두는 동안 맞으면 받은 피해의 30%를 되돌려 줌 (매 라운드 배고픔 4)");
            Desc("monkey", "직전에 동료가 쓴 액티브 스킬을 따라 함 (배고픔은 자기 몫 10만)");
            Desc("boar", "강한 단일 공격(계수 1.8), 대신 1턴 동안 자신 방어 -20%");
            Desc(GuestId, "자신의 배고픔 5를 써서 동료 1마리 HP를 최대치의 20% 회복");

            // ===== 홀로서기 (§4.4 확장, 6단계 팀 결정: "스킬 변형형 + 실제 동물 특성") =====
            Solo("rabbit", new SkillData(SkillId.Burrow, "토끼굴", SkillKind.Active, 5, SkillTarget.None, 0,
                "굴에 숨었다가 튀어나온다: 다음 차례까지 피해 -50%·회피 40%, HP 15% 회복, 다음 공격 ×2.0(뒷발차기)"), "토끼는 땅굴을 파서 천적을 피한다");
            Solo("turtle", new SkillData(SkillId.Shell, "등딱지(깊이 웅크리기)", SkillKind.Sustain, Balance.SustainCostPerTurn, SkillTarget.None, 0,
                "켜 두는 동안 받는 피해 -50%, 매 차례 HP 3% 회복"), "거북은 껍질 속에 완전히 몸을 숨기고 느긋하게 버틴다");
            Solo("gazelle", new SkillData(SkillId.Pronk, "프롱킹", SkillKind.Active, 5, SkillTarget.None, 0,
                "제자리에서 높이 뛰어 '잡기 힘들다'고 과시: 적 전체 공격 -30%(3회), 회피 60%, 다음 공격 ×1.8"), "가젤은 포식자 앞에서 높이 뛰어올라 체력을 과시한다(stotting)");
            Solo("rhino", new SkillData(SkillId.HideCharge, "가죽 갑옷 돌진", SkillKind.Active, 15, SkillTarget.Enemy, 0,
                "방어 무시 돌진(계수 1.6) + 다음 차례까지 방어 +30%"), "코뿔소의 가죽은 두께가 수 cm에 달한다");
            Solo("lion", new SkillData(SkillId.Frenzy, "광폭(무리 잃은 사자)", SkillKind.Passive, 0, SkillTarget.None, 0,
                "무리를 잃어 광폭 공격 보너스가 절반(최대 +40%)"), "사자는 고양잇과에서 유일하게 무리로 사냥한다 — 혼자면 약해진다");
            Solo("leopard", new SkillData(SkillId.LoneHunt, "고독한 사냥꾼", SkillKind.Charge, 5, SkillTarget.None, 3,
                "매복 강화: 다음 첫 공격 피해 ×3.0, 전투당 3회"), "표범은 원래 혼자 사냥하는 동물이다");
            Solo("snake", new SkillData(SkillId.Shedding, "허물 벗기", SkillKind.Active, 10, SkillTarget.None, 0,
                "HP 30% 회복 + 독·방어↓·공격↓ 해제, 다음 공격 ×1.8"), "뱀은 허물을 벗으며 몸을 새로 한다");
            Solo("badger", new SkillData(SkillId.Tenacity, "악바리(벌꿀오소리의 배짱)", SkillKind.Sustain, Balance.SustainCostPerTurn, SkillTarget.None, 0,
                "켜 두는 동안 받는 피해 -25%, 맞을 때마다 공격 +5%(최대 +30%). 혼자면 독에 면역"), "벌꿀오소리는 겁이 없고 뱀독에도 잘 버틴다");
            Solo("fox", new SkillData(SkillId.Trick, "꾀", SkillKind.Active, 5, SkillTarget.None, 0,
                "곧바로 두 번 더 행동 (라운드당 1회)"), "여우는 영리하고 몸놀림이 재빠르다");
            Solo("hedgehog", new SkillData(SkillId.Spines, "몸 말기", SkillKind.Sustain, Balance.SustainCostPerTurn, SkillTarget.None, 0,
                "켜 두는 동안 받은 피해의 50% 반격, 받는 피해 -20%"), "고슴도치는 위협을 받으면 가시 공처럼 몸을 만다");
            Solo("monkey", new SkillData(SkillId.StoneThrow, "돌 던지기", SkillKind.Active, 10, SkillTarget.Enemy, 0,
                "피해(계수 1.8) + 대상 방어↓(3턴)"), "일부 원숭이는 돌을 도구로 쓴다");
            Solo("boar", new SkillData(SkillId.MudBath, "진흙 목욕", SkillKind.Active, 10, SkillTarget.None, 0,
                "HP 15% 회복 + 방어 +30%(2턴), 방어↓ 해제"), "멧돼지는 진흙 목욕으로 몸을 식히고 보호한다");

            Add(GuestId, "사슴", Diet.Herbivore, Size.Medium, 6, 90, 9, 8, 12, new SkillData(SkillId.Graze, "풀 뜯기", SkillKind.Active, 5, SkillTarget.Ally), roster: false);
        }

        static void Add(string id, string name, Diet diet, Size size, int rank, int hp, int atk, int def, int pur, SkillData skill, bool roster = true)
        {
            var s = new SpeciesData { Id = id, Name = name, Diet = diet, Size = size, SpeedRank = rank, Hp = hp, Atk = atk, Def = def, Purify = pur, Skill = skill };
            All[id] = s;
            if (roster) Roster.Add(s);
        }

        static void Desc(string id, string text) => All[id].Skill.Desc = text;

        static void Solo(string id, SkillData s, string trait) { All[id].SoloSkill = s; All[id].SoloTrait = trait; }

        public static SpeciesData Get(string id) => All[id];
    }
}

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
    }

    public enum SkillTarget { None, Enemy, Ally }

    public sealed class SkillData
    {
        public SkillId Id; public string Name; public SkillKind Kind; public int Cost; public SkillTarget Target;
        public int MaxUsesPerBattle; // 0 = 무제한
        public SkillData(SkillId id, string name, SkillKind kind, int cost, SkillTarget target, int maxUses = 0)
        { Id = id; Name = name; Kind = kind; Cost = cost; Target = target; MaxUsesPerBattle = maxUses; }
    }

    public sealed class SpeciesData
    {
        public string Id, Name; public Diet Diet; public Size Size;
        public int SpeedRank;                 // 1 = 가장 빠름 (§4.3)
        public int Hp, Atk, Def, Purify;       // Purify = 정화 효율(%p)
        public SkillData Skill;
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
            Add("lion", "사자", Diet.Carnivore, Size.Large, 2, 110, 17, 8, 3, new SkillData(SkillId.Frenzy, "광폭", SkillKind.Passive, 0, SkillTarget.None));
            Add("leopard", "표범", Diet.Carnivore, Size.Medium, 4, 90, 15, 7, 5, new SkillData(SkillId.Ambush, "매복", SkillKind.Charge, 5, SkillTarget.None, 2));
            Add("snake", "뱀", Diet.Carnivore, Size.Small, 8, 70, 12, 6, 7, new SkillData(SkillId.VenomBite, "독 물기", SkillKind.Active, 10, SkillTarget.Enemy));
            Add("badger", "오소리", Diet.Carnivore, Size.Medium, 9, 115, 10, 13, 6, new SkillData(SkillId.Tenacity, "악바리", SkillKind.Sustain, Balance.SustainCostPerTurn, SkillTarget.None));
            Add("fox", "여우", Diet.Omnivore, Size.Small, 5, 75, 10, 7, 11, new SkillData(SkillId.Wits, "눈치", SkillKind.Active, 5, SkillTarget.Ally));
            Add("hedgehog", "고슴도치", Diet.Omnivore, Size.Small, 10, 85, 8, 14, 9, new SkillData(SkillId.Spines, "가시 세우기", SkillKind.Sustain, Balance.SustainCostPerTurn, SkillTarget.None));
            Add("monkey", "원숭이", Diet.Omnivore, Size.Small, 7, 80, 10, 8, 10, new SkillData(SkillId.Mimic, "모방", SkillKind.Active, 10, SkillTarget.None));
            Add("boar", "멧돼지", Diet.Omnivore, Size.Medium, 6, 115, 14, 10, 5, new SkillData(SkillId.Rampage, "저돌", SkillKind.Active, 10, SkillTarget.Enemy));
            Add(GuestId, "사슴", Diet.Herbivore, Size.Medium, 6, 90, 9, 8, 12, new SkillData(SkillId.Graze, "풀 뜯기", SkillKind.Active, 5, SkillTarget.Ally), roster: false);
        }

        static void Add(string id, string name, Diet diet, Size size, int rank, int hp, int atk, int def, int pur, SkillData skill, bool roster = true)
        {
            var s = new SpeciesData { Id = id, Name = name, Diet = diet, Size = size, SpeedRank = rank, Hp = hp, Atk = atk, Def = def, Purify = pur, Skill = skill };
            All[id] = s;
            if (roster) Roster.Add(s);
        }

        public static SpeciesData Get(string id) => All[id];
    }
}

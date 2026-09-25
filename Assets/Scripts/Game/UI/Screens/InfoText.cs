using FedAndFound.Core;

namespace FedAndFound.Game.UI.Screens
{
    /// <summary>툴팁에 쓰는 설명 문구 모음 (스킬·동물·유물). 수치는 Core 데이터에서 가져온다.</summary>
    public static class InfoText
    {
        public static string Species(SpeciesData s)
        {
            string t = $"{s.Name}  ·  {SpeciesSelectScreen.DietName(s.Diet)}  ·  속도 {s.SpeedRank}위\n"
                     + $"HP {s.Hp}  공격 {s.Atk}  방어 {s.Def}  정화 효율 {s.Purify}\n\n"
                     + $"[스킬] {s.Skill.Name}\n{s.Skill.Desc}";
            if (s.SoloSkill != null)
                t += $"\n\n[홀로서기] {s.SoloSkill.Name}\n{s.SoloSkill.Desc}\n— {s.SoloTrait}";
            return t;
        }

        public static string Skill(SkillData k, SpeciesData owner = null)
        {
            string t = $"{k.Name}\n{k.Desc}";
            if (owner != null && k == owner.SoloSkill) t += $"\n— {owner.SoloTrait}";
            return t;
        }

        public static string EnemySkill(Unit e) => $"{e.Name}의 스킬: {e.Species.Skill.Name}\n{Battle.EnemySkillDesc(e.Species.Skill)}";

        public static string Relic(RelicId r)
        {
            var d = RelicDb.Get(r);
            string kind = d.Kind switch
            {
                RelicKind.Active => "액티브 (전투당 1회, 행동과 함께 사용)",
                RelicKind.Consumable => "소모형",
                RelicKind.TerrainPassive => $"지형 패시브 ({RunState.TerrainName(d.Terrain.Value)}에서만)",
                _ => "패시브",
            };
            return $"{d.Name}\n{d.Desc}\n\n{kind}";
        }
    }
}

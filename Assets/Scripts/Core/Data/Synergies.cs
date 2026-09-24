using System.Collections.Generic;
using System.Linq;

namespace FedAndFound.Core
{
    /// <summary>원석 → 시너지 스킬 (§4.5, §8, §20-8 팀 확정안).
    /// 식성별 원석 1개를 만들면 그 런 동안 시너지가 켜진다(패시브, 행동 소모 없음). 같은 원석을 더 만들어도 중첩되지 않는다.
    /// 실제 효과 계산은 Battle(Atk/Purify/정화 성공 처리)에 있고, 여기는 이름·설명과 조합 판정만 둔다.</summary>
    public static class Synergies
    {
        public static string GemName(Diet d) => d switch
        {
            Diet.Carnivore => "포식의 원석", Diet.Herbivore => "생명의 원석", _ => "적응의 원석",
        };

        public static string SkillName(Diet d) => d switch
        {
            Diet.Carnivore => "무리사냥", Diet.Herbivore => "생명의 순환", _ => "적응",
        };

        public static string Desc(Diet d) => d switch
        {
            Diet.Carnivore => $"전투 중인 육식동물 1마리마다 아군 전체 공격력 +{Balance.PackHuntAtkPerCarnivore * 100:0}%",
            Diet.Herbivore => $"초식동물이 정화에 성공할 때마다 아군 전체 배고픔 +{Balance.CycleHungerRestore}",
            _ => "잡식동물이 있을 때 파티 구성에 따라: 초식+잡식 = 정화↑ / 육식+잡식 = 공격↑ / 셋 다 = 둘 다 소폭↑ / 잡식뿐 = 적 HP 50% 이상엔 공격↑, 미만엔 정화↑",
        };

        /// <summary>적응(잡식 시너지)의 현재 형태. 전투 중(기절하지 않은) 아군 구성으로 판정한다.</summary>
        public enum AdaptMode { None, HerbOmni, CarnOmni, All, OmniOnly }

        public static AdaptMode Adapt(IEnumerable<Unit> activeAllies)
        {
            var diets = new HashSet<Diet>(activeAllies.Select(a => a.Species.Diet));
            if (!diets.Contains(Diet.Omnivore)) return AdaptMode.None;
            bool h = diets.Contains(Diet.Herbivore), c = diets.Contains(Diet.Carnivore);
            return h && c ? AdaptMode.All : h ? AdaptMode.HerbOmni : c ? AdaptMode.CarnOmni : AdaptMode.OmniOnly;
        }

        public static string AdaptName(AdaptMode m) => m switch
        {
            AdaptMode.HerbOmni => $"초식+잡식: 정화 +{Balance.AdaptPurify:0}%p",
            AdaptMode.CarnOmni => $"육식+잡식: 공격 +{Balance.AdaptAtk * 100:0}%",
            AdaptMode.All => $"초식+육식+잡식: 공격 +{Balance.AdaptAllAtk * 100:0}%, 정화 +{Balance.AdaptAllPurify:0}%p",
            AdaptMode.OmniOnly => $"잡식뿐: 적 HP 50% 이상 공격 +{Balance.AdaptSoloAtk * 100:0}% / 미만 정화 +{Balance.AdaptSoloPurify:0}%p",
            _ => "잡식동물이 없어 비활성",
        };
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace FedAndFound.Core
{
    /// <summary>선택지형 이벤트 (§15). 목록 확장은 Pool에 항목을 추가하면 된다.</summary>
    public sealed class RandomEvent
    {
        public sealed class Option
        {
            public string Text;
            public Func<RunState, bool> CanChoose = _ => true;
            public Func<RunState, string> Apply;
        }

        public string Title, Body;
        public List<Option> Options = new List<Option>();

        static readonly List<Func<RandomEvent>> Pool = new List<Func<RandomEvent>>
        {
            () => new RandomEvent
            {
                Title = "다친 동물", Body = "덤불 속에 다친 새끼 동물이 떨고 있다.",
                Options =
                {
                    new Option { Text = "열매 2개를 준다", CanChoose = r => r.Fruit >= 2,
                        Apply = r => { r.Fruit -= 2; r.UniversalFragments++; return "고마워하는 눈빛… 반짝이는 만능 조각을 남기고 떠났다."; } },
                    new Option { Text = "지나간다", Apply = r => "못 본 척 지나쳤다." },
                },
            },
            () => new RandomEvent
            {
                Title = "버려진 식량 창고", Body = "연구소 트럭에서 떨어진 상자가 보인다. 냄새가 수상하다.",
                Options =
                {
                    new Option { Text = "뒤져본다", Apply = r =>
                    {
                        if (r.Rng.Chance(0.6f)) { r.Meat += 2; r.Fruit += 2; return "고기 2, 열매 2를 얻었다!"; }
                        foreach (var a in r.Party.Where(p => !p.Fainted)) a.Hp = Math.Max(1, a.Hp - a.MaxHp / 10);
                        return "오염된 음식이었다… 모두 HP가 줄었다.";
                    } },
                    new Option { Text = "지나간다", Apply = r => "위험은 피하는 게 상책이다." },
                },
            },
        };

        public static RandomEvent Roll(IRng rng) => rng.Pick(Pool)();
    }
}

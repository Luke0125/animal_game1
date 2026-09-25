using System;
using System.Collections.Generic;
using System.Linq;

namespace FedAndFound.Core
{
    /// <summary>선택지형 이벤트 (§15, §20-9 제안안 7종). 목록 확장은 Pool에 항목을 추가하면 된다.
    /// 원칙: 모든 선택지는 득과 실이 함께 있거나 "지나간다"류 무난한 선택이 있어야 한다(딜레마).</summary>
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
                        Apply = r => { r.Fruit -= 2; r.UniversalFragments++; r.Stats.Tags.Add("event:gave_fruit"); return "고마워하는 눈빛… 반짝이는 만능 조각을 남기고 떠났다."; } },
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
                        r.Stats.Tags.Add("event:crate");
                        if (r.Rng.Chance(0.6f)) { r.Meat += 2; r.Fruit += 2; return "고기 2, 열매 2를 얻었다!"; }
                        foreach (var a in r.Party.Where(p => !p.Fainted)) a.Hp = Math.Max(1, a.Hp - a.MaxHp / 10);
                        return "오염된 음식이었다… 모두 HP가 줄었다.";
                    } },
                    new Option { Text = "지나간다", Apply = r => "위험은 피하는 게 상책이다." },
                },
            },
            () => new RandomEvent
            {
                Title = "연구소 순찰대", Body = "멀리서 연구소 트럭 엔진 소리가 들린다. 트럭 짐칸엔 먹이 상자가 실려 있다.",
                Options =
                {
                    new Option { Text = "덤불에 숨어 기다린다 (전원 배고픔 -10)", Apply = r =>
                    {
                        foreach (var a in r.Party) a.AddHunger(-10);
                        return "한참을 숨죽여 기다렸다. 배가 고프다.";
                    } },
                    new Option { Text = "짐칸을 습격한다 (전원 HP -15%, 고기 +3)", Apply = r =>
                    {
                        foreach (var a in r.Party.Where(p => !p.Fainted)) a.Hp = Math.Max(1, a.Hp - a.MaxHp * 15 / 100);
                        r.Meat += 3;
                        return "상처를 입었지만 고기 3개를 챙겼다!";
                    } },
                },
            },
            () => new RandomEvent
            {
                Title = "떠돌이 까마귀 상인", Body = "반짝이는 걸 좋아하는 까마귀가 거래를 제안한다.",
                Options =
                {
                    new Option { Text = "고기 2 → 열매 3", CanChoose = r => r.Meat >= 2,
                        Apply = r => { r.Meat -= 2; r.Fruit += 3; r.Stats.Tags.Add("event:crow_trade"); return "열매 3개를 받았다."; } },
                    new Option { Text = "열매 2 → 고기 3", CanChoose = r => r.Fruit >= 2,
                        Apply = r => { r.Fruit -= 2; r.Meat += 3; r.Stats.Tags.Add("event:crow_trade"); return "고기 3개를 받았다."; } },
                    new Option { Text = "고기 1 + 열매 1 → 무작위 조각 1", CanChoose = r => r.Meat >= 1 && r.Fruit >= 1,
                        Apply = r =>
                        {
                            r.Meat--; r.Fruit--; r.Stats.Tags.Add("event:crow_trade");
                            var d = (Diet)r.Rng.Range(0, 3);
                            r.Fragments[d]++;
                            return $"{DietName(d)} 조각 1개를 받았다.";
                        } },
                    new Option { Text = "거절한다", Apply = r => "까마귀가 시큰둥하게 날아갔다." },
                },
            },
            () => new RandomEvent
            {
                Title = "오염된 샘", Body = "검붉게 물든 샘에서 저주의 기운이 새어 나온다.",
                Options =
                {
                    new Option { Text = "정화를 시도한다 (성공 50%)", Apply = r =>
                    {
                        if (r.Rng.Chance(0.5f)) { r.UniversalFragments++; r.Stats.Tags.Add("event:spring_ok"); return "샘이 맑아졌다! 바닥에서 만능 조각을 찾았다."; }
                        foreach (var a in r.Party) a.AddHunger(-12);
                        return "저주에 기운을 빼앗겼다… 전원 배고픔 -12.";
                    } },
                    new Option { Text = "멀리 돌아간다", Apply = r => "괜한 위험은 피했다." },
                },
            },
            () => new RandomEvent
            {
                Title = "굶주린 새끼 여우", Body = "비쩍 마른 새끼 여우가 먹이를 쳐다보고 있다.",
                Options =
                {
                    new Option { Text = "고기 2개를 나눠 준다 (이번 스테이지 정화 +5%p)", CanChoose = r => r.Meat >= 2, Apply = r =>
                    {
                        r.Meat -= 2; r.StageBuffs.Purify += 5f; r.Stats.Tags.Add("event:fed_fox");
                        return "새끼 여우가 꼬리를 흔든다. 마음이 맑아진다. (이번 스테이지 정화 +5%p)";
                    } },
                    new Option { Text = "쫓아낸다", Apply = r => "새끼 여우는 덤불 속으로 사라졌다." },
                },
            },
            () => new RandomEvent
            {
                Title = "오래된 사냥터", Body = "커다란 짐승의 발자국이 숲 안쪽으로 이어진다.",
                Options =
                {
                    new Option { Text = "흔적을 따라간다 (전원 배고픔 -8, 이번 스테이지 공격 +10%)", Apply = r =>
                    {
                        foreach (var a in r.Party) a.AddHunger(-8);
                        r.StageBuffs.Atk += 0.1f;
                        return "사냥 감각이 깨어났다! (이번 스테이지 공격 +10%)";
                    } },
                    new Option { Text = "지나간다", Apply = r => "발자국을 뒤로하고 길을 재촉했다." },
                },
            },
        };

        static string DietName(Diet d) => d == Diet.Herbivore ? "초식" : d == Diet.Carnivore ? "육식" : "잡식";

        public static RandomEvent Roll(IRng rng) => rng.Pick(Pool)();

        // 테스트·데모용: 특정 이벤트를 골라 만들기 (CoreSim 전수 검사, 에디터 테스트 패널)
        public static int Count => Pool.Count;
        public static RandomEvent Create(int index) => Pool[index]();
    }
}

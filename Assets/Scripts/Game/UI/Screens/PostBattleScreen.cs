using FedAndFound.Core;
using UnityEngine;

namespace FedAndFound.Game.UI.Screens
{
    /// <summary>전투 후: 보상 확인 → 음식 먹기(배고픔만 회복, §9.1) → [휴식](배고픔 소모 → HP 회복) 또는 [계속 이동].</summary>
    public static class PostBattleScreen
    {
        public static void Build(RectTransform root, GameManager gm)
        {
            var run = gm.Run;
            var layout = UIFactory.VGroup(root, 12, new RectOffset(24, 24, 20, 20));
            UIFactory.Stretch(layout);

            UIFactory.Label(layout, "전투 종료", 28, TextAnchor.MiddleCenter, bold: true);
            var rw = run.LastRewards;
            if (rw != null)
                UIFactory.Label(layout, $"보상: 고기 +{rw.Meat}  열매 +{rw.Fruit}  조각 +{rw.Fragments.Count}", 18, TextAnchor.MiddleCenter, Theme.TextDim);
            UIFactory.Label(layout, $"보유: 고기 {run.Meat}  열매 {run.Fruit}", 18, TextAnchor.MiddleCenter, Theme.TextDim);

            foreach (var a in run.Party) PartyFeedRow(layout, gm, run, a);

            var btnRow = UIFactory.HGroup(layout, 12);
            UIFactory.FixedHeight(btnRow, 54);
            UIFactory.Button(btnRow, $"휴식 (배고픔 -{Balance.RestHungerCost} → HP 회복)", gm.Rest, !run.RestedThisStop, fontSize: 20);
            UIFactory.Button(btnRow, "계속 이동", gm.Continue, bg: Theme.Accent, fontSize: 20);
        }

        static void PartyFeedRow(Transform parent, GameManager gm, RunState run, Unit a)
        {
            var card = UIFactory.Panel(parent, "Feed", Theme.Panel, stretch: false);
            UIFactory.FixedHeight(card, 70);
            var h = UIFactory.HGroup(card, 10, new RectOffset(8, 8, 6, 6));
            UIFactory.Stretch(h);

            var info = UIFactory.VGroup(h, 2);
            UIFactory.Label(info, a.Fainted ? $"{a.Name} (기절 — 스테이지 클리어 시 부활)" : a.Name, 16);
            UIFactory.Bar(info, a.HpRatio, Theme.HpBar, $"HP {a.Hp}/{a.MaxHp}", 11);
            UIFactory.Bar(info, (float)a.Hunger / Balance.MaxHunger, Theme.HungerBar, $"배고픔 {a.Hunger}/{Balance.MaxHunger}", 11);

            UIFactory.Button(h, $"고기 먹기 (+{Balance.FoodRestore})", () => gm.Feed(a, FoodType.Meat),
                run.Meat > 0 && a.Species.CanEat(FoodType.Meat), fontSize: 15);
            UIFactory.Button(h, $"열매 먹기 (+{Balance.FoodRestore})", () => gm.Feed(a, FoodType.Fruit),
                run.Fruit > 0 && a.Species.CanEat(FoodType.Fruit), fontSize: 15);
        }
    }
}

using FedAndFound.Core;
using UnityEngine;

namespace FedAndFound.Game.UI.Screens
{
    /// <summary>스테이지 클리어: 파티원 1마리를 [포식](배고픔 즉시 회복) 또는 [집 보내기](유물 슬롯 +1)로 처리한다(§3).</summary>
    public static class FarewellScreen
    {
        public static void Build(RectTransform root, GameManager gm)
        {
            var run = gm.Run;
            var layout = UIFactory.VGroup(root, 14, new RectOffset(24, 24, 20, 20));
            UIFactory.Stretch(layout);

            UIFactory.Label(layout, $"{run.Stage}스테이지 클리어! 동료 1마리를 보내야 해", 26, TextAnchor.MiddleCenter, bold: true);
            UIFactory.Label(layout, "어느 쪽을 골라도 유물 2개를 얻어. 포식은 즉시 배고픔 회복, 집 보내기는 유물 슬롯이 늘어나.", 16, TextAnchor.MiddleCenter, Theme.TextDim);

            foreach (var a in run.Party)
            {
                var row = UIFactory.HGroup(layout, 10);
                UIFactory.FixedHeight(row, 54);
                UIFactory.Label(row, $"{a.Name} (HP {a.Hp}/{a.MaxHp})", 18);
                UIFactory.Button(row, "포식", () => gm.Farewell(a, true), bg: Theme.Warn, fontSize: 18);
                UIFactory.Button(row, "집 보내기", () => gm.Farewell(a, false), bg: Theme.Accent, fontSize: 18);
            }
        }
    }
}

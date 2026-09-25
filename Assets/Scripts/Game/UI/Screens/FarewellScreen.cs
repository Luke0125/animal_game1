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
            var layout = UIFactory.Window(root, new Vector2(0.1f, 0.22f), new Vector2(0.9f, 0.95f), $"{run.Stage}스테이지 클리어! 동료 1마리와 헤어져야 해", padding: 28);
            UIFactory.Label(layout, "어느 쪽을 골라도 유물 2개를 얻어. 포식은 남은 동료의 배고픔을 즉시 채우고, 집 보내기는 유물 슬롯이 늘어나.", 17, TextAnchor.MiddleCenter, Theme.TextDim);

            var row = UIFactory.HGroup(layout, 16);
            UIFactory.FixedHeight(row, 330);
            foreach (var a in run.Party)
            {
                var card = UIFactory.Panel(row, "Card", new Color(0.2f, 0.17f, 0.13f), stretch: false);
                UIFactory.Tooltip(card, InfoText.Species(a.Species));
                var v = UIFactory.VGroup(card, 6, new RectOffset(14, 14, 12, 12));
                UIFactory.Stretch(v);
                UIFactory.Icon(v, BattleView.AnimalArt.Get(a.Species.Id), 150);
                UIFactory.Label(v, a.Name, 24, TextAnchor.MiddleCenter, Theme.Gold, bold: true);
                int gain = Balance.DevourRestoreBySize[(int)a.Species.Size];
                UIFactory.Label(v, $"{SpeciesSelectScreen.DietName(a.Species.Diet)} · 포식 시 배고픔 +{gain}", 14, TextAnchor.MiddleCenter, Theme.TextDim);
                UIFactory.Button(v, "포식", () => gm.Farewell(a, true), bg: new Color(0.6f, 0.22f, 0.18f), fontSize: 19);
                UIFactory.Button(v, "집 보내기", () => gm.Farewell(a, false), bg: new Color(0.3f, 0.48f, 0.3f), fontSize: 19);
            }
        }
    }
}

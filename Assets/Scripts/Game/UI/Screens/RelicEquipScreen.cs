using System.Linq;
using FedAndFound.Core;
using UnityEngine;

namespace FedAndFound.Game.UI.Screens
{
    /// <summary>스테이지 입장 전: 지형 미리보기(§11) + 유물 장착(§10). 0스테이지는 튜토리얼 보스전으로 바로 이어진다.</summary>
    public static class RelicEquipScreen
    {
        public static void Build(RectTransform root, GameManager gm)
        {
            var run = gm.Run;
            var layout = UIFactory.VGroup(root, 10, new RectOffset(24, 24, 20, 20));
            UIFactory.Stretch(layout);

            UIFactory.Label(layout, $"{run.Stage}스테이지 — 지형: {RunState.TerrainName(run.Terrain)}", 30, TextAnchor.MiddleCenter, bold: true);
            UIFactory.Label(layout, $"유물 슬롯 {run.EquippedRelics.Count}/{run.RelicSlots} 장착 중  (보유 {run.OwnedRelics.Count}개)", 20, TextAnchor.MiddleCenter, Theme.TextDim);

            var list = UIFactory.ScrollList(layout, 520);
            foreach (var r in run.OwnedRelics)
            {
                var data = RelicDb.Get(r);
                bool equipped = run.EquippedRelics.Contains(r);
                bool effective = run.IsRelicEffective(r);
                var row = UIFactory.HGroup(list, 10);
                UIFactory.FixedHeight(row, 46);
                string label = (equipped ? "[장착] " : "") + $"{data.Name} — {data.Desc}" + (effective ? "" : " (지형 불일치)");
                var color = !effective ? Theme.Disabled : equipped ? Theme.Selected : Theme.PanelLight;
                UIFactory.Button(row, label, () => { if (equipped) gm.UnequipRelic(r); else gm.EquipRelic(r); },
                    interactable: equipped || run.EquippedRelics.Count < run.RelicSlots, bg: color, fontSize: 17);
            }

            UIFactory.Button(layout, run.Stage == 0 ? "튜토리얼 보스전 시작" : "노드 맵으로", gm.ConfirmEquip, bg: Theme.Accent, fontSize: 26);
        }
    }
}

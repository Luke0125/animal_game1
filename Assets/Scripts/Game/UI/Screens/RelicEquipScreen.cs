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
            // 뒤 배경(BackdropView)이 이번 스테이지 지형이라 그대로 "지형 미리보기"가 된다 (§11)
            var layout = UIFactory.Window(root, new Vector2(0.2f, 0.24f), new Vector2(0.8f, 0.95f),
                $"{run.Stage}스테이지 — {RunState.TerrainName(run.Terrain)}", padding: 28);
            UIFactory.Label(layout, $"유물 슬롯 {run.EquippedRelics.Count}/{run.RelicSlots} 장착 중  (보유 {run.OwnedRelics.Count}개) — 유물에 마우스를 올리면 설명", 17, TextAnchor.MiddleCenter, Theme.TextDim);

            var list = UIFactory.ScrollList(layout, 480);
            foreach (var r in run.OwnedRelics)
            {
                var data = RelicDb.Get(r);
                bool equipped = run.EquippedRelics.Contains(r);
                bool effective = run.IsRelicEffective(r);
                var row = UIFactory.HGroup(list, 10);
                UIFactory.FixedHeight(row, 46);
                string label = (equipped ? "[장착] " : "") + $"{data.Name} — {data.Desc}" + (effective ? "" : " (지형 불일치)");
                var color = !effective ? Theme.Disabled : equipped ? new Color(0.62f, 0.5f, 0.22f) : Theme.PanelLight;
                var b = UIFactory.Button(row, label, () => { if (equipped) gm.UnequipRelic(r); else gm.EquipRelic(r); },
                    interactable: equipped || run.EquippedRelics.Count < run.RelicSlots, bg: color, fontSize: 17);
                UIFactory.Tooltip(b, InfoText.Relic(r));
            }

            UIFactory.Button(layout, run.Stage == 0 ? "튜토리얼 보스전 시작" : "출발 — 노드 맵으로", gm.ConfirmEquip, bg: new Color(0.55f, 0.43f, 0.2f), fontSize: 24);
        }
    }
}

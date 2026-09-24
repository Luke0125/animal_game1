using FedAndFound.Core;
using FedAndFound.Game.Field;
using UnityEngine;

namespace FedAndFound.Game.UI.Screens
{
    /// <summary>노드 맵(§12)의 HUD: 보스 전 최대 3회 이동, 이벤트는 그중 1회. 파티 상태·재화·원석 제작도 여기서 확인한다.
    /// 실제 맵은 화면 가운데로 비치는 탑뷰 필드(Field/FieldView)가 그린다.</summary>
    public static class MapScreen
    {
        public static void Build(RectTransform root, GameManager gm)
        {
            var run = gm.Run;

            // 가운데는 비워 둔다 — 그 아래로 탑뷰 필드(Field/FieldView, 월드 공간 타일맵)가 보인다(4단계)
            var top = UIFactory.Panel(root, "TopHud", new Color(Theme.Background.r, Theme.Background.g, Theme.Background.b, 0.88f));
            UIFactory.Stretch(top, new Vector2(0, 0.8f), Vector2.one);
            var layout = UIFactory.VGroup(top, 6, new RectOffset(24, 24, 10, 10));
            UIFactory.Stretch(layout);

            UIFactory.Label(layout, $"{run.Stage}스테이지 — {RunState.TerrainName(run.Terrain)}  (이동 {run.NodesMoved}/{RunState.NodesBeforeBoss})", 26, TextAnchor.MiddleCenter, bold: true);
            UIFactory.Label(layout, $"고기 {run.Meat}  열매 {run.Fruit}   조각(초/육/잡) {run.Fragments[Diet.Herbivore]}/{run.Fragments[Diet.Carnivore]}/{run.Fragments[Diet.Omnivore]}  만능 {run.UniversalFragments}   원석(초/육/잡) {run.Gems[Diet.Herbivore]}/{run.Gems[Diet.Carnivore]}/{run.Gems[Diet.Omnivore]}",
                17, TextAnchor.MiddleCenter, Theme.TextDim);

            var partyRow = UIFactory.HGroup(layout, 8);
            UIFactory.FixedHeight(partyRow, 70);
            foreach (var a in run.Party) PartyMiniCard(partyRow, a);

            var bottom = UIFactory.Panel(root, "BottomHud", new Color(Theme.Background.r, Theme.Background.g, Theme.Background.b, 0.88f));
            UIFactory.Stretch(bottom, Vector2.zero, new Vector2(1, 0.17f));
            var bl = UIFactory.VGroup(bottom, 6, new RectOffset(24, 24, 8, 8));
            UIFactory.Stretch(bl);

            UIFactory.Label(bl, "방향키/WASD로 길을 따라 걷기 · 필드의 노드를 클릭하면 자동 이동 · 반짝이는 노드에 도착하면 진입",
                15, TextAnchor.MiddleCenter, Theme.TextDim);

            var nodeRow = UIFactory.HGroup(bl, 12);
            UIFactory.FixedHeight(nodeRow, 48);
            foreach (var n in run.NodeOptions())
            {
                var node = n;
                UIFactory.Button(nodeRow, NodeLabel(node), () => GoToNode(gm, node), bg: node == NodeType.Boss ? Theme.Danger : Theme.PanelLight, fontSize: 20);
            }

            var gemRow = UIFactory.HGroup(bl, 10);
            UIFactory.FixedHeight(gemRow, 40);
            foreach (Diet d in new[] { Diet.Herbivore, Diet.Carnivore, Diet.Omnivore })
                UIFactory.Button(gemRow, $"{SpeciesSelectScreen.DietName(d)} 원석 제작 (조각 3)", () => gm.CraftGem(d), interactable: run.CanCraftGem(d), fontSize: 16);
        }

        /// <summary>버튼으로 고른 노드도 필드에서 말이 걸어간 뒤 진입한다. 필드가 없거나 걸을 수 없으면 바로 진입.</summary>
        static void GoToNode(GameManager gm, NodeType n)
        {
            var field = FieldView.Instance;
            if (field != null && field.Walking) return; // 이미 걸어가는 중이면 중복 입력 무시
            if (field == null || !field.WalkToNode(n)) gm.EnterNode(n);
        }

        static void PartyMiniCard(Transform parent, Unit a)
        {
            var card = UIFactory.Panel(parent, "Mini", Theme.Panel, stretch: false);
            var v = UIFactory.VGroup(card, 2, new RectOffset(6, 6, 4, 4));
            UIFactory.Stretch(v);
            UIFactory.Label(v, a.Fainted ? $"{a.Name}(기절)" : a.Name, 16, TextAnchor.MiddleCenter);
            UIFactory.Bar(v, a.HpRatio, a.HpRatio > 0.3f ? Theme.HpBar : Theme.HpBarLow, $"{a.Hp}/{a.MaxHp}", 12);
            UIFactory.Bar(v, (float)a.Hunger / Balance.MaxHunger, Theme.HungerBar, $"배고픔 {a.Hunger}", 12);
        }

        static string NodeLabel(NodeType n) => n switch { NodeType.Mob => "몹 전투로 이동", NodeType.Event => "이벤트 살펴보기", _ => "보스전 돌입" };
    }
}

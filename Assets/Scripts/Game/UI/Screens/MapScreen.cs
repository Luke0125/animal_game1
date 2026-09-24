using System.Collections.Generic;
using System.Linq;
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
            UIFactory.Label(layout, SynergySummary(run, null), 16, TextAnchor.MiddleCenter, Theme.Selected);

            var partyRow = UIFactory.HGroup(layout, 8);
            UIFactory.FixedHeight(partyRow, 70);
            foreach (var a in run.Party) PartyMiniCard(partyRow, a);

            var bottom = UIFactory.Panel(root, "BottomHud", new Color(Theme.Background.r, Theme.Background.g, Theme.Background.b, 0.88f));
            UIFactory.Stretch(bottom, Vector2.zero, new Vector2(1, 0.17f));
            var bl = UIFactory.VGroup(bottom, 6, new RectOffset(24, 24, 8, 8));
            UIFactory.Stretch(bl);

            if (gm.LastEventResult != null)
                UIFactory.Label(bl, "이벤트 결과: " + gm.LastEventResult, 17, TextAnchor.MiddleCenter, Theme.Selected);
            else
                UIFactory.Label(bl, "방향키/WASD로 길을 따라 걷기 · 필드의 노드를 클릭하면 자동 이동 · 반짝이는 노드에 도착하면 진입",
                    15, TextAnchor.MiddleCenter, Theme.TextDim);

            var nodeRow = UIFactory.HGroup(bl, 12);
            UIFactory.FixedHeight(nodeRow, 48);
            foreach (var n in run.NodeOptions())
            {
                var node = n;
                UIFactory.Button(nodeRow, NodeLabel(node), () => GoToNode(gm, node), bg: node == NodeType.Boss ? Theme.Danger : Theme.PanelLight, fontSize: 20);
            }

            if (Debug.isDebugBuild) DebugEventPanel(root, gm); // 에디터·개발 빌드 전용 (정식 빌드에서는 안 보임)

            var gemRow = UIFactory.HGroup(bl, 10);
            UIFactory.FixedHeight(gemRow, 40);
            foreach (Diet d in new[] { Diet.Herbivore, Diet.Carnivore, Diet.Omnivore })
            {
                string label = run.HasSynergy(d)
                    ? $"[해금] {Synergies.SkillName(d)}"
                    : $"{Synergies.GemName(d)} 제작 → {Synergies.SkillName(d)} (조각 3)";
                UIFactory.Button(gemRow, label, () => gm.CraftGem(d), interactable: run.CanCraftGem(d), fontSize: 15);
            }
            // 아직 없는 시너지의 효과를 미리 보여 줘야 어떤 원석을 먼저 만들지 고를 수 있다
            var locked = new[] { Diet.Herbivore, Diet.Carnivore, Diet.Omnivore }.Where(d => !run.HasSynergy(d)).ToList();
            if (locked.Count > 0)
                UIFactory.Label(bl, string.Join("   ", locked.Select(d => $"{Synergies.SkillName(d)}: {ShortDesc(d)}")), 13, TextAnchor.MiddleCenter, Theme.TextDim);
        }

        static string ShortDesc(Diet d) => d switch
        {
            Diet.Carnivore => $"육식 1마리당 공격 +{Balance.PackHuntAtkPerCarnivore * 100:0}%",
            Diet.Herbivore => $"초식 정화 성공 시 전원 배고픔 +{Balance.CycleHungerRestore}",
            _ => "잡식이 있을 때 파티 구성 따라 공격/정화↑",
        };

        /// <summary>해금된 시너지 한 줄 요약. 전투 중이면 그 전투의 실제 형태(적응 모드)를 보여 준다.</summary>
        public static string SynergySummary(RunState run, Battle battle)
        {
            var parts = new List<string>();
            if (run.HasSynergy(Diet.Carnivore))
            {
                int n = battle != null ? battle.Allies.Count(a => a.Active && a.Species.Diet == Diet.Carnivore)
                                       : run.Party.Count(a => !a.Fainted && a.Species.Diet == Diet.Carnivore);
                parts.Add($"무리사냥 +{n * Balance.PackHuntAtkPerCarnivore * 100:0}%");
            }
            if (run.HasSynergy(Diet.Herbivore)) parts.Add("생명의 순환");
            if (run.HasSynergy(Diet.Omnivore))
            {
                var mode = battle != null ? battle.CurrentAdapt() : Synergies.Adapt(run.Party.Where(a => !a.Fainted));
                parts.Add($"적응({Synergies.AdaptName(mode)})");
            }
            return parts.Count == 0 ? "시너지 없음 — 조각 3개로 원석을 만들면 시너지 스킬이 해금된다" : "시너지: " + string.Join(" · ", parts);
        }

        /// <summary>이벤트는 무작위라 원하는 걸 확인하기 어렵다 → 에디터/개발 빌드에서만 오른쪽에 테스트 패널을 띄운다.
        /// 이벤트를 고르면 "다음 이벤트 노드"에 예약되고, 평소처럼 ? 노드로 걸어가면 그 이벤트가 열린다.</summary>
        static void DebugEventPanel(RectTransform root, GameManager gm)
        {
            var run = gm.Run;
            var panel = UIFactory.Panel(root, "DebugEvents", new Color(0.3f, 0.1f, 0.3f, 0.85f));
            UIFactory.Stretch(panel, new Vector2(0.84f, 0.2f), new Vector2(1f, 0.79f));
            var v = UIFactory.VGroup(panel, 4, new RectOffset(8, 8, 8, 8));
            UIFactory.Stretch(v);
            UIFactory.Label(v, "[테스트] 이벤트 고르기", 15, TextAnchor.MiddleCenter, bold: true);
            UIFactory.Button(v, $"이벤트 무제한: {(run.DebugUnlimitedEvents ? "ON" : "OFF")}", gm.DebugToggleUnlimitedEvents,
                bg: run.DebugUnlimitedEvents ? Theme.Accent : Theme.PanelLight, fontSize: 14);
            for (int i = 0; i < RandomEvent.Count; i++)
            {
                int idx = i;
                bool chosen = run.ForcedEventIndex == i;
                UIFactory.Button(v, (chosen ? "[예약] " : "") + RandomEvent.Create(i).Title, () => gm.DebugForceEvent(idx),
                    bg: chosen ? Theme.Selected : Theme.PanelLight, fontSize: 14);
            }
            UIFactory.Label(v, run.NodeOptions().Contains(NodeType.Event) ? "고른 뒤 ? 노드로 이동" : "이번 스테이지 이벤트 사용함\n→ 무제한 ON", 13, TextAnchor.MiddleCenter, Theme.TextDim);
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

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
            UIFactory.Label(layout, $"고기 {run.Meat}   열매 {run.Fruit}   (조각·원석은 왼쪽 아래 원석 도감)", 17, TextAnchor.MiddleCenter, Theme.TextDim);
            UIFactory.Label(layout, SynergySummary(run, null), 16, TextAnchor.MiddleCenter, Theme.Selected);

            var partyRow = UIFactory.HGroup(layout, 8);
            UIFactory.FixedHeight(partyRow, 70);
            foreach (var a in run.Party) PartyMiniCard(partyRow, a);

            TerrainSign(root, run);
            GemCodex(root, run);

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

            if (gm.CheatsEnabled) DebugEventPanel(root, gm); // 데모 모드 또는 에디터·개발 빌드에서만

            var gemRow = UIFactory.HGroup(bl, 10);
            UIFactory.FixedHeight(gemRow, 40);
            foreach (Diet d in new[] { Diet.Herbivore, Diet.Carnivore, Diet.Omnivore })
            {
                string label = run.HasSynergy(d)
                    ? $"[해금] {Synergies.SkillName(d)}"
                    : $"{Synergies.GemName(d)} 제작 → {Synergies.SkillName(d)} (조각 3)";
                var gem = UIFactory.Button(gemRow, label, () => gm.CraftGem(d), interactable: run.CanCraftGem(d), fontSize: 15);
                UIFactory.Tooltip(gem, $"{Synergies.GemName(d)} → {Synergies.SkillName(d)}\n{Synergies.Desc(d)}\n\n같은 식성 조각 {Balance.FragmentsPerGem}개(부족분은 만능 조각)로 제작. 한 번 만들면 이번 런 동안 계속 켜진다.");
            }
            // 아직 없는 시너지의 효과를 미리 보여 줘야 어떤 원석을 먼저 만들지 고를 수 있다
            var locked = new[] { Diet.Herbivore, Diet.Carnivore, Diet.Omnivore }.Where(d => !run.HasSynergy(d)).ToList();
            if (locked.Count > 0)
                UIFactory.Label(bl, string.Join("   ", locked.Select(d => $"{Synergies.SkillName(d)}: {ShortDesc(d)}")), 13, TextAnchor.MiddleCenter, Theme.TextDim);
        }

        /// <summary>레퍼런스의 양피지 지형 표지판: 지형 이름 + 그 지형의 장식 그림.</summary>
        static void TerrainSign(RectTransform root, RunState run)
        {
            var v = UIFactory.Window(root, new Vector2(0.01f, 0.56f), new Vector2(0.13f, 0.78f), parchment: true, padding: 16);
            UIFactory.Label(v, RunState.TerrainName(run.Terrain), 30, TextAnchor.MiddleCenter, Theme.Ink, bold: true);
            var tiles = Field.TerrainTiles.Get(run.Terrain);
            var row = UIFactory.HGroup(v, 2);
            UIFactory.FixedHeight(row, 64);
            foreach (var d in tiles.Deco) UIFactory.Icon(row, d.sprite, 56);
            UIFactory.Label(v, $"{run.Stage}스테이지", 15, TextAnchor.MiddleCenter, new Color(0.45f, 0.32f, 0.2f));
        }

        public static Color GemColor(Diet d) => d switch
        {
            Diet.Herbivore => new Color(0.35f, 0.78f, 0.38f), Diet.Carnivore => new Color(0.88f, 0.28f, 0.28f), _ => new Color(0.98f, 0.72f, 0.22f),
        };

        /// <summary>레퍼런스의 "원석 도감": 식성별 원석(가졌으면 빛나고 없으면 ?) + 조각 수. 마우스를 올리면 시너지 설명.</summary>
        static void GemCodex(RectTransform root, RunState run)
        {
            var v = UIFactory.Window(root, new Vector2(0.01f, 0.19f), new Vector2(0.2f, 0.54f), padding: 16);
            UIFactory.Label(v, "원석 도감", 22, TextAnchor.MiddleCenter, Theme.Gold, bold: true);
            var row = UIFactory.HGroup(v, 6);
            UIFactory.FixedHeight(row, 110);
            foreach (Diet d in new[] { Diet.Herbivore, Diet.Carnivore, Diet.Omnivore })
            {
                bool owned = run.HasSynergy(d);
                var slot = UIFactory.Panel(row, "Slot", new Color(0.05f, 0.05f, 0.05f, 0.6f), stretch: false);
                UIFactory.Tooltip(slot, $"{Synergies.GemName(d)} → {Synergies.SkillName(d)} {(owned ? "(해금됨)" : "(미획득)")}\n{Synergies.Desc(d)}\n\n{SpeciesSelectScreen.DietName(d)} 조각 {run.Fragments[d]}/{Balance.FragmentsPerGem}");
                var sv = UIFactory.VGroup(slot, 2, new RectOffset(4, 4, 6, 4));
                UIFactory.Stretch(sv);
                var gem = UIFactory.Icon(sv, UISkin.Gem(GemColor(d), (int)d), 56);
                if (!owned) gem.color = new Color(0.2f, 0.2f, 0.2f, 0.9f); // 미획득 = 어두운 실루엣
                UIFactory.Label(sv, owned ? Synergies.SkillName(d) : $"조각 {run.Fragments[d]}/{Balance.FragmentsPerGem}", 13, TextAnchor.MiddleCenter,
                    owned ? Theme.Gold : Theme.TextDim);
            }
            UIFactory.Label(v, $"만능 조각 {run.UniversalFragments}  (어느 원석에나 사용)", 14, TextAnchor.MiddleCenter, Theme.TextDim);
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

        /// <summary>데모 패널(7단계): 발표 5분 시연용 치트 + 이벤트 고르기. 데모 모드 또는 에디터/개발 빌드에서만 보인다.
        /// 이벤트를 고르면 "다음 이벤트 노드"에 예약되고, 평소처럼 ? 노드로 걸어가면 그 이벤트가 열린다.</summary>
        static void DebugEventPanel(RectTransform root, GameManager gm)
        {
            var run = gm.Run;
            var panel = UIFactory.Panel(root, "DebugEvents", new Color(0.3f, 0.1f, 0.3f, 0.85f));
            UIFactory.Stretch(panel, new Vector2(0.82f, 0.18f), new Vector2(1f, 0.795f));
            var v = UIFactory.VGroup(panel, 3, new RectOffset(8, 8, 6, 6));
            UIFactory.Stretch(v);
            UIFactory.Label(v, "[데모] 빠른 진행", 15, TextAnchor.MiddleCenter, bold: true);
            DemoRow(v, ("조각 +3", gm.DemoAddFragments), ("음식 +5", gm.DemoAddFood));
            DemoRow(v, ("전원 회복", gm.DemoHealAll), ("보스 앞으로", gm.DemoSkipToBoss));
            if (run.Stage < RunState.FinalStage)
                DemoRow(v, (run.Stage < 2 ? "2스테이지로" : "", run.Stage < 2 ? () => gm.DemoJumpToStage(2) : (System.Action)null),
                           ("3스테이지로", () => gm.DemoJumpToStage(3)));
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

        static void DemoRow(Transform parent, params (string label, System.Action act)[] buttons)
        {
            var row = UIFactory.HGroup(parent, 4);
            UIFactory.FixedHeight(row, 38);
            foreach (var (label, act) in buttons)
                if (act != null) UIFactory.Button(row, label, act, bg: new Color(0.45f, 0.2f, 0.45f), fontSize: 13);
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
            var card = UIFactory.Panel(parent, "Mini", new Color(0.2f, 0.17f, 0.13f), stretch: false);
            UIFactory.Tooltip(card, InfoText.Species(a.Species));
            var h = UIFactory.HGroup(card, 6, new RectOffset(6, 6, 3, 3));
            UIFactory.Stretch(h);
            var face = UIFactory.Icon(h, BattleView.AnimalArt.Get(a.Species.Id), 60);
            if (a.Fainted) face.color = new Color(0.4f, 0.4f, 0.4f);
            var v = UIFactory.VGroup(h, 2);
            UIFactory.Label(v, a.Fainted ? $"{a.Name}(기절)" : a.Name, 16, TextAnchor.MiddleCenter);
            UIFactory.Bar(v, a.HpRatio, a.HpRatio > 0.3f ? Theme.HpBar : Theme.HpBarLow, $"{a.Hp}/{a.MaxHp}", 12);
            UIFactory.Bar(v, (float)a.Hunger / Balance.MaxHunger, Theme.HungerBar, $"배고픔 {a.Hunger}", 12);
        }

        static string NodeLabel(NodeType n) => n switch { NodeType.Mob => "몹 전투로 이동", NodeType.Event => "이벤트 살펴보기", _ => "보스전 돌입" };
    }
}

using System.Collections.Generic;
using System.Linq;
using FedAndFound.Core;
using FedAndFound.Game.BattleView;
using UnityEngine;

namespace FedAndFound.Game.UI.Screens
{
    /// <summary>사이드뷰 전투 화면의 UI 틀 (레퍼런스 전투 화면 배치).
    /// 상단: 스테이지 배너 · 초상화 턴 순서 바(§4.4) · 적 정보(HP·정화 확률 §17) / 가운데: 비워 둠 → 뒤의 BattleStageView(동물·연출)가 보임
    /// 하단: 아군 카드(초상화·HP·배고픔·스탯) · 스테이지 유물 · 커맨드 메뉴.
    /// 이 UI는 상태가 바뀔 때마다 통째로 다시 그려지므로 애니메이션은 BattleStageView가 맡는다.</summary>
    public static class BattleScreen
    {
        static readonly RelicId[] ActiveRelics = { RelicId.BeastClaw, RelicId.PurifyIncense, RelicId.GuardShell };
        static Color PanelBg => new Color(0.09f, 0.08f, 0.07f, 0.88f);
        static readonly Color Gold = new Color(0.86f, 0.7f, 0.38f);
        static readonly Color DemoColor = new Color(0.45f, 0.2f, 0.45f);

        public static void Build(RectTransform root, GameManager gm)
        {
            var run = gm.Run;
            var battle = run.CurrentBattle;
            var domain = TargetDomain(gm, battle);

            Banner(root, run, battle);
            TurnBar(root, battle);
            EnemyPanel(root, gm, battle);

            var mid = Region(root, "Mid", 0.25f, 0.80f, 0.72f, 0.875f, null);
            var mv = UIFactory.VGroup(mid, 2); UIFactory.Stretch(mv);
            UIFactory.Label(mv, MapScreen.SynergySummary(run, battle), 15, TextAnchor.MiddleCenter, Theme.Selected);
            if (gm.CheatsEnabled && battle.Outcome == BattleOutcome.Ongoing) DemoRow(mv, gm, battle);

            LogStrip(root, gm);
            if (domain != null) HintBar(root, domain.Value);

            var cards = Region(root, "Allies", 0.005f, 0.01f, 0.595f, 0.34f, null);
            var cardRow = UIFactory.HGroup(cards, 8); UIFactory.Stretch(cardRow);
            foreach (var a in battle.Allies) AllyCard(cardRow, gm, battle, a, domain);

            RelicPanel(root, gm, battle);
            CommandPanel(root, gm, battle, domain);
        }

        /// <summary>BattleStageView의 필드 클릭과 같은 규칙.</summary>
        public static SkillTarget? TargetDomain(GameManager gm, Battle battle)
        {
            if (gm.PendingAction is ActionType.Attack or ActionType.Purify) return SkillTarget.Enemy;
            if (gm.PendingAction == ActionType.Skill) return battle.EffectiveSkill(battle.CurrentActor)?.Target;
            return null;
        }

        static RectTransform Region(Transform root, string name, float x0, float y0, float x1, float y1, Color? bg)
        {
            var rt = bg.HasValue ? UIFactory.Panel(root, name, bg.Value) : new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            if (!bg.HasValue) rt.SetParent(root, false);
            UIFactory.Stretch(rt, new Vector2(x0, y0), new Vector2(x1, y1));
            return rt;
        }

        // ================= 상단 =================

        static void Banner(Transform root, RunState run, Battle battle)
        {
            var p = Region(root, "Banner", 0.005f, 0.875f, 0.24f, 0.995f, PanelBg);
            var v = UIFactory.VGroup(p, 2, new RectOffset(14, 10, 8, 6)); UIFactory.Stretch(v);
            bool boss = battle.Enemies.Any(e => e.IsBoss);
            string where = run.Stage == 0 ? "튜토리얼" : $"{RunState.TerrainName(run.Terrain)}의 오염지 {run.Stage}-{(boss ? "보스" : run.NodesMoved.ToString())}";
            UIFactory.Label(v, where, 24, TextAnchor.MiddleLeft, Gold, bold: true);
            UIFactory.Label(v, boss ? "저주의 근원을 쓰러뜨리거나 풀어 주자." : "저주받은 동물들을 정화하거나 물리치자.", 15, TextAnchor.MiddleLeft);
            UIFactory.Label(v, $"라운드 {battle.Round}", 14, TextAnchor.MiddleLeft, Theme.TextDim);
        }

        /// <summary>레퍼런스처럼 원형 초상화를 가로로 나열. 적은 빨간 테두리, 현재 차례는 금색 + ▼.</summary>
        static void TurnBar(Transform root, Battle battle)
        {
            var area = Region(root, "TurnBar", 0.25f, 0.875f, 0.72f, 0.995f, null);
            var queue = battle.TurnQueue.Skip(TurnIndex(battle)).Where(x => x.Active).Take(8).ToList();
            const float size = 78f, gap = 14f;
            float total = queue.Count * size + (queue.Count - 1) * gap;
            for (int i = 0; i < queue.Count; i++)
            {
                var u = queue[i];
                bool cur = i == 0 && battle.CurrentActor == u;
                var ring = new GameObject("Turn", typeof(RectTransform)).GetComponent<RectTransform>();
                ring.SetParent(area, false);
                ring.anchorMin = ring.anchorMax = new Vector2(0.5f, 0.45f);
                ring.sizeDelta = Vector2.one * (cur ? size * 1.12f : size);
                ring.anchoredPosition = new Vector2(-total / 2 + size / 2 + i * (size + gap), 0);
                var img = ring.gameObject.AddComponent<UnityEngine.UI.Image>();
                img.sprite = Field.TerrainTiles.Circle(64, new Color(0.16f, 0.14f, 0.12f), u.IsEnemy ? new Color(0.85f, 0.2f, 0.2f) : cur ? Gold : new Color(0.8f, 0.78f, 0.7f));
                img.raycastTarget = false;
                var icon = UIFactory.Icon(ring, AnimalArt.Get(u.Species.Id), size * 0.78f, flipX: u.IsEnemy);
                icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                icon.rectTransform.anchoredPosition = new Vector2(0, -2);
                if (cur)
                {
                    var tag = UIFactory.Label(ring, "▼ 차례", 14, TextAnchor.MiddleCenter, Gold, bold: true, stretch: false);
                    tag.rectTransform.anchorMin = tag.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                    tag.rectTransform.sizeDelta = new Vector2(90, 20);
                    tag.rectTransform.anchoredPosition = new Vector2(0, -12);
                }
                if (i < queue.Count - 1)
                {
                    var arrow = UIFactory.Label(area, ">", 26, TextAnchor.MiddleCenter, Theme.TextDim, stretch: false);
                    arrow.rectTransform.anchorMin = arrow.rectTransform.anchorMax = new Vector2(0.5f, 0.45f);
                    arrow.rectTransform.sizeDelta = new Vector2(gap + 6, 40);
                    arrow.rectTransform.anchoredPosition = new Vector2(-total / 2 + size + gap / 2 + i * (size + gap), 0);
                }
            }
        }

        static int TurnIndex(Battle battle)
        {
            var cur = battle.CurrentActor;
            if (cur == null) return 0;
            for (int i = 0; i < battle.TurnQueue.Count; i++) if (battle.TurnQueue[i] == cur) return i;
            return 0;
        }

        /// <summary>우상단 적 정보: 이름·HP·정화 성공률(현재 행동 동물 기준, §17)·상태.</summary>
        static void EnemyPanel(Transform root, GameManager gm, Battle battle)
        {
            var p = Region(root, "Enemies", 0.76f, 0.77f, 0.995f, 0.995f, PanelBg);
            var v = UIFactory.VGroup(p, 4, new RectOffset(12, 12, 8, 8)); UIFactory.Stretch(v);
            var actor = battle.CurrentActor;
            foreach (var e in battle.Enemies.Where(x => x.Active))
            {
                var name = UIFactory.Label(v, $"{(e.IsBoss ? "[보스] " : "")}타락한 {e.Name}{EnemyTags(e)}", 17, TextAnchor.MiddleLeft, bold: true);
                UIFactory.Tooltip(name, InfoText.EnemySkill(e));
                UIFactory.Bar(v, e.HpRatio, Theme.HpBarLow, $"{e.Hp} / {e.MaxHp}", 13);
                if (actor != null && !actor.IsEnemy)
                {
                    float pc = battle.PurifyChance(actor, e, gm.PendingRelic == RelicId.PurifyIncense);
                    UIFactory.Label(v, $"정화 성공률 {pc:0}%  ({actor.Name} 기준)", 14, TextAnchor.MiddleLeft, new Color(0.6f, 0.9f, 0.6f));
                }
            }
        }

        /// <summary>적의 스킬 상태 표시(5단계 적 AI) — 매복처럼 예고가 있어야 방어로 대응할 수 있다.</summary>
        static string EnemyTags(Unit e)
        {
            var tags = new List<string>();
            if (e.ChargedReady) tags.Add("노리는 중!");
            if (e.SustainOn) tags.Add(e.Species.Skill.Name);
            if (e.Evade > 0) tags.Add("회피");
            if (e.PoisonTurns > 0) tags.Add($"독 {e.PoisonTurns}");
            if (e.DefDownTurns > 0) tags.Add("방어↓");
            if (e.AtkDownTurns > 0) tags.Add("공격↓");
            return tags.Count == 0 ? "" : "  [" + string.Join(", ", tags) + "]";
        }

        /// <summary>데모 모드(7단계): 정화 시연(적 HP 10%) · 홀로서기 시연(동료 기절) · 즉시 승리.</summary>
        static void DemoRow(Transform parent, GameManager gm, Battle battle)
        {
            var row = UIFactory.HGroup(parent, 8, new RectOffset(60, 60, 0, 0));
            UIFactory.FixedHeight(row, 32);
            UIFactory.Button(row, "[데모] 적 HP 10%", gm.DemoWeakenEnemies, !gm.Animating, bg: DemoColor, fontSize: 14);
            UIFactory.Button(row, "[데모] 혼자 남기기", gm.DemoGoSolo, !gm.Animating && battle.Allies.Count(a => a.Active) > 1, bg: DemoColor, fontSize: 14);
            UIFactory.Button(row, "[데모] 즉시 승리", gm.DemoWinNow, !gm.Animating, bg: DemoColor, fontSize: 14);
        }

        // ================= 가운데 =================

        static void LogStrip(Transform root, GameManager gm)
        {
            var p = Region(root, "Log", 0.005f, 0.735f, 0.24f, 0.87f, new Color(0, 0, 0, 0.45f)); // 배너 아래 — 동물과 겹치지 않는 자리
            var v = UIFactory.VGroup(p, 1, new RectOffset(10, 8, 4, 4)); UIFactory.Stretch(v);
            foreach (var line in gm.BattleLog.Skip(Mathf.Max(0, gm.BattleLog.Count - 5)))
                UIFactory.Label(v, line, 14, TextAnchor.MiddleLeft, new Color(0.9f, 0.9f, 0.88f));
        }

        static void HintBar(Transform root, SkillTarget domain)
        {
            var p = Region(root, "Hint", 0.36f, 0.72f, 0.64f, 0.78f, new Color(0.5f, 0.12f, 0.1f, 0.85f));
            UIFactory.Label(p, domain == SkillTarget.Ally ? "도울 동료를 클릭하세요" : "대상 적을 클릭하세요 (빨간 ▼)", 20, TextAnchor.MiddleCenter, bold: true);
        }

        // ================= 하단 =================

        static void AllyCard(Transform parent, GameManager gm, Battle battle, Unit a, SkillTarget? domain)
        {
            bool cur = a == battle.CurrentActor;
            bool pick = domain == SkillTarget.Ally && a.Active;
            var card = UIFactory.Panel(parent, "Ally", pick ? new Color(0.45f, 0.38f, 0.15f, 0.95f) : cur ? new Color(0.22f, 0.19f, 0.13f, 0.95f) : PanelBg, stretch: false);
            UIFactory.Tooltip(card, InfoText.Species(a.Species) + (battle.IsSolo(a) ? "\n\n지금 혼자 남아 홀로서기 중!" : ""));
            if (pick)
            {
                var btn = card.gameObject.AddComponent<UnityEngine.UI.Button>();
                btn.onClick.AddListener(() => gm.SubmitBattleAction(ActionType.Skill, a));
            }
            var h = UIFactory.HGroup(card, 8, new RectOffset(8, 8, 8, 8)); UIFactory.Stretch(h);
            var portrait = UIFactory.Icon(h, AnimalArt.Get(a.Species.Id), 96);
            if (a.Fainted) portrait.color = new Color(0.4f, 0.4f, 0.4f);

            var v = UIFactory.VGroup(h, 3);
            string tag = a.Fainted ? " (기절)" : a.Defending ? " (방어)" : a.SustainOn ? " (유지)" : "";
            if (!a.Fainted && a.PoisonTurns > 0) tag += $" (독 {a.PoisonTurns})";
            if (battle.IsSolo(a)) tag += " (홀로)";
            if (a.ChargedReady) tag += " (노리는 중)";
            UIFactory.Label(v, a.Name + tag, 19, TextAnchor.MiddleLeft, cur ? Gold : Theme.Text, bold: true);
            UIFactory.Label(v, DietLabel(a.Species.Diet), 13, TextAnchor.MiddleLeft, Theme.TextDim);
            UIFactory.Bar(v, a.HpRatio, a.HpRatio > 0.3f ? Theme.HpBar : Theme.HpBarLow, $"HP {a.Hp}/{a.MaxHp}", 12);
            UIFactory.Bar(v, (float)a.Hunger / Balance.MaxHunger, Theme.HungerBar, $"배고픔 {a.Hunger}/{Balance.MaxHunger}", 12);
            UIFactory.Label(v, $"공격 {battle.Atk(a):0}   방어 {battle.Def(a):0}   속도 {a.Species.SpeedRank}위", 14, TextAnchor.MiddleLeft, Theme.TextDim);
        }

        static string DietLabel(Diet d) => d switch { Diet.Herbivore => "초식 · 열매", Diet.Carnivore => "육식 · 고기", _ => "잡식 · 고기/열매" };

        static void RelicPanel(Transform root, GameManager gm, Battle battle)
        {
            var p = Region(root, "Relics", 0.6f, 0.01f, 0.705f, 0.34f, PanelBg);
            var v = UIFactory.VGroup(p, 4, new RectOffset(8, 8, 8, 8)); UIFactory.Stretch(v);
            UIFactory.Label(v, "스테이지 유물", 16, TextAnchor.MiddleCenter, Gold, bold: true);
            var equipped = gm.Run.EquippedRelics.Where(gm.Run.IsRelicEffective).ToList();
            if (equipped.Count == 0) UIFactory.Label(v, "없음", 14, TextAnchor.MiddleCenter, Theme.TextDim);
            foreach (var r in equipped)
            {
                if (ActiveRelics.Contains(r) && battle.CanUseRelic(r) && battle.Outcome == BattleOutcome.Ongoing && !gm.Animating)
                    UIFactory.Tooltip(UIFactory.Button(v, RelicDb.Get(r).Name + (gm.PendingRelic == r ? " (사용)" : ""), () => gm.ToggleRelic(r),
                        bg: gm.PendingRelic == r ? Theme.Selected : Theme.PanelLight, fontSize: 14), InfoText.Relic(r) + "\n누르면 이번 행동에 함께 사용");
                else
                    UIFactory.Tooltip(UIFactory.Label(v, RelicDb.Get(r).Name, 14, TextAnchor.MiddleCenter), InfoText.Relic(r));
            }
        }

        static void CommandPanel(Transform root, GameManager gm, Battle battle, SkillTarget? domain)
        {
            var p = Region(root, "Commands", 0.71f, 0.01f, 0.995f, 0.34f, PanelBg);
            var v = UIFactory.VGroup(p, 6, new RectOffset(12, 12, 10, 10)); UIFactory.Stretch(v);

            if (battle.Outcome != BattleOutcome.Ongoing)
            {
                bool win = battle.Outcome == BattleOutcome.Victory;
                UIFactory.Label(v, win ? "승리!" : "전멸…", 34, TextAnchor.MiddleCenter, win ? Gold : Theme.Danger, bold: true);
                if (!gm.Animating) UIFactory.Button(v, "확인", gm.FinishBattleAndContinue, bg: Theme.Accent, fontSize: 24);
                return;
            }
            var actor = battle.CurrentActor;
            if (gm.Animating || actor == null || actor.IsEnemy)
            {
                UIFactory.Label(v, actor != null && actor.IsEnemy ? $"{actor} 행동 중…" : "…", 20, TextAnchor.MiddleCenter, Theme.TextDim);
                return;
            }

            if (gm.PendingAction.HasValue)
            {
                UIFactory.Label(v, domain == SkillTarget.Ally ? "동료를 고르세요" : "대상을 고르세요", 18, TextAnchor.MiddleCenter, Gold, bold: true);
                if (domain == SkillTarget.Enemy)
                    foreach (var e in battle.Enemies.Where(x => x.Active))
                    {
                        string pc = gm.PendingAction == ActionType.Purify ? $"  정화 {battle.PurifyChance(actor, e, gm.PendingRelic == RelicId.PurifyIncense):0}%" : "";
                        UIFactory.Button(v, $"{e.Name}  {e.Hp}/{e.MaxHp}{pc}", () => gm.SubmitBattleAction(gm.PendingAction.Value, e), bg: Theme.Danger, fontSize: 17);
                    }
                UIFactory.Button(v, "취소", gm.CancelPending, fontSize: 16);
                return;
            }

            var skill = battle.CurrentSkill(actor); // 혼자 남으면 홀로서기 스킬로 바뀐다
            if (battle.IsSolo(actor) && actor.Species.SoloSkill != null)
            {
                float bonus = (battle.SoloDesperation(actor) - 1f) * 100f;
                UIFactory.Label(v, $"홀로서기! {(bonus > 0 ? $"궁지 본능 +{bonus:0}%" : "")}", 15, TextAnchor.MiddleCenter, Theme.Selected, bold: true);
                UIFactory.Label(v, $"{skill.Name}: {skill.Desc}", 13, TextAnchor.MiddleCenter, Theme.TextDim);
            }
            Command(v, "▶  기본 공격", () => gm.SetPendingAction(ActionType.Attack), true, highlight: true,
                tip: $"적 1마리를 공격 (지금 공격력 {battle.Atk(actor):0}).\n물리치면 고기 + 무작위 조각, 잡몹이면 영구 능력치 조금 상승.");
            Command(v, "◎  정화", () => gm.SetPendingAction(ActionType.Purify), true,
                tip: $"적의 저주를 풀어 전투에서 빼낸다.\n확률 = 저주 게이지(적 HP를 깎을수록 ↑) + {actor.Name}의 정화 효율.\n성공하면 열매 + 조각 (보스는 만능 조각).");
            if (skill.Kind == SkillKind.Sustain)
                Command(v, $"◆  {skill.Name} {(actor.SustainOn ? "끄기" : "켜기")}", () => gm.ToggleSustain(actor), true, selected: actor.SustainOn,
                    tip: InfoText.Skill(skill, actor.Species) + "\n(유지형: 켜고 끄는 데 행동을 쓰지 않음)");
            else if (skill.Kind != SkillKind.Passive)
            {
                bool ok = battle.CanUseSkill(actor, out _);
                string uses = skill.MaxUsesPerBattle > 0 ? $" {actor.SkillUses}/{skill.MaxUsesPerBattle}" : "";
                Command(v, $"◆  {skill.Name}  (배고픔 {battle.SkillCost(actor)}{uses})", () => gm.SetPendingAction(ActionType.Skill), ok,
                    tip: InfoText.Skill(skill, actor.Species) + (ok ? "" : $"\n\n지금은 사용 불가: {Why(battle, actor)}"));
            }
            Command(v, "■  방어", () => gm.SetPendingAction(ActionType.Defend), true,
                tip: "다음 차례까지 받는 피해 절반. \"노리는 중!\"인 적이 있으면 특히 유용.");
        }

        static string Why(Battle b, Unit u) { b.CanUseSkill(u, out var reason); return reason; }

        static void Command(Transform parent, string label, System.Action act, bool ok, bool highlight = false, bool selected = false, string tip = null)
        {
            var btn = UIFactory.Button(parent, label, act, ok,
                bg: selected ? Theme.Selected : highlight ? new Color(0.55f, 0.43f, 0.2f) : new Color(0.2f, 0.18f, 0.16f), fontSize: 19);
            var text = btn.GetComponentInChildren<UnityEngine.UI.Text>();
            if (text != null) { text.alignment = TextAnchor.MiddleLeft; text.rectTransform.offsetMin = new Vector2(16, 0); text.raycastTarget = false; }
            UIFactory.Tooltip(btn, tip);
        }
    }
}

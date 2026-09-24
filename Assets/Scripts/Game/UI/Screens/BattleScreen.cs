using System.Linq;
using FedAndFound.Core;
using UnityEngine;

namespace FedAndFound.Game.UI.Screens
{
    /// <summary>사이드뷰 전투 화면. 턴 순서 바(§4.4) · 적/아군 정보 · 커맨드(공격/정화/방어/스킬) ·
    /// 유지형 토글 · 액티브 유물 · 전투 로그(연출 대신 텍스트, 3단계에서 애니메이션으로 보강).</summary>
    public static class BattleScreen
    {
        static readonly RelicId[] ActiveRelics = { RelicId.BeastClaw, RelicId.PurifyIncense, RelicId.GuardShell };

        public static void Build(RectTransform root, GameManager gm)
        {
            var battle = gm.Run.CurrentBattle;
            var layout = UIFactory.VGroup(root, 8, new RectOffset(20, 20, 14, 14));
            UIFactory.Stretch(layout);

            UIFactory.Label(layout, $"{gm.Run.Stage}스테이지 라운드 {battle.Round}", 22, TextAnchor.MiddleCenter, bold: true);
            TurnQueueRow(layout, battle);

            var domain = TargetDomain(gm, battle);

            var enemyArea = UIFactory.VGroup(layout, 4);
            UIFactory.FixedHeight(enemyArea, 130);
            foreach (var e in battle.Enemies) EnemyRow(enemyArea, gm, battle, e, domain);

            var logBox = UIFactory.ScrollList(layout, 140);
            foreach (var line in gm.BattleLog) UIFactory.Label(logBox, line, 15, TextAnchor.MiddleLeft, Theme.TextDim);
            UIFactory.ScrollToBottom(logBox); // 방금 재생된 줄이 항상 보이게

            var allyRow = UIFactory.HGroup(layout, 8);
            UIFactory.FixedHeight(allyRow, 110);
            foreach (var a in battle.Allies) AllyCard(allyRow, gm, battle, a, domain);

            if (battle.Outcome != BattleOutcome.Ongoing)
            {
                UIFactory.Label(layout, battle.Outcome == BattleOutcome.Victory ? "승리!" : "전멸…", 24, TextAnchor.MiddleCenter, bold: true);
                UIFactory.Button(layout, "확인", gm.FinishBattleAndContinue, bg: Theme.Accent, fontSize: 24);
                return;
            }

            if (gm.Animating) { UIFactory.Label(layout, "…", 20, TextAnchor.MiddleCenter, Theme.TextDim); return; }

            var actor = battle.CurrentActor;
            if (actor.IsEnemy) { UIFactory.Label(layout, $"{actor} 행동 중…", 18, TextAnchor.MiddleCenter, Theme.TextDim); return; }

            if (gm.PendingAction.HasValue) TargetPrompt(layout, gm);
            else ActionBar(layout, gm, battle, actor);
        }

        static SkillTarget? TargetDomain(GameManager gm, Battle battle)
        {
            if (gm.PendingAction is ActionType.Attack or ActionType.Purify) return SkillTarget.Enemy;
            if (gm.PendingAction == ActionType.Skill) return battle.EffectiveSkill(battle.CurrentActor)?.Target;
            return null;
        }

        static void TurnQueueRow(Transform parent, Battle battle)
        {
            var row = UIFactory.HGroup(parent, 6);
            UIFactory.FixedHeight(row, 34);
            foreach (var u in battle.TurnQueue.Where(x => x.Active))
            {
                bool cur = u == battle.CurrentActor;
                var chip = UIFactory.Panel(row, "Chip", cur ? Theme.Selected : (u.IsEnemy ? Theme.Danger : Theme.PanelLight), stretch: false);
                UIFactory.Label(chip, u.Name, 14, TextAnchor.MiddleCenter, cur ? Color.black : Theme.Text);
            }
        }

        static void EnemyRow(Transform parent, GameManager gm, Battle battle, Unit e, SkillTarget? domain)
        {
            string status = e.Fate == EnemyFate.Defeated ? " (처치됨)" : e.Fate == EnemyFate.Purified ? " (정화됨)" : e.Fainted ? " (기절)" : "";
            string purify = gm.PendingAction == ActionType.Purify && e.Active ? $"  정화 {battle.PurifyChance(battle.CurrentActor, e, gm.PendingRelic == RelicId.PurifyIncense):0}%" : "";
            string label = $"{e}  {e.Hp}/{e.MaxHp}{purify}{status}";

            if (domain == SkillTarget.Enemy && e.Active)
                UIFactory.Button(parent, label, () => gm.SubmitBattleAction(gm.PendingAction.Value, e), bg: Theme.Danger, fontSize: 18);
            else
            {
                var row = UIFactory.HGroup(parent, 6); UIFactory.FixedHeight(row, 34);
                UIFactory.Label(row, label, 18, TextAnchor.MiddleLeft, e.Active ? Theme.Text : Theme.TextDim);
                UIFactory.Bar(row, e.HpRatio, e.HpRatio > 0.3f ? Theme.HpBar : Theme.HpBarLow);
            }
        }

        static void AllyCard(Transform parent, GameManager gm, Battle battle, Unit a, SkillTarget? domain)
        {
            if (domain == SkillTarget.Ally && a.Active)
            {
                UIFactory.Button(parent, $"{a}\n{a.Hp}/{a.MaxHp}", () => gm.SubmitBattleAction(ActionType.Skill, a), bg: Theme.Selected, fontSize: 16);
                return;
            }
            var card = UIFactory.Panel(parent, "Ally", a == battle.CurrentActor ? Theme.PanelLight : Theme.Panel, stretch: false);
            var v = UIFactory.VGroup(card, 2, new RectOffset(6, 6, 4, 4));
            UIFactory.Stretch(v);
            string tag = a.Fainted ? "(기절)" : a.Defending ? "(방어)" : a.SustainOn ? "(유지)" : "";
            UIFactory.Label(v, $"{a.Name} {tag}", 15, TextAnchor.MiddleCenter);
            UIFactory.Bar(v, a.HpRatio, a.HpRatio > 0.3f ? Theme.HpBar : Theme.HpBarLow, $"{a.Hp}/{a.MaxHp}", 11);
            UIFactory.Bar(v, (float)a.Hunger / Balance.MaxHunger, Theme.HungerBar, $"배고픔 {a.Hunger}", 11);
        }

        static void TargetPrompt(Transform parent, GameManager gm)
        {
            var row = UIFactory.HGroup(parent, 10);
            UIFactory.FixedHeight(row, 44);
            UIFactory.Label(row, "대상을 선택하세요", 18, TextAnchor.MiddleCenter);
            UIFactory.Button(row, "취소", gm.CancelPending, fontSize: 16);
        }

        static void ActionBar(Transform parent, GameManager gm, Battle battle, Unit actor)
        {
            var relics = UIFactory.HGroup(parent, 8);
            UIFactory.FixedHeight(relics, 40);
            foreach (var r in ActiveRelics.Where(battle.CanUseRelic))
                UIFactory.Button(relics, RelicDb.Get(r).Name, () => gm.ToggleRelic(r),
                    bg: gm.PendingRelic == r ? Theme.Selected : Theme.PanelLight, fontSize: 15);

            var skill = actor.Species.Skill;
            var row = UIFactory.HGroup(parent, 10);
            UIFactory.FixedHeight(row, 54);
            UIFactory.Button(row, "기본 공격", () => gm.SetPendingAction(ActionType.Attack), fontSize: 20);
            UIFactory.Button(row, "정화", () => gm.SetPendingAction(ActionType.Purify), fontSize: 20);
            UIFactory.Button(row, "방어", () => gm.SetPendingAction(ActionType.Defend), fontSize: 20);

            if (skill.Kind == SkillKind.Sustain)
                UIFactory.Button(row, $"{skill.Name} {(actor.SustainOn ? "끄기" : "켜기")}", () => gm.ToggleSustain(actor),
                    bg: actor.SustainOn ? Theme.Selected : Theme.PanelLight, fontSize: 18);
            else if (skill.Kind != SkillKind.Passive)
            {
                bool ok = battle.CanUseSkill(actor, out var reason);
                string uses = skill.MaxUsesPerBattle > 0 ? $" ({actor.SkillUses}/{skill.MaxUsesPerBattle})" : "";
                UIFactory.Button(row, $"{skill.Name}({battle.SkillCost(actor)}){uses}", () => gm.SetPendingAction(ActionType.Skill), ok, fontSize: 18);
            }
        }
    }
}

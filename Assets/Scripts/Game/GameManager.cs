using System;
using System.Collections.Generic;
using System.Linq;
using FedAndFound.Core;
using UnityEngine;

namespace FedAndFound.Game
{
    /// <summary>Core(RunState/Battle)를 들고 있는 유일한 MonoBehaviour. UI는 이 클래스만 호출한다(CLAUDE.md).
    /// 상태를 바꾸는 메서드는 전부 끝에서 Refresh()를 불러 현재 화면을 다시 그리게 한다.</summary>
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public RunState Run { get; private set; }
        public readonly List<string> BattleLog = new List<string>();
        public ActionType? PendingAction { get; private set; }
        public RelicId? PendingRelic { get; private set; }
        public string LastError { get; private set; }

        Battle _lastBattleSeen;
        public event Action OnChanged;

        void Awake()
        {
            Instance = this;
            Refresh();
        }

        public void Refresh()
        {
            if (Run != null && Run.Phase == RunPhase.Battle && Run.CurrentBattle != _lastBattleSeen)
            {
                _lastBattleSeen = Run.CurrentBattle;
                BattleLog.Clear();
                PendingAction = null; PendingRelic = null;
            }
            OnChanged?.Invoke();
        }

        // ================= 종 선택 =================

        public void StartRun(IList<string> speciesIds, bool skipTutorial)
        {
            LastError = null;
            try { Run = new RunState(speciesIds, Environment.TickCount, skipTutorial); }
            catch (Exception e) { LastError = e.Message; }
            Refresh();
        }

        public void ResetToSpeciesSelect()
        {
            Run = null; _lastBattleSeen = null; BattleLog.Clear();
            PendingAction = null; PendingRelic = null; LastError = null;
            Refresh();
        }

        // ================= 유물 장착 =================

        public void EquipRelic(RelicId r) { Run.Equip(r); Refresh(); }
        public void UnequipRelic(RelicId r) { Run.Unequip(r); Refresh(); }
        public void ConfirmEquip() { Run.ConfirmEquip(); Refresh(); }

        // ================= 맵 =================

        public void EnterNode(NodeType n) { Run.EnterNode(n); Refresh(); }
        public void ChooseEvent(int i) { Run.ChooseEventOption(i); Refresh(); }
        public void CraftGem(Diet d) { Run.CraftGem(d); Refresh(); }

        // ================= 전투 =================

        void DrainLog()
        {
            foreach (var e in Run.CurrentBattle.DrainEvents())
            {
                BattleLog.Add(e.Text);
                if (BattleLog.Count > 30) BattleLog.RemoveAt(0);
            }
        }

        public void SetPendingAction(ActionType type)
        {
            if (type == ActionType.Defend) { SubmitBattleAction(ActionType.Defend, null); return; }
            if (type == ActionType.Skill)
            {
                var actor = Run.CurrentBattle.CurrentActor;
                var target = Run.CurrentBattle.EffectiveSkill(actor)?.Target ?? SkillTarget.None;
                if (target == SkillTarget.None) { SubmitBattleAction(ActionType.Skill, null); return; }
            }
            PendingAction = type; Refresh();
        }

        public void CancelPending() { PendingAction = null; PendingRelic = null; Refresh(); }

        public void ToggleRelic(RelicId r) { PendingRelic = PendingRelic == r ? (RelicId?)null : r; Refresh(); }

        public void SubmitBattleAction(ActionType type, Unit target)
        {
            var relic = PendingRelic;
            PendingAction = null; PendingRelic = null;
            try { Run.CurrentBattle.Submit(new BattleAction(type, target, relic)); }
            catch (Exception e) { BattleLog.Add($"[오류] {e.Message}"); }
            DrainLog();
            Refresh();
        }

        public void ToggleSustain(Unit u)
        {
            Run.CurrentBattle.SetSustain(u, !u.SustainOn);
            DrainLog();
            Refresh();
        }

        public void FinishBattleAndContinue()
        {
            Run.FinishBattle();
            BattleLog.Clear();
            Refresh();
        }

        // ================= 전투 후 =================

        public void Feed(Unit u, FoodType f) { Run.Feed(u, f); Refresh(); }
        public void Rest() { Run.Rest(); Refresh(); }
        public void Continue() { Run.Continue(); Refresh(); }

        // ================= 작별 =================

        public void Farewell(Unit u, bool devour) { Run.Farewell(u, devour); Refresh(); }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FedAndFound.Core;
using UnityEngine;

namespace FedAndFound.Game
{
    /// <summary>Core(RunState/Battle)를 들고 있는 유일한 MonoBehaviour. UI는 이 클래스만 호출한다(CLAUDE.md).
    /// 상태를 바꾸는 메서드는 전부 끝에서 Refresh()를 불러 현재 화면을 다시 그리게 한다.</summary>
    /// <summary>런 시작 전 화면 (Run == null일 때). 7단계 타이틀·인트로.</summary>
    public enum FrontScreen { Title, Intro, HowTo, SpeciesSelect }

    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public RunState Run { get; private set; }
        public FrontScreen Front { get; private set; } = FrontScreen.Title;
        public int IntroPage { get; private set; }
        /// <summary>데모 모드(발표 5분 시연용). 켜면 맵·전투 화면에 치트 패널이 나온다. 에디터/개발 빌드에서는 항상 켜짐.</summary>
        public bool DemoMode { get; private set; }
        public bool CheatsEnabled => DemoMode || Debug.isDebugBuild;
        public readonly List<string> BattleLog = new List<string>();
        public ActionType? PendingAction { get; private set; }
        public RelicId? PendingRelic { get; private set; }
        public string LastError { get; private set; }
        /// <summary>방금 고른 이벤트 선택지의 결과 문구. 맵 화면에 보여 주고, 다음 노드에 들어가면 지운다.</summary>
        public string LastEventResult { get; private set; }
        /// <summary>전투 이벤트를 한 줄씩 재생하는 동안 true. 이 동안은 화면에서 행동 입력을 잠근다(§3단계 연출).</summary>
        public bool Animating { get; private set; }
        float EventDelay => Meta.Settings.FastBattle ? 0.16f : 0.32f; // 전투 무대 연출(돌진·피격·숫자)이 보일 만큼, 설정에서 2배속

        Battle _lastBattleSeen;
        public event Action OnChanged;
        /// <summary>전투 이벤트가 한 줄씩 재생될 때마다 발생. 화면 전체를 다시 그리지 않는 연출(화면 플래시 등)이 구독한다.</summary>
        public event Action<BattleEvent> OnBattleEvent;
        /// <summary>업적을 처음 달성했을 때 (알림 토스트가 구독).</summary>
        public event Action<AchievementData> OnAchievement;
        /// <summary>이번 판에 새로 달성한 업적 (결과 화면에 표시).</summary>
        public readonly List<AchievementData> NewAchievements = new List<AchievementData>();

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
            CheckAchievements();
            OnChanged?.Invoke();
        }

        void CheckAchievements()
        {
            if (Run == null) return;
            foreach (var id in Achievements.Satisfied(Run))
                if (Meta.AchievementStore.Unlock(id))
                {
                    var a = Achievements.Get(id);
                    NewAchievements.Add(a);
                    OnAchievement?.Invoke(a);
                }
        }

        // ================= 타이틀 / 인트로 (7단계) =================

        public void ShowFront(FrontScreen s) { Front = s; IntroPage = 0; Refresh(); }
        public void NextIntroPage(int pageCount)
        {
            if (++IntroPage >= pageCount) { ShowFront(FrontScreen.SpeciesSelect); return; }
            Refresh();
        }
        public void ToggleDemoMode() { DemoMode = !DemoMode; Refresh(); }
        public void QuitGame()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        // ================= 종 선택 =================

        public void StartRun(IList<string> speciesIds, bool skipTutorial)
        {
            LastError = null;
            NewAchievements.Clear();
            try { Run = new RunState(speciesIds, Environment.TickCount, skipTutorial); }
            catch (Exception e) { LastError = e.Message; }
            Refresh();
        }

        public void ResetToTitle() { ResetToSpeciesSelect(); ShowFront(FrontScreen.Title); }

        public void ResetToSpeciesSelect()
        {
            Run = null; _lastBattleSeen = null; BattleLog.Clear(); LastEventResult = null;
            Front = FrontScreen.SpeciesSelect;
            PendingAction = null; PendingRelic = null; LastError = null;
            Refresh();
        }

        // ================= 유물 장착 =================

        public void EquipRelic(RelicId r) { Run.Equip(r); Refresh(); }
        public void UnequipRelic(RelicId r) { Run.Unequip(r); Refresh(); }
        public void ConfirmEquip() { Run.ConfirmEquip(); Refresh(); }

        // ================= 맵 =================

        public void EnterNode(NodeType n) { LastEventResult = null; Run.EnterNode(n); Refresh(); }
        public void ChooseEvent(int i) { LastEventResult = Run.ChooseEventOption(i); Refresh(); }

        // ================= 테스트용 (에디터/개발 빌드에서만 맵 화면에 노출) =================

        /// <summary>다음 이벤트 노드에서 i번 이벤트가 나오게 예약. 실제 진입은 평소처럼 노드로 걸어가서 한다.</summary>
        public void DebugForceEvent(int i) { Run.ForcedEventIndex = i; Refresh(); }
        public void DebugToggleUnlimitedEvents() { Run.DebugUnlimitedEvents = !Run.DebugUnlimitedEvents; Refresh(); }

        // 데모 모드 치트 (7단계) — 전부 Core의 Debug* 메서드를 호출만 한다
        public void DemoAddFragments() { Run.DebugAddFragments(3); Refresh(); }
        public void DemoAddFood() { Run.DebugAddFood(5); Refresh(); }
        public void DemoHealAll() { Run.DebugHealAll(); Refresh(); }
        public void DemoSkipToBoss() { Run.DebugSkipToBoss(); Refresh(); }
        public void DemoJumpToStage(int stage) { LastEventResult = null; Run.DebugJumpToStage(stage); Refresh(); }
        public void DemoWeakenEnemies() { if (Animating) return; Run.CurrentBattle.DebugWeakenEnemies(); StartCoroutine(PlayEvents(Run.CurrentBattle.DrainEvents())); }
        public void DemoGoSolo()
        {
            if (Animating) return;
            var b = Run.CurrentBattle;
            b.DebugKoAlliesExcept(b.CurrentActor ?? b.Allies.First(a => a.Active));
            StartCoroutine(PlayEvents(b.DrainEvents()));
        }
        public void DemoWinNow() { if (Animating) return; Run.CurrentBattle.DebugWinNow(); StartCoroutine(PlayEvents(Run.CurrentBattle.DrainEvents())); }
        public void CraftGem(Diet d) { if (Run.CraftGem(d)) Audio.SoundManager.Play(Audio.Sfx.Gem); Refresh(); }

        // ================= 전투 =================

        public void SetPendingAction(ActionType type)
        {
            if (Animating) return;
            if (type == ActionType.Defend) { SubmitBattleAction(ActionType.Defend, null); return; }
            if (type == ActionType.Skill)
            {
                var actor = Run.CurrentBattle.CurrentActor;
                var target = Run.CurrentBattle.EffectiveSkill(actor)?.Target ?? SkillTarget.None;
                if (target == SkillTarget.None) { SubmitBattleAction(ActionType.Skill, null); return; }
            }
            PendingAction = type; Refresh();
        }

        public void CancelPending() { if (Animating) return; PendingAction = null; PendingRelic = null; Refresh(); }

        public void ToggleRelic(RelicId r) { if (Animating) return; PendingRelic = PendingRelic == r ? (RelicId?)null : r; Refresh(); }

        public void SubmitBattleAction(ActionType type, Unit target)
        {
            if (Animating) return;
            var relic = PendingRelic;
            PendingAction = null; PendingRelic = null;
            List<BattleEvent> events;
            try { Run.CurrentBattle.Submit(new BattleAction(type, target, relic)); events = Run.CurrentBattle.DrainEvents(); }
            catch (Exception e) { BattleLog.Add($"[오류] {e.Message}"); Refresh(); return; }
            StartCoroutine(PlayEvents(events));
        }

        public void ToggleSustain(Unit u)
        {
            if (Animating) return;
            Run.CurrentBattle.SetSustain(u, !u.SustainOn);
            StartCoroutine(PlayEvents(Run.CurrentBattle.DrainEvents()));
        }

        /// <summary>드레인된 전투 이벤트를 한 줄씩 로그에 얹으며 짧게 멈춘다 — 3단계 "이벤트 재생 큐".
        /// 재생 중에는 Animating이 true라 BattleScreen이 행동 버튼을 그리지 않는다.</summary>
        IEnumerator PlayEvents(List<BattleEvent> events)
        {
            Animating = true;
            foreach (var e in events)
            {
                BattleLog.Add(e.Text);
                if (BattleLog.Count > 30) BattleLog.RemoveAt(0);
                OnBattleEvent?.Invoke(e);
                Refresh();
                yield return new WaitForSeconds(EventDelay);
            }
            Animating = false;
            Refresh();
        }

        public void FinishBattleAndContinue()
        {
            if (Animating) return;
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

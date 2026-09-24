using System;
using System.Collections.Generic;
using System.Linq;

namespace FedAndFound.Core
{
    public enum BattleOutcome { Ongoing, Victory, Defeat }
    public enum ActionType { Attack, Defend, Purify, Skill }

    public sealed class BattleAction
    {
        public ActionType Type;
        public Unit Target;          // Attack/Purify/적 대상 스킬 → 적, 아군 대상 스킬 → 아군
        public RelicId? UseRelic;    // 액티브 유물 (§10.1, FR-3)
        public BattleAction(ActionType type, Unit target = null, RelicId? relic = null) { Type = type; Target = target; UseRelic = relic; }
    }

    public enum BattleEventType { RoundStart, Damage, Miss, Heal, Faint, Defeated, Purified, PurifyFailed, Skill, Sustain, Status, Relic, End }

    /// <summary>UI는 이 이벤트 목록을 순서대로 재생(애니메이션/로그)하면 된다.</summary>
    public sealed class BattleEvent
    {
        public BattleEventType Type; public Unit Actor, Target; public int Value; public string Text;
        public override string ToString() => Text;
    }

    public sealed class BattleRewards
    {
        public int Meat, Fruit, MobsDefeated;
        public readonly List<Diet?> Fragments = new List<Diet?>(); // null = 만능 조각
        public bool BossDefeated, BossPurified;
    }

    /// <summary>스테이지 동안 누적되는 유물 버프(피 묻은 부적, 맑은 수정). RunState가 소유한다.</summary>
    public sealed class StageBuffs { public float Atk, Purify; }

    public sealed class BattleContext
    {
        public HashSet<RelicId> Relics = new HashSet<RelicId>(); // 이미 지형 판정이 끝난, 이번 전투에서 유효한 유물
        public Terrain Terrain;
        public IRng Rng;
        public StageBuffs StageBuffs = new StageBuffs();
        public readonly List<RelicId> ConsumedRelics = new List<RelicId>(); // 런이 인벤토리에서 제거해야 할 소모형
        public HashSet<Diet> SynergyDiets = new HashSet<Diet>(); // 원석으로 해금된 시너지 (§4.5, Data/Synergies.cs)
        public bool Has(RelicId r) => Relics.Contains(r);
        public bool HasSynergy(Diet d) => SynergyDiets.Contains(d);
    }

    /// <summary>
    /// 사이드뷰 턴제 전투 엔진 (UnityEngine 비의존).
    /// 사용법: new Battle(...) → CurrentActor가 아군이면 UI에서 행동을 골라 Submit → DrainEvents로 연출 재생 → 반복.
    /// 적 턴은 Submit 내부에서 다음 아군 차례까지 자동 진행된다.
    /// </summary>
    public sealed class Battle
    {
        public readonly List<Unit> Allies, Enemies;
        public readonly BattleContext Ctx;
        public readonly BattleRewards Rewards = new BattleRewards();
        public BattleOutcome Outcome { get; private set; }
        public int Round { get; private set; }
        public IReadOnlyList<Unit> TurnQueue => _queue;
        public Unit CurrentActor => Outcome == BattleOutcome.Ongoing && _turn < _queue.Count ? _queue[_turn] : null;

        readonly List<Unit> _queue = new List<Unit>();
        readonly List<BattleEvent> _events = new List<BattleEvent>();
        readonly HashSet<RelicId> _usedRelics = new HashSet<RelicId>();
        int _turn;
        float _roundPurifyBonus, _bellBonus;
        SkillData _lastAllyActive;
        IRng Rng => Ctx.Rng;

        public Battle(List<Unit> allies, List<Unit> enemies, BattleContext ctx)
        {
            Allies = allies; Enemies = enemies; Ctx = ctx;
            foreach (var u in allies.Concat(enemies)) u.ResetBattleState();
            StartRound();
            AdvanceToPlayer();
        }

        public List<BattleEvent> DrainEvents() { var e = new List<BattleEvent>(_events); _events.Clear(); return e; }

        // ================= 공개 조회 API (UI용) =================

        public float CurseGauge(Unit enemy) =>
            Balance.CurseGaugeMax * (1f - enemy.HpRatio) * (enemy.IsBoss ? Balance.BossCurseMult : 1f);

        /// <summary>§7: 정화 확률 = 저주 게이지 + 시전자 정화 효율 (+ 보너스). 0~100 (%).</summary>
        public float PurifyChance(Unit actor, Unit target, bool withIncense = false)
        {
            float c = CurseGauge(target) + Purify(actor) + _roundPurifyBonus + (withIncense ? Balance.IncensePurifyBonus : 0)
                + SynergyPurify(actor, target);
            return Math.Max(0, Math.Min(Balance.PurifyChanceCap, c));
        }

        /// <summary>홀로서기: 전투에서 살아 있는 아군이 자기뿐인가 (6단계).</summary>
        public bool IsSolo(Unit u) => !u.IsEnemy && u.Active && Allies.Count(x => x.Active) == 1;

        /// <summary>지금 이 동물의 스킬(홀로서기면 변형 스킬). UI 표시와 규칙 판정 모두 이걸 쓴다.</summary>
        public SkillData CurrentSkill(Unit u) => IsSolo(u) && u.Species.SoloSkill != null ? u.Species.SoloSkill : u.Species.Skill;

        public int SkillCost(Unit u)
        {
            int c = CurrentSkill(u).Cost;
            return Ctx.Has(RelicId.ThriftCharm) ? (int)Math.Ceiling(c * Balance.ThriftMult) : c;
        }

        /// <summary>원숭이는 직전 아군 액티브 스킬로 대체된다.</summary>
        public SkillData EffectiveSkill(Unit u)
        {
            var s = CurrentSkill(u);
            return s.Id == SkillId.Mimic ? _lastAllyActive : s;
        }

        public bool CanUseSkill(Unit u, out string reason)
        {
            var s = CurrentSkill(u); reason = null;
            if (Ctx.Has(RelicId.SealedClaw)) reason = "봉인된 발톱: 스킬 사용 불가";
            else if (s.Kind == SkillKind.Passive) reason = "패시브 스킬";
            else if (s.Kind == SkillKind.Sustain) reason = "유지형은 SetSustain으로 켜고 끈다";
            else if (u.Hunger < SkillCost(u)) reason = "배고픔 부족";
            else if (s.MaxUsesPerBattle > 0 && u.SkillUses >= s.MaxUsesPerBattle) reason = "이번 전투 사용 횟수 소진";
            else if (s.Id == SkillId.Mimic && _lastAllyActive == null) reason = "복사할 스킬이 없음";
            else if ((s.Id == SkillId.Ambush || s.Id == SkillId.LoneHunt) && u.ChargedReady) reason = "이미 매복 중";
            else if (s.Id == SkillId.Trick && u.ExtraTurnRound == Round) reason = "이번 라운드에 이미 사용";
            return reason == null;
        }

        public bool CanUseRelic(RelicId r) =>
            Ctx.Has(r) && RelicDb.Get(r).Kind == RelicKind.Active && !_usedRelics.Contains(r);

        /// <summary>적응(잡식 원석) 시너지의 현재 형태. 원석이 없으면 None. UI 표시용으로도 쓴다.</summary>
        public Synergies.AdaptMode CurrentAdapt() =>
            Ctx.HasSynergy(Diet.Omnivore) ? Synergies.Adapt(Allies.Where(x => x.Active)) : Synergies.AdaptMode.None;

        /// <summary>FR-1: 선제형 판정은 적 포함 전체 유닛 기준.</summary>
        public bool IsFirstActorThisRound(Unit u) => _queue.FirstOrDefault(x => x.Active) == u;

        /// <summary>유지형 스킬 토글. 행동을 소모하지 않는다. 이번 라운드 미지불이면 켜는 즉시 지불.</summary>
        public bool SetSustain(Unit u, bool on)
        {
            if (u.IsEnemy || u.Species.Skill.Kind != SkillKind.Sustain || Ctx.Has(RelicId.SealedClaw) || !u.Active) return false;
            if (on == u.SustainOn) return true;
            if (on && !u.SustainPaidThisRound)
            {
                if (u.Hunger < SustainCost()) return false;
                u.AddHunger(-SustainCost()); u.SustainPaidThisRound = true;
            }
            u.SustainOn = on;
            Log(BattleEventType.Sustain, u, null, 0, $"{u} {u.Species.Skill.Name} {(on ? "ON" : "OFF")}");
            return true;
        }

        // ================= 행동 =================

        public void Submit(BattleAction a)
        {
            var u = CurrentActor;
            if (u == null || u.IsEnemy) throw new InvalidOperationException("아군 차례가 아니다");
            Validate(u, a);

            if (a.UseRelic is RelicId r)
            {
                _usedRelics.Add(r);
                Log(BattleEventType.Relic, u, null, 0, $"{u}: {RelicDb.Get(r).Name} 사용");
                if (r == RelicId.GuardShell) u.GuardShellArmed = true;
            }

            switch (a.Type)
            {
                case ActionType.Attack: DoAttack(u, a.Target, a.UseRelic == RelicId.BeastClaw ? Balance.BeastClawMult : 1f); break;
                case ActionType.Defend: u.Defending = true; Log(BattleEventType.Status, u, null, 0, $"{u} 방어 태세"); break;
                case ActionType.Purify: DoPurify(u, a.Target, a.UseRelic == RelicId.PurifyIncense); break;
                case ActionType.Skill: DoSkill(u, a.Target); break;
            }
            EndTurn();
            AdvanceToPlayer();
        }

        void Validate(Unit u, BattleAction a)
        {
            if (a.UseRelic is RelicId r)
            {
                if (!CanUseRelic(r)) throw new InvalidOperationException($"유물 사용 불가: {r}");
                if (r == RelicId.BeastClaw && a.Type != ActionType.Attack) throw new InvalidOperationException("맹수의 발톱은 기본 공격에만");
                if (r == RelicId.PurifyIncense && a.Type != ActionType.Purify) throw new InvalidOperationException("정화의 향로는 정화에만");
            }
            bool needEnemy = a.Type == ActionType.Attack || a.Type == ActionType.Purify
                || (a.Type == ActionType.Skill && EffectiveSkill(u)?.Target == SkillTarget.Enemy);
            bool needAlly = a.Type == ActionType.Skill && EffectiveSkill(u)?.Target == SkillTarget.Ally;
            if (a.Type == ActionType.Skill && !CanUseSkill(u, out var why)) throw new InvalidOperationException(why);
            if (needEnemy && (a.Target == null || !a.Target.IsEnemy || !a.Target.Active)) throw new InvalidOperationException("유효한 적 대상 필요");
            if (needAlly && (a.Target == null || a.Target.IsEnemy || !a.Target.Active)) throw new InvalidOperationException("유효한 아군 대상 필요");
        }

        void DoAttack(Unit u, Unit target, float mult)
        {
            // 사자 광폭: 배고픔이 낮을수록 오사 확률 (§5.2)
            if (u.Species.Skill.Id == SkillId.Frenzy && !u.IsEnemy && Rng.Chance(0.2f * HungerDeficit(u)))
            {
                var others = Allies.Where(x => x != u && x.Active).ToList();
                if (others.Count > 0)
                {
                    target = Rng.Pick(others);
                    Log(BattleEventType.Status, u, target, 0, $"{u}이(가) 광폭해져 {target}을(를) 공격했다!");
                }
            }
            DealDamage(u, target, 1f, false, mult);
        }

        void DoPurify(Unit u, Unit target, bool incense)
        {
            float chance = PurifyChance(u, target, incense) + (Ctx.Has(RelicId.PurifyBell) ? _bellBonus : 0);
            chance = Math.Min(Balance.PurifyChanceCap, chance);
            // TODO(§20-11): Balance.PurifyFruitCost > 0이면 여기서 열매 소비 (RunState 연동 필요)
            if (Rng.Value() * 100f < chance)
            {
                _bellBonus = 0;
                RemoveEnemy(target, EnemyFate.Purified, $"{u}이(가) {target}의 저주를 풀었다! ({chance:0}%)");
                if (Ctx.HasSynergy(Diet.Herbivore) && u.Species.Diet == Diet.Herbivore) // 생명의 순환
                {
                    foreach (var a in Allies.Where(x => x.Active)) a.AddHunger(Balance.CycleHungerRestore);
                    Log(BattleEventType.Heal, u, null, Balance.CycleHungerRestore, $"생명의 순환: 아군 전체 배고픔 +{Balance.CycleHungerRestore}");
                }
            }
            else
            {
                if (Ctx.Has(RelicId.PurifyBell)) _bellBonus = Math.Min(Balance.BellPurifyCap, _bellBonus + Balance.BellPurifyStep);
                Log(BattleEventType.PurifyFailed, u, target, 0, $"{u}의 정화 실패 ({chance:0}%)");
            }
        }

        void DoSkill(Unit u, Unit target)
        {
            var own = CurrentSkill(u);
            var s = EffectiveSkill(u);
            u.AddHunger(-SkillCost(u));
            u.SkillUses++;
            Log(BattleEventType.Skill, u, target, 0, own.Id == SkillId.Mimic ? $"{u} 모방 → {s.Name}" : $"{u} {s.Name}");
            ExecuteSkill(u, s.Id, target);
            if (own.Kind == SkillKind.Active && own.Id != SkillId.Mimic && own == u.Species.Skill) _lastAllyActive = own; // 홀로서기 스킬은 모방 대상 아님
        }

        void ExecuteSkill(Unit u, SkillId id, Unit target)
        {
            switch (id)
            {
                case SkillId.Soothe: _roundPurifyBonus += 8; break;
                case SkillId.Swerve: u.Evade = 0.4f; break;
                case SkillId.Charge: DealDamage(u, target, 2.2f, true); break;
                case SkillId.Ambush: u.ChargedReady = true; u.ChargeMult = 2.6f; break; // 이번 턴은 쉼, 다음 첫 공격 ×2.6
                case SkillId.VenomBite:
                    DealDamage(u, target, 1.0f, false);
                    TryPoison(u, target);
                    break;

                // ----- 홀로서기 -----
                case SkillId.Burrow:
                    // 굴에 숨었다가 튀어나오며 뒷발차기
                    u.Defending = true; u.Evade = 0.4f; HealPct(u, u, 0.15f);
                    u.ChargedReady = true; u.ChargeMult = 2.0f;
                    break;
                case SkillId.Pronk:
                    foreach (var e in Enemies.Where(x => x.Active)) e.AtkDownTurns = 3;
                    u.Evade = 0.6f;
                    u.ChargedReady = true; u.ChargeMult = 1.8f; // 착지하며 뒷발로 걷어차기
                    Log(BattleEventType.Status, u, null, 0, "적들이 쫓기를 망설인다 (적 공격 -30%, 3회)");
                    break;
                case SkillId.HideCharge: DealDamage(u, target, 1.6f, true); u.DefUpTurns = 1; break;
                case SkillId.LoneHunt: u.ChargedReady = true; u.ChargeMult = 3.0f; break;
                case SkillId.Shedding:
                    // 새 비늘로 몸을 바꾸고, 다음 독 물기/공격을 노린다
                    HealPct(u, u, 0.3f);
                    u.PoisonTurns = 0; u.DefDownTurns = 0; u.AtkDownTurns = 0;
                    u.ChargedReady = true; u.ChargeMult = 1.8f;
                    break;
                case SkillId.Trick:
                    // 이 스킬에 쓴 차례까지 돌려받아야 의미가 있으므로 2번 더 움직인다 (순이득 +1 행동)
                    u.ExtraTurnRound = Round;
                    _queue.Insert(_turn + 1, u); _queue.Insert(_turn + 1, u);
                    Log(BattleEventType.Status, u, null, 0, $"{u}이(가) 재빠르게 두 번 더 움직인다!");
                    break;
                case SkillId.StoneThrow:
                    DealDamage(u, target, 1.8f, false);
                    if (target.Active) target.DefDownTurns = Math.Max(target.DefDownTurns, 3);
                    break;
                case SkillId.MudBath:
                    HealPct(u, u, 0.15f); u.DefUpTurns = 2; u.DefDownTurns = 0;
                    break;
                case SkillId.Wits: MoveUp(target, 2); break;
                case SkillId.Rampage: DealDamage(u, target, 1.8f, false); u.DefDownTurns = 1; break;
                case SkillId.Graze: target.Heal((int)Math.Round(target.MaxHp * 0.2f)); Log(BattleEventType.Heal, u, target, 0, $"{target} 회복"); break;
            }
        }

        void HealPct(Unit src, Unit dst, float pct)
        {
            int amt = Math.Max(1, (int)Math.Round(dst.MaxHp * pct));
            dst.Heal(amt);
            Log(BattleEventType.Heal, src, dst, amt, $"{dst} HP {amt} 회복");
        }

        /// <summary>독 물기 공통. 혼자 남은 벌꿀오소리는 뱀독에 면역.</summary>
        void TryPoison(Unit src, Unit target)
        {
            if (!target.Active) return;
            if (IsSolo(target) && target.Species.Skill.Id == SkillId.Tenacity)
            {
                Log(BattleEventType.Status, src, target, 0, $"{target}에게는 독이 통하지 않는다!");
                return;
            }
            target.PoisonTurns = 3; target.PoisonDmg = Math.Max(1, (int)Math.Round(Atk(src) * 0.25f));
        }

        /// <summary>여우 눈치: 이번 라운드에 아직 행동 전이면 즉시 2칸 앞당기고, 이미 행동했으면 다음 라운드에 적용.</summary>
        void MoveUp(Unit target, int n)
        {
            int idx = _queue.IndexOf(target);
            if (idx > _turn)
            {
                int to = Math.Max(_turn + 1, idx - n);
                _queue.RemoveAt(idx); _queue.Insert(to, target);
            }
            else target.NextRoundBoost += n;
        }

        // ================= 턴 진행 =================

        void StartRound()
        {
            Round++;
            _roundPurifyBonus = 0;
            _turn = 0;
            _queue.Clear();
            _queue.AddRange(Allies.Concat(Enemies).Where(x => x.Active)
                .OrderBy(OrderKey).ThenBy(x => x.IsEnemy ? 0 : 1)); // 동률이면 적 우선 (§4.4)
            foreach (var u in _queue.Where(x => x.NextRoundBoost > 0).ToList())
            {
                int idx = _queue.IndexOf(u), to = Math.Max(0, idx - u.NextRoundBoost);
                _queue.RemoveAt(idx); _queue.Insert(to, u); u.NextRoundBoost = 0;
            }
            foreach (var a in Allies) a.SustainPaidThisRound = false;
            Log(BattleEventType.RoundStart, null, null, Round, $"— 라운드 {Round} —");
        }

        float OrderKey(Unit u) => u.Species.SpeedRank - (!u.IsEnemy && Ctx.Has(RelicId.FennecEar) ? Balance.FennecRankBonus : 0);

        /// <summary>적 턴을 자동 처리하며 다음 아군 입력 대기 또는 전투 종료까지 진행.</summary>
        void AdvanceToPlayer()
        {
            while (Outcome == BattleOutcome.Ongoing)
            {
                if (_turn >= _queue.Count) { EndRound(); if (Outcome != BattleOutcome.Ongoing) return; StartRound(); continue; }
                var u = _queue[_turn];
                if (!u.Active) { _turn++; continue; }
                BeginTurn(u);
                if (!u.Active || Outcome != BattleOutcome.Ongoing) { EndTurn(); continue; }
                if (!u.IsEnemy) return; // 플레이어 입력 대기
                EnemyAct(u);
                EndTurn();
            }
        }

        void BeginTurn(Unit u)
        {
            u.Defending = false; u.Evade = 0;
            if (u.DefDownTurns > 0) u.DefDownTurns--;
            if (u.DefUpTurns > 0) u.DefUpTurns--;
            if (u.SustainOn && u.Species.Skill.Id == SkillId.Shell && IsSolo(u)) HealPct(u, u, 0.03f); // 깊이 웅크리기
            if (!u.IsEnemy && u.SustainOn && !u.SustainPaidThisRound)
            {
                // FR-2: 배고픔 부족 시 자동 해제
                if (u.Hunger >= SustainCost()) { u.AddHunger(-SustainCost()); u.SustainPaidThisRound = true; }
                else { u.SustainOn = false; Log(BattleEventType.Sustain, u, null, 0, $"{u} 배고픔 부족 — {u.Species.Skill.Name} 자동 해제"); }
            }
            if (u.PoisonTurns > 0)
            {
                u.PoisonTurns--;
                Log(BattleEventType.Status, u, null, u.PoisonDmg, $"{u} 독 피해 {u.PoisonDmg}");
                ApplyDamage(u, u.PoisonDmg);
            }
        }

        void EndTurn()
        {
            // 공격↓(프롱킹)은 "그 유닛이 행동한 횟수"로 센다
            if (_turn < _queue.Count && _queue[_turn].AtkDownTurns > 0) _queue[_turn].AtkDownTurns--;
            _turn++; CheckOutcome();
        }

        void EndRound()
        {
            if (Ctx.Has(RelicId.SwampMoss))
                foreach (var a in Allies.Where(x => x.Active)) a.Heal(Math.Max(1, (int)(a.MaxHp * Balance.SwampHealRatio)));
        }

        int SustainCost() => Ctx.Has(RelicId.ThriftCharm) ? (int)Math.Ceiling(Balance.SustainCostPerTurn * Balance.ThriftMult) : Balance.SustainCostPerTurn;

        // ================= 적 AI (5단계) =================

        /// <summary>적 행동: 유지형은 HP가 줄면 켠다(행동 소모 X) → 확률로 종별 스킬 → 아니면 기본 공격(보스는 가끔 강타).
        /// 적은 배고픔이 없으므로 코스트 대신 확률(Balance.EnemySkillChance)로 제한한다.</summary>
        void EnemyAct(Unit e)
        {
            var targets = Allies.Where(x => x.Active).ToList();
            if (targets.Count == 0) return;
            var skill = e.Species.Skill;

            if (skill.Kind == SkillKind.Sustain && !e.SustainOn && e.HpRatio < Balance.EnemySustainHpThreshold)
            {
                e.SustainOn = true;
                Log(BattleEventType.Sustain, e, null, 0, $"{e} {skill.Name} ON");
            }

            // 매복을 걸어 둔 상태면 이번 턴은 무조건 공격(×2.6이 실린다)
            if (!e.ChargedReady && Rng.Chance(e.IsBoss ? Balance.BossSkillChance : Balance.EnemySkillChance) && TryEnemySkill(e, skill.Id, targets))
                return;

            var target = EnemyPickTarget(targets);
            float coef = e.IsBoss && Rng.Chance(Balance.BossSmashChance) ? Balance.BossSmashCoef : 1f;
            if (coef > 1f) Log(BattleEventType.Skill, e, target, 0, $"{e} 강타!");
            DealDamage(e, target, coef, false);
        }

        Unit EnemyPickTarget(List<Unit> targets)
        {
            // 오소리 악바리: 40% 유도
            var taunter = targets.FirstOrDefault(x => x.SustainOn && x.Species.Skill.Id == SkillId.Tenacity);
            return taunter != null && Rng.Chance(0.4f) ? taunter : Rng.Pick(targets);
        }

        /// <summary>적이 쓸 수 있는 스킬이면 실행하고 true. 쓸 상황이 아니면 false(→ 기본 공격).
        /// 효과는 아군 버전과 최대한 같게 하되, 적에게 의미 없는 스킬은 적 전용으로 바꿨다(달래기 = 적 회복).</summary>
        bool TryEnemySkill(Unit e, SkillId id, List<Unit> targets)
        {
            switch (id)
            {
                case SkillId.Soothe:
                {
                    var hurt = Enemies.Where(x => x.Active && x.HpRatio < 0.8f).OrderBy(x => x.HpRatio).FirstOrDefault();
                    if (hurt == null) return false;
                    int amt = Math.Max(1, (int)Math.Round(hurt.MaxHp * Balance.EnemySootheHealRatio));
                    hurt.Heal(amt);
                    Log(BattleEventType.Heal, e, hurt, amt, $"{e} 달래기 → {hurt} HP {amt} 회복");
                    return true;
                }
                case SkillId.Swerve:
                    e.Evade = 0.4f; // BeginTurn에서 0으로 돌아가므로 자기 다음 차례까지 유지
                    Log(BattleEventType.Skill, e, null, 0, $"{e} 급선회 — 다음 차례까지 회피 40%");
                    return true;
                case SkillId.Charge:
                {
                    var t = EnemyPickTarget(targets);
                    Log(BattleEventType.Skill, e, t, 0, $"{e} 돌진!");
                    DealDamage(e, t, 2.2f, true);
                    return true;
                }
                case SkillId.Ambush:
                    if (e.SkillUses >= e.Species.Skill.MaxUsesPerBattle) return false;
                    e.SkillUses++; e.ChargedReady = true; e.ChargeMult = 2.6f;
                    Log(BattleEventType.Skill, e, null, 0, $"{e}이(가) 몸을 낮추고 노린다… (다음 공격 ×2.6, 방어 추천)");
                    return true;
                case SkillId.VenomBite:
                {
                    var t = EnemyPickTarget(targets);
                    if (t.PoisonTurns > 0) return false;
                    Log(BattleEventType.Skill, e, t, 0, $"{e} 독 물기!");
                    DealDamage(e, t, 1.0f, false);
                    TryPoison(e, t);
                    return true;
                }
                case SkillId.Wits:
                {
                    int me = _queue.IndexOf(e);
                    var buddy = _queue.Skip(me + 1).FirstOrDefault(x => x.IsEnemy && x.Active && x != e);
                    if (buddy == null) return false;
                    MoveUp(buddy, 2);
                    Log(BattleEventType.Skill, e, buddy, 0, $"{e} 눈치 → {buddy}의 차례를 앞당겼다");
                    return true;
                }
                case SkillId.Mimic:
                {
                    // 원숭이는 파티가 마지막으로 쓴 액티브 스킬을 흉내 낸다
                    var copy = _lastAllyActive;
                    if (copy == null || copy.Id == SkillId.Mimic) return false;
                    Log(BattleEventType.Skill, e, null, 0, $"{e} 모방 → {copy.Name}");
                    if (TryEnemySkill(e, copy.Id, targets)) return true;
                    Log(BattleEventType.Status, e, null, 0, "…하지만 흉내에 실패했다");
                    return true;
                }
                case SkillId.Rampage:
                {
                    var t = EnemyPickTarget(targets);
                    Log(BattleEventType.Skill, e, t, 0, $"{e} 저돌!");
                    DealDamage(e, t, 1.8f, false);
                    e.DefDownTurns = 1;
                    return true;
                }
                case SkillId.Graze:
                {
                    var hurt = Enemies.Where(x => x.Active && x.HpRatio < 0.8f).OrderBy(x => x.HpRatio).FirstOrDefault();
                    if (hurt == null) return false;
                    int amt = (int)Math.Round(hurt.MaxHp * 0.2f);
                    hurt.Heal(amt);
                    Log(BattleEventType.Heal, e, hurt, amt, $"{e} 풀 뜯기 → {hurt} HP {amt} 회복");
                    return true;
                }
                default: return false; // 유지형(위에서 처리)·패시브(광폭은 Atk에서 처리)
            }
        }

        // ================= 피해 & 스탯 =================

        void DealDamage(Unit src, Unit dst, float coef, bool ignoreDef, float extraMult = 1f)
        {
            if (dst.Evade > 0 && Rng.Chance(dst.Evade)) { Log(BattleEventType.Miss, src, dst, 0, $"{dst}이(가) 피했다!"); return; }
            float atk = Atk(src) * coef * extraMult;
            // 적응(잡식뿐): 적 HP 50% 이상이면 공격 보너스
            if (!src.IsEnemy && dst.IsEnemy && dst.HpRatio >= 0.5f && CurrentAdapt() == Synergies.AdaptMode.OmniOnly)
                atk *= 1 + Balance.AdaptSoloAtk;
            if (src.ChargedReady) { atk *= src.ChargeMult; src.ChargedReady = false; }
            float def = ignoreDef ? 0 : Def(dst);
            float dmg = atk * Balance.DefenseK / (Balance.DefenseK + def);
            dmg *= 1f + (Rng.Value() * 2f - 1f) * Balance.DamageVariance;
            if (dst.Defending) dmg *= Balance.DefendDamageMult;
            bool solo = IsSolo(dst);
            if (dst.SustainOn && dst.Species.Skill.Id == SkillId.Shell) dmg *= solo ? 0.5f : 0.68f;
            if (dst.SustainOn && dst.Species.Skill.Id == SkillId.Tenacity) dmg *= solo ? 0.75f : 0.85f;
            if (dst.SustainOn && dst.Species.Skill.Id == SkillId.Spines && solo) dmg *= 0.8f;
            if (dst.GuardShellArmed && src.IsEnemy) { dmg *= Balance.GuardShellMult; dst.GuardShellArmed = false; }
            int d = Math.Max(1, (int)Math.Round(dmg));
            Log(BattleEventType.Damage, src, dst, d, $"{src} → {dst} {d} 피해");
            ApplyDamage(dst, d);
            if (solo && dst.Active && dst.SustainOn && dst.Species.Skill.Id == SkillId.Tenacity && dst.RageStacks < 6) dst.RageStacks++; // 벌꿀오소리의 배짱

            // 쓰러진 고슴도치는 반격하지 않는다 (마지막 적과 마지막 아군이 동시에 쓰러지는 버그 방지)
            if (dst.SustainOn && dst.Species.Skill.Id == SkillId.Spines && dst.Active && src != dst && src.Active)
            {
                int r = Math.Max(1, (int)Math.Round(d * (solo ? 0.5f : 0.3f)));
                Log(BattleEventType.Damage, dst, src, r, $"가시 반격 {r}");
                ApplyDamage(src, r);
            }
        }

        void ApplyDamage(Unit dst, int d)
        {
            if (!dst.Active) return;
            if (!dst.IsEnemy && d >= dst.Hp && Ctx.Has(RelicId.LastEmber) && Allies.Count(x => x.Active) == 1)
            {
                dst.Hp = 1; Ctx.Relics.Remove(RelicId.LastEmber); Ctx.ConsumedRelics.Add(RelicId.LastEmber);
                Log(BattleEventType.Relic, dst, null, 0, $"마지막 불씨! {dst}이(가) 버텼다");
                return;
            }
            dst.Hp = Math.Max(0, dst.Hp - d);
            if (dst.Hp > 0) return;
            if (dst.IsEnemy) RemoveEnemy(dst, EnemyFate.Defeated, $"{dst}을(를) 물리쳤다");
            else { dst.SustainOn = false; Log(BattleEventType.Faint, dst, null, 0, $"{dst} 기절"); }
        }

        void RemoveEnemy(Unit e, EnemyFate fate, string text)
        {
            e.Fate = fate;
            Log(fate == EnemyFate.Defeated ? BattleEventType.Defeated : BattleEventType.Purified, null, e, 0, text);
            var r = Rewards; var b = Ctx.StageBuffs;
            bool clover = Ctx.Has(RelicId.FourLeafClover) && Rng.Chance(Balance.CloverExtraFoodChance);
            if (fate == EnemyFate.Defeated)
            {
                r.Meat += e.IsBoss ? Balance.BossMeat : Balance.MobMeat;
                if (Ctx.Has(RelicId.BrokenTooth) && Rng.Chance(Balance.ToothExtraMeatChance)) r.Meat++;
                if (clover) r.Meat++;
                if (e.IsBoss) r.BossDefeated = true;
                else { r.Fragments.Add((Diet)Rng.Range(0, 3)); r.MobsDefeated++; }
                if (Ctx.Has(RelicId.BloodAmulet)) b.Atk = Math.Min(Balance.BloodAmuletCap, b.Atk + Balance.BloodAmuletAtkStep);
            }
            else
            {
                r.Fruit += e.IsBoss ? Balance.BossFruit : Balance.MobFruit;
                if (Ctx.Has(RelicId.GlowingSeed) && Rng.Chance(Balance.SeedExtraFruitChance)) r.Fruit++;
                if (clover) r.Fruit++;
                if (e.IsBoss) { r.BossPurified = true; r.Fragments.Add(null); }
                else r.Fragments.Add((Diet)Rng.Range(0, 3));
                if (Ctx.Has(RelicId.ClearCrystal)) b.Purify = Math.Min(Balance.CrystalPurifyCap, b.Purify + Balance.CrystalPurifyStep);
            }
        }

        void CheckOutcome()
        {
            if (Outcome != BattleOutcome.Ongoing) return;
            // 동시에 끝나면 전멸이 우선 (§13: 전원 기절 = 게임 오버)
            if (Allies.All(x => x.Fainted)) Finish(BattleOutcome.Defeat, "전멸… 게임 오버"); // §13
            else if (Enemies.All(x => x.Removed)) Finish(BattleOutcome.Victory, "승리!");
        }

        void Finish(BattleOutcome o, string text)
        {
            Outcome = o;
            foreach (var a in Allies) a.SustainOn = false;
            Log(BattleEventType.End, null, null, 0, text);
        }

        static float HungerDeficit(Unit u) => 1f - (float)u.Hunger / Balance.MaxHunger;
        float StarveMult(Unit u) => u.Starving ? Balance.StarvingStatMult : 1f;

        public float Atk(Unit u)
        {
            float v = u.BaseAtk * (1 + u.PermanentBonus) * StarveMult(u);
            if (u.AtkDownTurns > 0) v *= 0.7f; // 가젤 프롱킹
            v *= SoloDesperation(u);
            if (u.IsEnemy)
                return u.Species.Skill.Id == SkillId.Frenzy && u.HpRatio < Balance.EnemyFrenzyHpThreshold ? v * Balance.EnemyFrenzyAtkMult : v;
            float bonus = Ctx.StageBuffs.Atk + SynergyAtk();
            if (u.Species.Skill.Id == SkillId.Frenzy) bonus += (IsSolo(u) ? 0.4f : 0.8f) * HungerDeficit(u); // 무리 잃은 사자는 절반
            bonus += 0.05f * u.RageStacks;
            if (Ctx.Has(RelicId.HeatedFang)) bonus += Math.Min(Balance.FangAtkCap, Balance.FangAtkPerRound * (Round - 1));
            if (Ctx.Has(RelicId.SealedClaw)) bonus += Balance.SealedClawAtk;
            return v * (1 + bonus);
        }

        public float Def(Unit u)
        {
            float v = u.BaseDef * (1 + u.PermanentBonus) * StarveMult(u);
            if (u.DefDownTurns > 0) v *= 0.8f;
            if (u.DefUpTurns > 0) v *= 1.3f; // 멧돼지 진흙 목욕
            v *= SoloDesperation(u);
            if (!u.IsEnemy && Ctx.Has(RelicId.YetiFur)) v *= Balance.YetiDefMult;
            return v;
        }

        public float Purify(Unit u)
        {
            float v = u.BasePurify * (1 + u.PermanentBonus) * StarveMult(u) + Ctx.StageBuffs.Purify;
            if (Ctx.Has(RelicId.ClearingSpring)) v += Math.Min(Balance.SpringPurifyCap, Balance.SpringPurifyPerRound * (Round - 1));
            if (Ctx.Has(RelicId.SealedClaw)) v += Balance.SealedClawPurify;
            return v;
        }

        /// <summary>홀로서기 "궁지 본능": 혼자 남으면 몸집이 작을수록 필사적으로 싸운다(ATK·DEF 배율).</summary>
        public float SoloDesperation(Unit u) => IsSolo(u) ? 1f + Balance.SoloDesperationBySize[(int)u.Species.Size] : 1f;

        /// <summary>무리사냥 + 적응(대상 무관한 부분)의 공격 보너스 합.</summary>
        float SynergyAtk()
        {
            float b = 0;
            if (Ctx.HasSynergy(Diet.Carnivore))
                b += Balance.PackHuntAtkPerCarnivore * Allies.Count(x => x.Active && x.Species.Diet == Diet.Carnivore);
            var m = CurrentAdapt();
            if (m == Synergies.AdaptMode.CarnOmni) b += Balance.AdaptAtk;
            else if (m == Synergies.AdaptMode.All) b += Balance.AdaptAllAtk;
            return b;
        }

        float SynergyPurify(Unit actor, Unit target)
        {
            if (actor == null || actor.IsEnemy) return 0;
            switch (CurrentAdapt())
            {
                case Synergies.AdaptMode.HerbOmni: return Balance.AdaptPurify;
                case Synergies.AdaptMode.All: return Balance.AdaptAllPurify;
                case Synergies.AdaptMode.OmniOnly: return target != null && target.HpRatio < 0.5f ? Balance.AdaptSoloPurify : 0;
                default: return 0;
            }
        }

        void Log(BattleEventType t, Unit a, Unit tg, int v, string text) =>
            _events.Add(new BattleEvent { Type = t, Actor = a, Target = tg, Value = v, Text = text });
    }
}

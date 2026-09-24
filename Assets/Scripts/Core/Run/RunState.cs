using System;
using System.Collections.Generic;
using System.Linq;

namespace FedAndFound.Core
{
    public enum RunPhase
    {
        RelicEquip,   // 스테이지 입장 전: 지형 미리보기 + 유물 장착 (§10, §11)
        Map,          // 노드 선택
        Battle,       // CurrentBattle 진행 중
        PostBattle,   // 먹기/저장, [휴식]/[계속 이동]
        Event,        // CurrentEvent 선택지
        Farewell,     // 스테이지 클리어: 포식/집 보내기 (§3)
        Victory, GameOver,
    }

    public enum NodeType { Mob, Event, Boss }

    /// <summary>한 판(런) 전체 상태와 규칙. UI는 Phase를 보고 화면을 전환하고, 아래 메서드만 호출한다.</summary>
    public sealed class RunState
    {
        public const int FinalStage = 3, NodesBeforeBoss = 3;

        public readonly IRng Rng;
        public readonly List<Unit> Party = new List<Unit>();
        public Unit Guest { get; private set; }      // 0스테이지 손님 (Party에도 포함됨)
        public int Stage { get; private set; }
        public Terrain Terrain { get; private set; }
        public int NodesMoved { get; private set; }
        public bool EventUsed { get; private set; }
        public RunPhase Phase { get; private set; }
        public bool RestedThisStop { get; private set; }

        public int Meat, Fruit;
        public readonly Dictionary<Diet, int> Fragments = new Dictionary<Diet, int> { { Diet.Herbivore, 0 }, { Diet.Carnivore, 0 }, { Diet.Omnivore, 0 } };
        public int UniversalFragments;
        public readonly Dictionary<Diet, int> Gems = new Dictionary<Diet, int> { { Diet.Herbivore, 0 }, { Diet.Carnivore, 0 }, { Diet.Omnivore, 0 } };

        public readonly List<RelicId> OwnedRelics = new List<RelicId>();
        public readonly List<RelicId> EquippedRelics = new List<RelicId>();
        public int RelicSlots { get; private set; }
        public StageBuffs StageBuffs { get; private set; } = new StageBuffs();

        public Battle CurrentBattle { get; private set; }
        public RandomEvent CurrentEvent { get; private set; }
        public BattleRewards LastRewards { get; private set; }
        public readonly List<string> Log = new List<string>();

        public RunState(IList<string> speciesIds, int seed, bool skipTutorial = false)
        {
            if (speciesIds.Count != 3) throw new ArgumentException("파티는 동물 3마리");
            Rng = new SystemRng(seed);
            foreach (var id in speciesIds) Party.Add(Unit.CreateAlly(SpeciesDb.Get(id)));

            if (skipTutorial)
            {
                // §12: 스킵 시 유물만 받고 1스테이지로
                RelicSlots = 1; GrantRelics(1);
                BeginStage(1);
            }
            else
            {
                Guest = Unit.CreateAlly(SpeciesDb.Get(SpeciesDb.GuestId));
                Party.Add(Guest);
                BeginStage(0);
            }
        }

        // ================= 스테이지 / 유물 장착 =================

        void BeginStage(int stage)
        {
            Stage = stage; NodesMoved = 0; EventUsed = false;
            StageBuffs = new StageBuffs();
            Terrain = Balance.StageTerrains[stage]; // 참고 맵 이미지처럼 스테이지마다 고정 지형 (§11)
            Phase = RunPhase.RelicEquip;
            Log.Add($"== {stage}스테이지 ({TerrainName(Terrain)}) ==");
        }

        public bool Equip(RelicId r)
        {
            if (Phase != RunPhase.RelicEquip || !OwnedRelics.Contains(r) || EquippedRelics.Contains(r) || EquippedRelics.Count >= RelicSlots) return false;
            EquippedRelics.Add(r); return true;
        }

        public bool Unequip(RelicId r) => Phase == RunPhase.RelicEquip && EquippedRelics.Remove(r);

        /// <summary>장착 확정 → 0스테이지는 바로 튜토리얼 보스전, 그 외엔 노드 맵.</summary>
        public void ConfirmEquip()
        {
            Expect(RunPhase.RelicEquip);
            if (Stage == 0) StartBattle(boss: true);
            else Phase = RunPhase.Map;
        }

        /// <summary>지형 유물은 지형이 일치할 때만 유효 (§10.1).</summary>
        public bool IsRelicEffective(RelicId r)
        {
            var d = RelicDb.Get(r);
            return d.Kind != RelicKind.TerrainPassive || d.Terrain == Terrain;
        }

        void GrantRelics(int n)
        {
            var pool = RelicDb.All.Keys.Where(r => !OwnedRelics.Contains(r)).ToList();
            for (int i = 0; i < n && pool.Count > 0; i++)
            {
                var r = Rng.Pick(pool); pool.Remove(r); OwnedRelics.Add(r);
                Log.Add($"유물 획득: {RelicDb.Get(r).Name}");
            }
        }

        // ================= 맵 / 노드 (§12, FR-4) =================

        public List<NodeType> NodeOptions()
        {
            if (Phase != RunPhase.Map) return new List<NodeType>();
            if (NodesMoved >= NodesBeforeBoss) return new List<NodeType> { NodeType.Boss };
            var list = new List<NodeType> { NodeType.Mob };
            if (!EventUsed) list.Add(NodeType.Event);
            return list;
        }

        public void EnterNode(NodeType node)
        {
            Expect(RunPhase.Map);
            if (!NodeOptions().Contains(node)) throw new InvalidOperationException($"선택 불가 노드: {node}");
            if (node == NodeType.Boss) { StartBattle(boss: true); return; }

            NodesMoved++;
            foreach (var a in Party) a.AddHunger(-Balance.HungerPerNode); // §9
            if (node == NodeType.Event)
            {
                EventUsed = true;
                CurrentEvent = RandomEvent.Roll(Rng);
                Phase = RunPhase.Event;
            }
            else StartBattle(boss: false);
        }

        public string ChooseEventOption(int index)
        {
            Expect(RunPhase.Event);
            var result = CurrentEvent.Options[index].Apply(this);
            Log.Add(result);
            CurrentEvent = null;
            Phase = RunPhase.Map;
            return result;
        }

        // ================= 전투 =================

        void StartBattle(bool boss)
        {
            float scale = 1f + Balance.EnemyScalePerStage * Stage;
            int alive = Party.Count(p => !p.Fainted);
            var enemies = new List<Unit>();
            if (boss)
            {
                // 0스테이지 보스는 잡몹 수준 난이도 (§12)
                if (Stage == 0) scale = 1f / Balance.BossHpMult;
                var bossUnit = Unit.CreateEnemy(Rng.Pick(SpeciesDb.Roster), true, scale);
                // 파티가 줄어드는 구조(3→2→1)라 보스 HP를 파티 규모에 맞춘다
                bossUnit.MaxHp = bossUnit.Hp = Math.Max(1, (int)(bossUnit.MaxHp * (0.2f + 0.27f * Party.Count)));
                enemies.Add(bossUnit);
            }
            else
            {
                int n = Rng.Range(Balance.MinMobsPerNode, Math.Min(Balance.MaxMobsPerNode, Math.Max(1, alive)) + 1);
                for (int i = 0; i < n; i++) enemies.Add(Unit.CreateEnemy(Rng.Pick(SpeciesDb.Roster), false, scale));
            }

            var ctx = new BattleContext { Terrain = Terrain, Rng = Rng, StageBuffs = StageBuffs };
            foreach (var kv in Gems) if (kv.Value > 0) ctx.SynergyDiets.Add(kv.Key); // 원석 시너지 (§4.5)
            foreach (var r in EquippedRelics.Where(IsRelicEffective)) ctx.Relics.Add(r);
            CurrentBattle = new Battle(Party.Where(p => !p.Fainted).ToList(), enemies, ctx);
            Phase = RunPhase.Battle;
        }

        /// <summary>CurrentBattle.Outcome이 결정된 뒤 호출.</summary>
        public void FinishBattle()
        {
            Expect(RunPhase.Battle);
            var b = CurrentBattle;
            if (b.Outcome == BattleOutcome.Ongoing) throw new InvalidOperationException("전투가 끝나지 않았다");
            foreach (var r in b.Ctx.ConsumedRelics) { OwnedRelics.Remove(r); EquippedRelics.Remove(r); }
            CurrentBattle = null;

            if (b.Outcome == BattleOutcome.Defeat) { Phase = RunPhase.GameOver; Log.Add("게임 오버"); return; }

            var rw = LastRewards = b.Rewards;
            Meat += rw.Meat; Fruit += rw.Fruit;
            foreach (var f in rw.Fragments) { if (f is Diet d) Fragments[d]++; else UniversalFragments++; }
            if (rw.MobsDefeated > 0)
                foreach (var a in Party) a.PermanentBonus += Balance.MobKillPermanentBonus * rw.MobsDefeated;
            Log.Add($"보상: 고기 {rw.Meat}, 열매 {rw.Fruit}, 조각 {rw.Fragments.Count}");

            if (b.Enemies.Any(e => e.IsBoss)) ClearStage();
            else { Phase = RunPhase.PostBattle; RestedThisStop = false; }
        }

        // ================= 전투 후: 먹기 / 휴식 (§9) =================

        /// <summary>음식 1개 먹이기. 배고픔만 회복, HP는 회복하지 않는다 (§9.1).</summary>
        public bool Feed(Unit u, FoodType f)
        {
            if (!Party.Contains(u) || !u.Species.CanEat(f)) return false;
            if (f == FoodType.Meat ? Meat <= 0 : Fruit <= 0) return false;
            u.AddHunger(Balance.FoodRestore);
            bool free = EquippedRelics.Contains(RelicId.SharedPantry) && Rng.Chance(Balance.PantryNoConsumeChance);
            if (!free) { if (f == FoodType.Meat) Meat--; else Fruit--; }
            return true;
        }

        /// <summary>[휴식]: 배고픔을 소모해 HP 회복. 기절한 동물은 회복되지 않는다.</summary>
        public void Rest()
        {
            Expect(RunPhase.PostBattle);
            if (RestedThisStop) return;
            RestedThisStop = true;
            foreach (var a in Party.Where(x => !x.Fainted && x.Hunger >= Balance.RestHungerCost))
            {
                a.AddHunger(-Balance.RestHungerCost);
                a.Heal((int)Math.Round(a.MaxHp * Balance.RestHealRatio));
            }
        }

        /// <summary>[계속 이동]</summary>
        public void Continue() { Expect(RunPhase.PostBattle); Phase = RunPhase.Map; }

        // ================= 조각 → 원석 (§8, FR-6) =================

        /// <summary>원석은 식성별 1개면 시너지가 해금되고 중첩되지 않으므로, 이미 가진 식성은 다시 만들 수 없다(조각 낭비 방지).</summary>
        public bool CanCraftGem(Diet d) => !HasSynergy(d) && Fragments[d] + UniversalFragments >= Balance.FragmentsPerGem;

        public bool HasSynergy(Diet d) => Gems[d] > 0;

        /// <summary>같은 식성 조각을 먼저 쓰고, 부족분만 만능 조각으로 채운다.</summary>
        public bool CraftGem(Diet d)
        {
            if (!CanCraftGem(d)) return false;
            int useOwn = Math.Min(Fragments[d], Balance.FragmentsPerGem);
            Fragments[d] -= useOwn;
            UniversalFragments -= Balance.FragmentsPerGem - useOwn;
            Gems[d]++;
            return true;
        }

        // ================= 스테이지 클리어 (§3) =================

        void ClearStage()
        {
            foreach (var a in Party) { a.Hp = a.MaxHp; a.ResetBattleState(); } // 전체 회복 + 기절 부활

            if (Stage == 0)
            {
                Party.Remove(Guest); Log.Add($"{Guest.Name}이(가) 집으로 돌아갔다");
                Guest = null; RelicSlots++; GrantRelics(1);
                BeginStage(1);
            }
            else if (Stage >= FinalStage)
            {
                Log.Add($"{Party[0].Name}이(가) 집으로 돌아갔다. 여정 끝!"); // FR-5
                Phase = RunPhase.Victory;
            }
            else Phase = RunPhase.Farewell;
        }

        /// <param name="devour">true = 포식, false = 집 보내기</param>
        public void Farewell(Unit u, bool devour)
        {
            Expect(RunPhase.Farewell);
            if (!Party.Contains(u)) throw new ArgumentException("파티원이 아니다");
            Party.Remove(u);
            if (devour)
            {
                int gain = Balance.DevourRestoreBySize[(int)u.Species.Size];
                foreach (var a in Party) a.AddHunger(gain);
                Log.Add($"{u.Name}을(를) 포식했다 (배고픔 +{gain})");
            }
            else { RelicSlots++; Log.Add($"{u.Name}을(를) 집으로 보냈다 (유물 슬롯 +1)"); }
            GrantRelics(2);
            BeginStage(Stage + 1);
        }

        // ================= 유틸 =================

        void Expect(RunPhase p) { if (Phase != p) throw new InvalidOperationException($"현재 단계는 {Phase}, 필요: {p}"); }

        public static string TerrainName(Terrain t) => t switch
        {
            Terrain.Swamp => "늪", Terrain.SnowMountain => "설산", Terrain.Plains => "들판", _ => "사막",
        };
    }
}

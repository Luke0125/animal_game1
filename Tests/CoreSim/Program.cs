using System;
using System.Collections.Generic;
using System.Linq;
using FedAndFound.Core;

// 1) 규칙 단위 검사  2) 봇으로 N판 자동 플레이 → 예외 없는지 + 밸런스 지표 출력
static class Program
{
    static int _fail;

    static void Check(bool ok, string name)
    {
        Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {name}");
        if (!ok) _fail++;
    }

    static int Main(string[] args)
    {
        RuleTests();
        int runs = args.Length > 0 ? int.Parse(args[0]) : 2000;
        Simulate(runs);
        Console.WriteLine(_fail == 0 ? "ALL OK" : $"{_fail} FAILED");
        return _fail == 0 ? 0 : 1;
    }

    static BattleContext Ctx(int seed = 1) => new BattleContext { Rng = new SystemRng(seed) };

    static void RuleTests()
    {
        // FR-1 / §4.4: 같은 종 동률이면 적이 먼저
        var ally = Unit.CreateAlly(SpeciesDb.Get("gazelle"));
        var foe = Unit.CreateEnemy(SpeciesDb.Get("gazelle"), false, 1f);
        var b = new Battle(new List<Unit> { ally }, new List<Unit> { foe }, Ctx());
        Check(b.TurnQueue[0] == foe, "속도 동률 시 적 선행");
        Check(!b.IsFirstActorThisRound(ally), "선제 판정은 전체 유닛 기준");

        // §7: 풀HP 적 → 저주 게이지 0, 확률 = 정화 효율
        var rabbit = Unit.CreateAlly(SpeciesDb.Get("rabbit"));
        var rhinoFoe = Unit.CreateEnemy(SpeciesDb.Get("rhino"), false, 1f);
        b = new Battle(new List<Unit> { rabbit }, new List<Unit> { rhinoFoe }, Ctx());
        Check(Math.Abs(b.PurifyChance(rabbit, rhinoFoe) - 14f) < 0.01f, "정화 확률 = 게이지 0 + 효율 14");
        rhinoFoe.Hp = rhinoFoe.MaxHp / 2;
        Check(b.PurifyChance(rabbit, rhinoFoe) > 14f, "HP가 줄면 정화 확률 상승");

        // §9: 배고픔 0 → 능력치 50%
        float atk = b.Atk(rabbit); rabbit.Hunger = 0;
        Check(Math.Abs(b.Atk(rabbit) - atk * 0.5f) < 0.01f, "배고픔 0이면 ATK 50%");

        // FR-2: 유지형 자동 해제
        var turtle = Unit.CreateAlly(SpeciesDb.Get("turtle"));
        var slowFoe = Unit.CreateEnemy(SpeciesDb.Get("rhino"), false, 1f);
        b = new Battle(new List<Unit> { turtle }, new List<Unit> { slowFoe }, Ctx());
        turtle.Hunger = 5;
        Check(b.SetSustain(turtle, true) && turtle.Hunger == 1, "유지형 ON 즉시 4 지불");
        b.Submit(new BattleAction(ActionType.Defend)); // 라운드 넘김 → 다음 턴 지불 불가
        Check(!turtle.SustainOn || b.Outcome != BattleOutcome.Ongoing, "배고픔 부족 시 유지형 자동 해제");

        // FR-6: 조각 → 원석 (만능 조각이 부족분 보충)
        var run = new RunState(new[] { "lion", "rabbit", "fox" }, 7, skipTutorial: true);
        run.Fragments[Diet.Carnivore] = 2; run.UniversalFragments = 1;
        Check(run.CraftGem(Diet.Carnivore) && run.Gems[Diet.Carnivore] == 1 && run.UniversalFragments == 0, "조각 2 + 만능 1 → 원석");
        Check(run.RelicSlots == 1 && run.OwnedRelics.Count == 1, "튜토리얼 스킵 → 슬롯 1, 유물 1");

        // 식성 규칙
        run.Meat = 1; run.Fruit = 1;
        Check(!run.Feed(run.Party[1], FoodType.Meat) && run.Feed(run.Party[1], FoodType.Fruit), "초식은 열매만");

        // §20-8 원석 = 시너지 해금, 중첩 없음
        run.Fragments[Diet.Carnivore] = 3;
        Check(!run.CanCraftGem(Diet.Carnivore), "이미 가진 원석은 다시 못 만듦");

        SynergyTests();
        EnemySkillTests();
        SimultaneousKoTest();
        EventTests();
    }

    static Battle SynergyBattle(Diet[] gems, params string[] allyIds)
    {
        var ctx = Ctx();
        foreach (var d in gems) ctx.SynergyDiets.Add(d);
        var allies = allyIds.Select(id => Unit.CreateAlly(SpeciesDb.Get(id))).ToList();
        var foe = Unit.CreateEnemy(SpeciesDb.Get("rhino"), false, 1f); // 가장 느린 적 → 아군이 먼저 행동
        return new Battle(allies, new List<Unit> { foe }, ctx);
    }

    static void SynergyTests()
    {
        // 무리사냥: 육식 2마리 → +16%
        var none = SynergyBattle(new Diet[0], "lion", "leopard", "rabbit");
        var pack = SynergyBattle(new[] { Diet.Carnivore }, "lion", "leopard", "rabbit");
        float ratio = pack.Atk(pack.Allies[2]) / none.Atk(none.Allies[2]);
        Check(Math.Abs(ratio - (1 + 2 * Balance.PackHuntAtkPerCarnivore)) < 0.001f, "무리사냥: 육식 2마리 → 공격 +16%");

        // 적응: 파티 구성별 형태
        Check(SynergyBattle(new[] { Diet.Omnivore }, "rabbit", "fox").CurrentAdapt() == Synergies.AdaptMode.HerbOmni, "적응: 초식+잡식");
        Check(SynergyBattle(new[] { Diet.Omnivore }, "lion", "fox").CurrentAdapt() == Synergies.AdaptMode.CarnOmni, "적응: 육식+잡식");
        Check(SynergyBattle(new[] { Diet.Omnivore }, "rabbit", "lion", "fox").CurrentAdapt() == Synergies.AdaptMode.All, "적응: 셋 다");
        Check(SynergyBattle(new[] { Diet.Omnivore }, "rabbit", "lion").CurrentAdapt() == Synergies.AdaptMode.None, "적응: 잡식 없으면 비활성");
        var solo = SynergyBattle(new[] { Diet.Omnivore }, "fox");
        var foe = solo.Enemies[0];
        float full = solo.PurifyChance(solo.Allies[0], foe);
        Check(solo.CurrentAdapt() == Synergies.AdaptMode.OmniOnly && Math.Abs(full - 11f) < 0.01f, "적응(잡식뿐): 적 HP 50% 이상이면 정화 보너스 없음");
        foe.Hp = foe.MaxHp / 4;
        var soloNo = SynergyBattle(new Diet[0], "fox"); soloNo.Enemies[0].Hp = soloNo.Enemies[0].MaxHp / 4;
        Check(Math.Abs(solo.PurifyChance(solo.Allies[0], foe) - soloNo.PurifyChance(soloNo.Allies[0], soloNo.Enemies[0]) - Balance.AdaptSoloPurify) < 0.01f,
            "적응(잡식뿐): 적 HP 50% 미만이면 정화 +8%p");

        // 생명의 순환: 초식 정화 성공 → 전원 배고픔 회복. 확률 95% 상한 상황을 여러 시드로 시도
        bool restored = false;
        for (int seed = 1; seed < 30 && !restored; seed++)
        {
            var ctx = Ctx(seed); ctx.SynergyDiets.Add(Diet.Herbivore);
            var rabbit = Unit.CreateAlly(SpeciesDb.Get("rabbit")); var lion = Unit.CreateAlly(SpeciesDb.Get("turtle")); // 토끼보다 느린 동료
            rabbit.Hunger = 50; lion.Hunger = 50;
            var weak = Unit.CreateEnemy(SpeciesDb.Get("rhino"), false, 1f); weak.Hp = 1;
            var b = new Battle(new List<Unit> { rabbit, lion }, new List<Unit> { weak }, ctx);
            if (b.CurrentActor != rabbit) continue;
            b.Submit(new BattleAction(ActionType.Purify, weak));
            restored = weak.Fate == EnemyFate.Purified && lion.Hunger == 50 + Balance.CycleHungerRestore;
        }
        Check(restored, "생명의 순환: 초식 정화 성공 → 아군 배고픔 +4");
    }

    /// <summary>이벤트는 무작위라 사람이 전부 확인하기 어렵다 → 모든 이벤트의 모든 선택지를 강제로 한 번씩 실행.</summary>
    static void EventTests()
    {
        int tried = 0, errors = 0;
        for (int i = 0; i < RandomEvent.Count; i++)
        {
            int options = RandomEvent.Create(i).Options.Count;
            for (int o = 0; o < options; o++)
            {
                foreach (bool rich in new[] { true, false })
                    for (int seed = 0; seed < 10; seed++)
                    {
                        try
                        {
                            var run = new RunState(new[] { "lion", "rabbit", "fox" }, seed, skipTutorial: true);
                            run.ConfirmEquip();
                            if (rich) { run.Meat = 5; run.Fruit = 5; }
                            run.ForcedEventIndex = i;
                            run.EnterNode(NodeType.Event);
                            var ev = run.CurrentEvent;
                            if (ev.Title != RandomEvent.Create(i).Title) throw new Exception("강제 이벤트가 아님");
                            if (!ev.Options[o].CanChoose(run)) continue; // 가난한 경우 못 고르는 선택지는 건너뜀
                            int meat = run.Meat, fruit = run.Fruit;
                            string result = run.ChooseEventOption(o);
                            tried++;
                            if (string.IsNullOrEmpty(result)) throw new Exception("결과 문구 없음");
                            if (run.Phase != RunPhase.Map) throw new Exception("이벤트 후 맵으로 안 돌아감");
                            if (run.Meat < 0 || run.Fruit < 0) throw new Exception("음식이 음수");
                            if (run.Party.Any(a => a.Hp < 0 || a.Hunger < 0)) throw new Exception("HP/배고픔 음수");
                        }
                        catch (Exception e)
                        {
                            if (errors++ < 3) Console.WriteLine($"  이벤트 {i} 선택지 {o}: {e.Message}");
                        }
                    }
            }
        }
        Check(errors == 0 && tried > 0, $"이벤트 {RandomEvent.Count}종 모든 선택지 실행 ({tried}회, 오류 {errors})");

        // 이벤트 무제한 디버그 플래그
        var r2 = new RunState(new[] { "lion", "rabbit", "fox" }, 3, skipTutorial: true);
        r2.ConfirmEquip();
        r2.DebugUnlimitedEvents = true;
        r2.EnterNode(NodeType.Event); r2.ChooseEventOption(r2.CurrentEvent.Options.Count - 1);
        Check(r2.NodeOptions().Contains(NodeType.Event), "디버그: 이벤트 무제한이면 이벤트 노드가 다시 열림");
    }

    /// <summary>회귀 테스트: 가시 세운 마지막 적을 쓰러뜨려도, 쓰러진 적은 반격하지 않는다(= 전원 기절 승리 없음).</summary>
    static void SimultaneousKoTest()
    {
        var ally = Unit.CreateAlly(SpeciesDb.Get("gazelle")); ally.Hp = 1;
        var hog = Unit.CreateEnemy(SpeciesDb.Get("hedgehog"), false, 1f); hog.Hp = 1;
        var b = new Battle(new List<Unit> { ally }, new List<Unit> { hog }, Ctx());
        hog.SustainOn = true;
        b.Submit(new BattleAction(ActionType.Attack, hog));
        Check(b.Outcome == BattleOutcome.Victory && !ally.Fainted, "쓰러진 가시 적은 반격 안 함 (전원 기절 승리 버그)");
    }

    static void EnemySkillTests()
    {
        // 각 종이 적으로 나와도 예외 없이 100라운드 버티는지 + 스킬을 실제로 쓰는지
        int skillEvents = 0;
        foreach (var s in SpeciesDb.All.Values)
        {
            for (int seed = 0; seed < 20; seed++)
            {
                var tank = Unit.CreateAlly(SpeciesDb.Get("turtle")); tank.MaxHp = tank.Hp = 100000;
                var mimicBait = Unit.CreateAlly(SpeciesDb.Get("rhino")); mimicBait.MaxHp = mimicBait.Hp = 100000;
                var e1 = Unit.CreateEnemy(s, true, 1f); e1.MaxHp = e1.Hp = 100000;
                var e2 = Unit.CreateEnemy(SpeciesDb.Get("rabbit"), false, 1f); e2.MaxHp = e2.Hp = 100000;
                var b = new Battle(new List<Unit> { tank, mimicBait }, new List<Unit> { e1, e2 }, Ctx(seed));
                for (int i = 0; i < 100 && b.Outcome == BattleOutcome.Ongoing; i++)
                {
                    var u = b.CurrentActor;
                    var act = u.Species.Id == "rhino" && b.CanUseSkill(u, out _) ? new BattleAction(ActionType.Skill, e1) : new BattleAction(ActionType.Attack, e1);
                    if (u.Hunger < 20) u.Hunger = 100;
                    b.Submit(act);
                    skillEvents += b.DrainEvents().Count(ev => ev.Actor == e1 && (ev.Type == BattleEventType.Skill || ev.Type == BattleEventType.Sustain || ev.Type == BattleEventType.Heal));
                }
            }
        }
        Check(skillEvents > 0, $"적 종별 스킬 사용 (스킬 이벤트 {skillEvents}회, 예외 없음)");
    }

    // ================= 자동 플레이 =================

    sealed class Stats
    {
        public int Runs, Wins, Crashes, Purified, Defeated, Battles, Rounds;
        public int[] ReachedStage = new int[RunState.FinalStage + 2];
        public int[] DeathAtStage = new int[RunState.FinalStage + 1];
        public int BossDeaths, MobDeaths;
        public int[] BossFights = new int[RunState.FinalStage + 1], BossLosses = new int[RunState.FinalStage + 1];
        public float BossHungerSum, BossHpSum; public int StarvingAtBoss;
        public System.Collections.Generic.Dictionary<string, int> PickWins, PickCount;
    }

    static void Simulate(int runs)
    {
        var all = new[] { RunBot(new SmartBot(false), runs), RunBot(new SmartBot(true), runs), RunBot(new RandomBot(), runs) };
        Check(all.All(x => x.Crashes == 0), $"자동 플레이 {runs}판×{all.Length} 예외 없음 (crash {all.Sum(x => x.Crashes)})");
        foreach (var st in all) Report(st);
    }

    static Stats RunBot(IBot bot, int runs)
    {
        var rosterIds = SpeciesDb.Roster.Select(s => s.Id).ToList();
        var st = new Stats { Runs = runs, PickWins = rosterIds.ToDictionary(x => x, _ => 0), PickCount = rosterIds.ToDictionary(x => x, _ => 0) };

        for (int seed = 0; seed < runs; seed++)
        {
            var rng = new SystemRng(seed * 31 + 7);
            var ids = rosterIds.OrderBy(_ => rng.Value()).Take(3).ToList();
            try
            {
                var run = new RunState(ids, seed, skipTutorial: seed % 2 == 0);
                int guard = 0;
                while (run.Phase != RunPhase.Victory && run.Phase != RunPhase.GameOver && guard++ < 10000)
                    Step(bot, run, rng, st);
                if (guard >= 10000) throw new Exception("무한 루프");
                st.ReachedStage[run.Stage]++;
                foreach (var id in ids) st.PickCount[id]++;
                if (run.Phase == RunPhase.Victory) { st.Wins++; foreach (var id in ids) st.PickWins[id]++; }
                else st.DeathAtStage[run.Stage]++;
            }
            catch (Exception e)
            {
                if (st.Crashes++ < 3) Console.WriteLine($"CRASH [{bot.Name}] seed={seed}: {e}");
            }
        }
        _names[st] = bot.Name;
        return st;
    }

    static readonly System.Collections.Generic.Dictionary<Stats, string> _names = new System.Collections.Generic.Dictionary<Stats, string>();

    static void Report(Stats st)
    {
        int n = st.Runs;
        Console.WriteLine($"\n[{_names[st]}] 승률 {100f * st.Wins / n:0.0}%");
        Console.WriteLine($"  패배 스테이지: {string.Join(", ", st.DeathAtStage.Select((c, i) => $"{i}:{c}"))}  (보스전 패배 {st.BossDeaths} / 잡몹전 패배 {st.MobDeaths})");
        Console.WriteLine($"  처치 방식: 물리치기 {st.Defeated} / 정화 {st.Purified} (정화 비율 {100f * st.Purified / Math.Max(1, st.Purified + st.Defeated):0}%)");
        int bf = st.BossFights.Sum();
        Console.WriteLine($"  보스 패배율(스테이지별): {string.Join(", ", st.BossFights.Select((c, i) => $"{i}:{100f * st.BossLosses[i] / Math.Max(1, c):0}%"))}" +
            $"  | 보스 입장 시 평균 배고픔 {st.BossHungerSum / Math.Max(1, bf):0}, 평균 HP {100 * st.BossHpSum / Math.Max(1, bf):0}%, 굶주린 동물 있음 {100f * st.StarvingAtBoss / Math.Max(1, bf):0}%");
        Console.WriteLine($"  전투당 평균 라운드 {(float)st.Rounds / Math.Max(1, st.Battles):0.0}, 판당 평균 전투 {(float)st.Battles / n:0.0}");
        var ids = SpeciesDb.Roster.Select(s => s.Id);
        Console.WriteLine("  종별 승률: " + string.Join(", ",
            ids.Select(id => $"{SpeciesDb.Get(id).Name} {100f * st.PickWins[id] / Math.Max(1, st.PickCount[id]):0}%")));
    }

    static void Step(IBot bot, RunState run, IRng rng, Stats st)
    {
        switch (run.Phase)
        {
            case RunPhase.RelicEquip:
                bot.Equip(run);
                run.ConfirmEquip(); break;
            case RunPhase.Map:
                bot.CraftGems(run);
                run.EnterNode(bot.PickNode(run, rng)); break;
            case RunPhase.Event:
                run.ChooseEventOption(bot.PickEventOption(run, rng)); break;
            case RunPhase.Battle:
                var b = run.CurrentBattle;
                bool boss = b.Enemies.Any(e => e.IsBoss);
                if (boss)
                {
                    st.BossFights[run.Stage]++;
                    st.BossHungerSum += (float)b.Allies.Average(a => a.Hunger);
                    st.BossHpSum += (float)b.Allies.Average(a => a.HpRatio);
                    if (b.Allies.Any(a => a.Starving)) st.StarvingAtBoss++;
                }
                while (b.Outcome == BattleOutcome.Ongoing) b.Submit(bot.Act(b, rng));
                st.Battles++; st.Rounds += b.Round;
                st.Purified += b.Enemies.Count(e => e.Fate == EnemyFate.Purified);
                st.Defeated += b.Enemies.Count(e => e.Fate == EnemyFate.Defeated);
                if (b.Outcome == BattleOutcome.Defeat) { if (boss) { st.BossDeaths++; st.BossLosses[run.Stage]++; } else st.MobDeaths++; }
                run.FinishBattle(); break;
            case RunPhase.PostBattle:
                bot.PostBattle(run);
                run.Continue(); break;
            case RunPhase.Farewell:
                bot.Farewell(run, rng); break;
        }
    }
}

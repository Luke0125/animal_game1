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
    }

    // ================= 자동 플레이 =================

    static void Simulate(int runs)
    {
        var rosterIds = SpeciesDb.Roster.Select(s => s.Id).ToList();
        int wins = 0, crashes = 0;
        var reachedStage = new int[RunState.FinalStage + 2];
        int purified = 0, defeated = 0;
        var pickWins = rosterIds.ToDictionary(x => x, _ => 0);
        var pickCount = rosterIds.ToDictionary(x => x, _ => 0);

        for (int seed = 0; seed < runs; seed++)
        {
            var rng = new SystemRng(seed * 31 + 7);
            var ids = rosterIds.OrderBy(_ => rng.Value()).Take(3).ToList();
            try
            {
                var run = new RunState(ids, seed, skipTutorial: seed % 2 == 0);
                int guard = 0;
                while (run.Phase != RunPhase.Victory && run.Phase != RunPhase.GameOver && guard++ < 10000)
                    Step(run, rng, ref purified, ref defeated);
                if (guard >= 10000) throw new Exception("무한 루프");
                reachedStage[run.Stage]++;
                foreach (var id in ids) pickCount[id]++;
                if (run.Phase == RunPhase.Victory) { wins++; foreach (var id in ids) pickWins[id]++; }
            }
            catch (Exception e)
            {
                if (crashes++ < 3) Console.WriteLine($"CRASH seed={seed}: {e}");
            }
        }

        Check(crashes == 0, $"자동 플레이 {runs}판 예외 없음 (crash {crashes})");
        Console.WriteLine($"\n승률 {100f * wins / runs:0.0}% | 도달 스테이지 분포 {string.Join(", ", reachedStage.Select((c, i) => $"{i}:{c}"))}");
        Console.WriteLine($"처치 방식: 물리치기 {defeated} / 정화 {purified}");
        Console.WriteLine("종별 승률(포함된 판 기준): " + string.Join(", ",
            rosterIds.Select(id => $"{SpeciesDb.Get(id).Name} {100f * pickWins[id] / Math.Max(1, pickCount[id]):0}%")));
    }

    static void Step(RunState run, IRng rng, ref int purified, ref int defeated)
    {
        switch (run.Phase)
        {
            case RunPhase.RelicEquip:
                foreach (var r in run.OwnedRelics.Where(run.IsRelicEffective).ToList()) run.Equip(r);
                run.ConfirmEquip(); break;
            case RunPhase.Map:
                var opts = run.NodeOptions();
                run.EnterNode(rng.Pick(opts)); break;
            case RunPhase.Event:
                var ev = run.CurrentEvent;
                var ok = Enumerable.Range(0, ev.Options.Count).Where(i => ev.Options[i].CanChoose(run)).ToList();
                run.ChooseEventOption(rng.Pick(ok)); break;
            case RunPhase.Battle:
                var b = run.CurrentBattle;
                while (b.Outcome == BattleOutcome.Ongoing) b.Submit(BotAction(b, rng));
                purified += b.Enemies.Count(e => e.Fate == EnemyFate.Purified);
                defeated += b.Enemies.Count(e => e.Fate == EnemyFate.Defeated);
                run.FinishBattle(); break;
            case RunPhase.PostBattle:
                foreach (var a in run.Party)
                    while (a.Hunger < 60 && (run.Feed(a, FoodType.Fruit) || run.Feed(a, FoodType.Meat))) { }
                if (run.Party.Where(a => !a.Fainted).Average(a => a.HpRatio) < 0.6f) run.Rest();
                run.Continue(); break;
            case RunPhase.Farewell:
                run.Farewell(rng.Pick(run.Party), rng.Chance(0.5f)); break;
        }
    }

    static BattleAction BotAction(Battle b, IRng rng)
    {
        var u = b.CurrentActor;
        foreach (var a in b.Allies.Where(x => x.Active && x.Species.Skill.Kind == SkillKind.Sustain))
            b.SetSustain(a, a.Hunger > 40);
        var foes = b.Enemies.Where(e => e.Active).ToList();
        var weakest = foes.OrderBy(e => e.HpRatio).First();
        var relic = b.CanUseRelic(RelicId.GuardShell) ? RelicId.GuardShell : (RelicId?)null;

        if (rng.Chance(0.35f) && b.CanUseSkill(u, out _))
        {
            var s = b.EffectiveSkill(u);
            Unit t = s.Target == SkillTarget.Enemy ? weakest
                   : s.Target == SkillTarget.Ally ? b.Allies.Where(x => x.Active).OrderBy(x => x.HpRatio).First() : null;
            return new BattleAction(ActionType.Skill, t, relic);
        }
        if (b.PurifyChance(u, weakest) > 35f && rng.Chance(0.6f))
            return new BattleAction(ActionType.Purify, weakest, b.CanUseRelic(RelicId.PurifyIncense) ? RelicId.PurifyIncense : relic);
        return new BattleAction(ActionType.Attack, weakest, b.CanUseRelic(RelicId.BeastClaw) ? RelicId.BeastClaw : relic);
    }
}

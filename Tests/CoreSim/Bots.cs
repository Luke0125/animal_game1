using System.Linq;
using FedAndFound.Core;

/// <summary>자동 플레이 정책. RandomBot = 아무렇게나 누르는 하한선, SmartBot = "생각하고 하는" 일반 플레이어 근사치.
/// 밸런스 목표는 SmartBot 기준으로 잡는다(6단계). 사람은 SmartBot보다 잘하거나 비슷하다고 본다.</summary>
interface IBot
{
    string Name { get; }
    void Equip(RunState run);
    NodeType PickNode(RunState run, IRng rng);
    int PickEventOption(RunState run, IRng rng);
    BattleAction Act(Battle b, IRng rng);
    void PostBattle(RunState run);
    void Farewell(RunState run, IRng rng);
    void CraftGems(RunState run);
}

sealed class RandomBot : IBot
{
    public string Name => "무작위 봇";

    public void Equip(RunState run)
    {
        foreach (var r in run.OwnedRelics.Where(run.IsRelicEffective).ToList()) run.Equip(r);
    }

    public NodeType PickNode(RunState run, IRng rng) => rng.Pick(run.NodeOptions());

    public int PickEventOption(RunState run, IRng rng)
    {
        var ev = run.CurrentEvent;
        return rng.Pick(Enumerable.Range(0, ev.Options.Count).Where(i => ev.Options[i].CanChoose(run)).ToList());
    }

    public BattleAction Act(Battle b, IRng rng)
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

    public void PostBattle(RunState run)
    {
        foreach (var a in run.Party)
            while (a.Hunger < 60 && (run.Feed(a, FoodType.Fruit) || run.Feed(a, FoodType.Meat))) { }
        if (run.Party.Where(a => !a.Fainted).Average(a => a.HpRatio) < 0.6f) run.Rest();
    }

    public void Farewell(RunState run, IRng rng) => run.Farewell(rng.Pick(run.Party), rng.Chance(0.5f));

    public void CraftGems(RunState run)
    {
        foreach (Diet d in new[] { Diet.Carnivore, Diet.Herbivore, Diet.Omnivore }) run.CraftGem(d);
    }
}

sealed class SmartBot : IBot
{
    readonly bool _purifier;
    /// <param name="purifier">true = 정화 성향(적을 깎아 두고 정화), false = 사냥 성향(웬만하면 물리침)</param>
    public SmartBot(bool purifier) { _purifier = purifier; }
    public string Name => _purifier ? "정화 성향 봇" : "사냥 성향 봇";

    public void Equip(RunState run)
    {
        // 봉인된 발톱은 스킬을 막으므로 뒤로 미룬다
        foreach (var r in run.OwnedRelics.Where(run.IsRelicEffective).OrderBy(r => r == RelicId.SealedClaw ? 1 : 0).ToList()) run.Equip(r);
    }

    public NodeType PickNode(RunState run, IRng rng)
    {
        var opts = run.NodeOptions();
        if (opts.Contains(NodeType.Boss)) return NodeType.Boss;
        // 파티가 많이 다쳤으면 이벤트로 전투 한 번을 피한다
        var alive = run.Party.Where(a => !a.Fainted).ToList();
        if (opts.Contains(NodeType.Event) && (alive.Average(a => a.HpRatio) < 0.55f || rng.Chance(0.25f))) return NodeType.Event;
        return NodeType.Mob;
    }

    public int PickEventOption(RunState run, IRng rng)
    {
        // 이벤트마다 규칙을 다 넣지 않고, 가능한 선택지 중 첫 번째(대개 이득을 노리는 쪽)를 60%, 나머지는 무작위
        var ev = run.CurrentEvent;
        var ok = Enumerable.Range(0, ev.Options.Count).Where(i => ev.Options[i].CanChoose(run)).ToList();
        return rng.Chance(0.6f) ? ok[0] : rng.Pick(ok);
    }

    public BattleAction Act(Battle b, IRng rng)
    {
        var u = b.CurrentActor;
        var foes = b.Enemies.Where(e => e.Active).ToList();
        var allies = b.Allies.Where(x => x.Active).ToList();

        foreach (var a in allies.Where(x => x.Species.Skill.Kind == SkillKind.Sustain))
            b.SetSustain(a, a.Hunger > 30 && (foes.Count > 1 || a.HpRatio < 0.8f || a.Species.Skill.Id == SkillId.Spines));

        // 노리는 적이 있고 내가 약하면 방어 (매복 예고 대응)
        bool threat = foes.Any(e => e.ChargedReady);
        var guard = b.CanUseRelic(RelicId.GuardShell) && (threat || u.HpRatio < 0.5f) ? RelicId.GuardShell : (RelicId?)null;
        if (u.HpRatio < 0.3f && (threat || foes.Count > 1) && allies.Count > 1)
            return new BattleAction(ActionType.Defend, null, guard);

        var focus = foes.OrderBy(e => e.Hp).First(); // 가장 쉽게 끝낼 적에 집중
        float purify = b.PurifyChance(u, focus);
        float expDmg = b.Atk(u) * Balance.DefenseK / (Balance.DefenseK + b.Def(focus));
        bool killable = expDmg * 0.9f >= focus.Hp;

        if (b.CanUseSkill(u, out _) && u.Hunger > 35)
        {
            var s = b.EffectiveSkill(u);
            switch (s.Id)
            {
                case SkillId.Graze:
                    var hurt = allies.OrderBy(x => x.HpRatio).First();
                    if (hurt.HpRatio < 0.6f) return new BattleAction(ActionType.Skill, hurt, guard);
                    break;
                case SkillId.Charge: case SkillId.Rampage: case SkillId.VenomBite:
                    if (!killable) return new BattleAction(ActionType.Skill, foes.OrderByDescending(e => e.Hp).First(), guard);
                    break;
                case SkillId.Ambush:
                    if (foes.Sum(e => e.Hp) > expDmg * 3) return new BattleAction(ActionType.Skill, null, guard);
                    break;
                case SkillId.Soothe:
                    if (purify > 30f && purify < 80f) return new BattleAction(ActionType.Skill, null, guard);
                    break;
                case SkillId.Swerve:
                    if (u.HpRatio < 0.5f) return new BattleAction(ActionType.Skill, null, guard);
                    break;
            }
        }

        // 한 방에 못 끝내고 정화 확률이 충분하면 정화, 아니면 공격
        if (!killable && purify >= (_purifier ? 40f : 80f))
            return new BattleAction(ActionType.Purify, focus, b.CanUseRelic(RelicId.PurifyIncense) && purify < 85f ? RelicId.PurifyIncense : guard);
        return new BattleAction(ActionType.Attack, focus, b.CanUseRelic(RelicId.BeastClaw) && !killable ? RelicId.BeastClaw : guard);
    }

    public void PostBattle(RunState run)
    {
        var alive = run.Party.Where(a => !a.Fainted).ToList();
        if (alive.Average(a => a.HpRatio) < 0.7f) run.Rest();
        // 저장도 딜레마라 전부 먹지 않고 배고픔 70까지만
        foreach (var a in run.Party.OrderBy(a => a.Hunger))
            while (a.Hunger < 70 && (run.Feed(a, a.Species.CanEat(FoodType.Meat) && run.Meat > run.Fruit ? FoodType.Meat : FoodType.Fruit)
                                     || run.Feed(a, FoodType.Meat) || run.Feed(a, FoodType.Fruit))) { }
    }

    public void Farewell(RunState run, IRng rng)
    {
        // 가장 약한 동물과 작별. 배고프면 포식, 여유 있으면 집 보내기(유물 슬롯)
        var weakest = run.Party.OrderBy(a => a.MaxHp * a.BaseAtk).First();
        bool hungry = run.Party.Where(a => a != weakest).Average(a => a.Hunger) < 55;
        run.Farewell(weakest, hungry);
    }

    public void CraftGems(RunState run)
    {
        foreach (Diet d in new[] { Diet.Carnivore, Diet.Omnivore, Diet.Herbivore }) run.CraftGem(d);
    }
}

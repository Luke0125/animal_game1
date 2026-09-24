namespace FedAndFound.Core
{
    public enum EnemyFate { None, Defeated, Purified }

    /// <summary>아군 동물 또는 적. 아군은 런 전체에서 같은 인스턴스를 재사용한다(HP·배고픔 유지).</summary>
    public sealed class Unit
    {
        public readonly SpeciesData Species;
        public readonly bool IsEnemy, IsBoss;
        public int MaxHp, Hp;
        public float BaseAtk, BaseDef, BasePurify;
        public float PermanentBonus;          // 잡몹 처치 영구 보너스 (§12)
        public int Hunger = Balance.MaxHunger; // 아군만 의미 있음

        public string Name => Species.Name;
        public bool Fainted => Hp <= 0;
        public bool Starving => !IsEnemy && Hunger <= 0;
        public float HpRatio => MaxHp <= 0 ? 0 : (float)Hp / MaxHp;

        // --- 전투 한정 상태 (ResetBattleState로 초기화) ---
        public bool Defending, SustainOn, ChargedReady, GuardShellArmed;
        public EnemyFate Fate;
        public float Evade;
        public int PoisonTurns, PoisonDmg, DefDownTurns, SkillUses, NextRoundBoost;
        internal bool SustainPaidThisRound;
        public bool Removed => Fate != EnemyFate.None;
        public bool Active => !Fainted && !Removed;

        Unit(SpeciesData s, bool enemy, bool boss) { Species = s; IsEnemy = enemy; IsBoss = boss; }

        public static Unit CreateAlly(SpeciesData s) => new Unit(s, false, false)
        { MaxHp = s.Hp, Hp = s.Hp, BaseAtk = s.Atk, BaseDef = s.Def, BasePurify = s.Purify };

        /// <param name="stageScale">스테이지가 올라갈수록 적이 강해지는 배율</param>
        public static Unit CreateEnemy(SpeciesData s, bool boss, float stageScale)
        {
            float hp = boss ? Balance.BossHpMult : Balance.MobHpMult;
            float atk = boss ? Balance.BossAtkMult : Balance.MobAtkMult;
            int maxHp = (int)(s.Hp * hp * stageScale);
            return new Unit(s, true, boss) { MaxHp = maxHp, Hp = maxHp, BaseAtk = s.Atk * atk * stageScale, BaseDef = s.Def * stageScale };
        }

        public void ResetBattleState()
        {
            Defending = SustainOn = ChargedReady = GuardShellArmed = SustainPaidThisRound = false;
            Fate = EnemyFate.None; Evade = 0;
            PoisonTurns = PoisonDmg = DefDownTurns = SkillUses = NextRoundBoost = 0;
        }

        public void Heal(int amount) { if (!Fainted) Hp = System.Math.Min(MaxHp, Hp + amount); }
        public void AddHunger(int amount) => Hunger = System.Math.Max(0, System.Math.Min(Balance.MaxHunger, Hunger + amount));
        public override string ToString() => $"{(IsEnemy ? (IsBoss ? "[보스]" : "[적]") : "")}{Name}";
    }
}

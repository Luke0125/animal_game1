namespace FedAndFound.Core
{
    /// <summary>모든 제안 수치를 한 곳에 모은다. 플레이테스트 후 여기만 고치면 된다. (GDD §20 미정 사항 대부분)</summary>
    public static class Balance
    {
        // 배고픔 (§9)
        public const int MaxHunger = 100;
        public const int HungerPerNode = 8;
        public const int FoodRestore = 12;              // 음식 1개당 (의도적으로 낮게)
        public const float StarvingStatMult = 0.5f;     // 배고픔 0 → 능력치 50%
        public static readonly int[] DevourRestoreBySize = { 25, 40, 60 }; // 소/중/대

        // 휴식 (§2-6, 수치 미정)
        public const int RestHungerCost = 15;
        public const float RestHealRatio = 0.5f;

        // 전투
        public const float DefenseK = 30f;              // dmg = atk*coef * K/(K+def)
        public const float DamageVariance = 0.1f;
        public const float DefendDamageMult = 0.5f;
        public const float MobHpMult = 0.6f, MobAtkMult = 0.7f;
        public const float BossHpMult = 2.2f, BossAtkMult = 1.1f;
        public const float EnemyScalePerStage = 0.12f;
        public const float MobKillPermanentBonus = 0.02f; // 잡몹 물리치기 1마리당 영구 +2%
        public const int MinMobsPerNode = 1, MaxMobsPerNode = 3; // 3노드 × 3 = 최대 9 (§12/§20-13)

        // 정화 (§7)
        public const float CurseGaugeMax = 50f;         // HP 0%에 가까울수록 이 값에 수렴
        public const float BossCurseMult = 0.6f;
        public const float PurifyChanceCap = 95f;
        public const int PurifyFruitCost = 0;           // §20-11 미정: 0이면 소비 없음

        // 스킬 (§5)
        public const int SustainCostPerTurn = 4;

        // 보상 (§6, §8, §14)
        public const int MobMeat = 1, MobFruit = 1;
        public const int BossMeat = 5, BossFruit = 5;
        public const int FragmentsPerGem = 3;

        // 유물 (§10) — "일정 확률/소폭" 제안값
        public const float ToothExtraMeatChance = 0.3f;
        public const float SeedExtraFruitChance = 0.3f;
        public const float PantryNoConsumeChance = 0.25f;
        public const float BellPurifyStep = 5f, BellPurifyCap = 20f;
        public const float BloodAmuletAtkStep = 0.03f, BloodAmuletCap = 0.3f;
        public const float CrystalPurifyStep = 2f, CrystalPurifyCap = 12f;
        public const float SealedClawAtk = 0.25f, SealedClawPurify = 10f;
        public const float ThriftMult = 0.6f;
        public const float FangAtkPerRound = 0.04f, FangAtkCap = 0.4f;
        public const float SpringPurifyPerRound = 2f, SpringPurifyCap = 16f;
        public const float YetiDefMult = 1.25f;
        public const float FennecRankBonus = 1.5f;      // 속도 순위를 1.5칸 앞당김
        public const float SwampHealRatio = 0.04f;
        public const float CloverExtraFoodChance = 0.35f;
        public const float BeastClawMult = 1.5f;
        public const float IncensePurifyBonus = 15f;
        public const float GuardShellMult = 0.5f;
    }
}

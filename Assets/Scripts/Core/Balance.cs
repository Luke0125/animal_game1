namespace FedAndFound.Core
{
    /// <summary>모든 제안 수치를 한 곳에 모은다. 플레이테스트 후 여기만 고치면 된다. (GDD §20 미정 사항 대부분)</summary>
    public static class Balance
    {
        // 스테이지별 지형 (0→3). 무작위로 바꾸려면 RunState.BeginStage 수정
        public static readonly Terrain[] StageTerrains = { Terrain.Plains, Terrain.Swamp, Terrain.SnowMountain, Terrain.Desert };

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
        public const float MobHpMult = 0.6f, MobAtkMult = 0.6f;
        public const float BossHpMult = 2.2f, BossAtkMult = 1.1f;
        // 보스 규모 보정: 배율 = Base + PerAlly × 파티 수 (파티 3→2→1로 줄어드는 구조 대응)
        public const float BossHpBase = 0.2f, BossHpPerAlly = 0.25f;
        public const float BossAtkBase = 1f, BossAtkPerAlly = 0f;
        public const float BossGrowthPerStage = 0.03f;    // 보스 추가 성장: 1스테이지 +0, 2스테이지 +N, 3스테이지 +2N (HP·ATK 모두)
        public const float EnemyScalePerStage = 0.1f;
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

        // 원석 시너지 (§4.5, §20-8 팀 확정안) — 수치는 제안값
        public const float PackHuntAtkPerCarnivore = 0.08f;  // 무리사냥: 육식 1마리당 아군 전체 ATK +8%
        public const int CycleHungerRestore = 4;             // 생명의 순환: 초식 정화 성공마다 아군 전체 배고픔 +4
        public const float AdaptPurify = 10f;                // 적응 초식+잡식
        public const float AdaptAtk = 0.12f;                 // 적응 육식+잡식
        public const float AdaptAllAtk = 0.06f, AdaptAllPurify = 5f; // 적응 셋 다
        public const float AdaptSoloAtk = 0.12f, AdaptSoloPurify = 8f; // 적응 잡식뿐: 적 HP 50% 기준

        // 홀로서기 (6단계): 전투에서 혼자 남은 동물의 "궁지 본능" ATK·DEF 보너스 — 소/중/대
        public static readonly float[] SoloDesperationBySize = { 0.5f, 0.25f, 0f };

        // 적 스킬 AI (5단계) — 적은 배고픔이 없으므로 확률로 스킬을 쓴다
        public const float EnemySkillChance = 0.3f, BossSkillChance = 0.45f;
        public const float BossSmashChance = 0.3f, BossSmashCoef = 1.5f; // 스킬을 안 쓸 때 보스 강타
        public const float EnemySustainHpThreshold = 0.6f;   // 적 유지형은 HP가 이 비율 아래로 떨어지면 켠다
        public const float EnemySootheHealRatio = 0.15f;     // 적 토끼 달래기 = 다친 적 1명 회복
        public const float EnemyFrenzyAtkMult = 1.3f, EnemyFrenzyHpThreshold = 0.5f; // 적 사자 광폭
    }
}

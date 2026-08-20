namespace Game.GamePlay.Attack
{
    public enum DirectAttackAdvanceMode
    {
        Stay,
        TargetStart,
        BeforeTarget
    }

    public readonly struct DirectAttackRule
    {
        public int PushDistance { get; }
        public DirectAttackAdvanceMode AdvanceMode { get; }
        public int SelfHealAmount { get; }
        public int AreaDamage { get; }
        public int AreaPushDistance { get; }
        public bool AreaCenteredOnAttacker { get; }

        public bool HasAreaEffect =>
            AreaDamage > 0 || AreaPushDistance > 0;

        public DirectAttackRule(
            int pushDistance,
            DirectAttackAdvanceMode advanceMode,
            int selfHealAmount = 0,
            int areaDamage = 0,
            int areaPushDistance = 0,
            bool areaCenteredOnAttacker = false)
        {
            PushDistance = pushDistance;
            AdvanceMode = advanceMode;
            SelfHealAmount = selfHealAmount;
            AreaDamage = areaDamage;
            AreaPushDistance = areaPushDistance;
            AreaCenteredOnAttacker = areaCenteredOnAttacker;
        }
    }
}
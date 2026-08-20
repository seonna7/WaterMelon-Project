namespace Game.GamePlay.Attack
{
    public sealed class DirectAttackRuleResolver
    {
        public DirectAttackRule GetRule(
            ChessPiece attacker)
        {
            if (attacker == null)
                return CreateDefault();

            switch (attacker.ActionType)
            {
                case ActionType.Pacemaker:
                    return new DirectAttackRule(
                        pushDistance: 2,
                        advanceMode:
                            DirectAttackAdvanceMode.TargetStart,
                        selfHealAmount: 2
                    );

                case ActionType.Observer:
                    return new DirectAttackRule(
                        pushDistance: 1,
                        advanceMode:
                            DirectAttackAdvanceMode.Stay
                    );

                case ActionType.Sweeper:
                    return new DirectAttackRule(
                        pushDistance: 1,
                        advanceMode:
                            DirectAttackAdvanceMode.BeforeTarget
                    );

                case ActionType.Vagabond:
                    return new DirectAttackRule(
                        pushDistance: 2,
                        advanceMode:
                            DirectAttackAdvanceMode.TargetStart,
                        areaPushDistance: 1,
                        areaCenteredOnAttacker: true
                    );

                case ActionType.Mastermind:
                    return new DirectAttackRule(
                        pushDistance: 0,
                        advanceMode:
                            DirectAttackAdvanceMode.BeforeTarget,
                        areaDamage: 2,
                        areaPushDistance: 1,
                        areaCenteredOnAttacker: false
                    );

                case ActionType.Backstabber:
                    return new DirectAttackRule(
                        pushDistance: 1,
                        advanceMode:
                            DirectAttackAdvanceMode.Stay
                    );

                default:
                    return CreateDefault();
            }
        }

        private static DirectAttackRule CreateDefault()
        {
            return new DirectAttackRule(
                pushDistance: 1,
                advanceMode:
                    DirectAttackAdvanceMode.TargetStart
            );
        }
    }
}
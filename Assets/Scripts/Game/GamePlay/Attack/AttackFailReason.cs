namespace Game.GamePlay.Attack
{
    public enum AttackFailReason
    {
        None,

        GridManagerIsNull,

        AttackerIsNull,

        TargetIsNull,

        SamePiece,

        AttackerIsDead,

        TargetIsDead,

        AttackerIsNotPlaced,

        TargetIsNotPlaced,

        AttackerIsMoving,

        SameTeam,

        TargetPositionMismatch,

        TargetOutOfAttackRange,

        AttackExecutionFailed,

        TargetRemovalFailed
    }
}

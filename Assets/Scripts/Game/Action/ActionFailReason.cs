namespace Game
{
    namespace Action
    {
        public enum ActionFailReason
        {
            None = 0,

            InvalidPhase = 1,

            NotPlayersTurn = 2,

            PieceIsNull = 3,

            PieceIsDead = 4,

            InvalidTargetPosition = 5,

            NotEnoughGem = 6,

            InvalidMove = 7,

            InvalidAttack = 8,

            SameTeamTarget = 9,

            NoTarget = 10
        }
    }
}

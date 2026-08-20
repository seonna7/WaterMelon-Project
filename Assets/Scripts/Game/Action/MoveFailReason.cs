namespace Game
{
    namespace Action
    {
        public enum MoveFailReason
        {
            None = 0,

            PieceIsNull = 1,

            PieceIsDead = 2,

            InvalidPosition = 3,

            NotInsideBoard = 4,

            TargetNotWalkable = 5,

            TargetOccupied = 6,

            InvalidMovePattern = 7
        }
    }
}

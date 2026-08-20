namespace Game
{
    namespace Action
    {
        public enum PushFailReason
        {
            None = 0,

            TargetIsNull = 1,

            InvalidBoardState = 2,

            TargetAlreadyDead = 3
        }
    }
}

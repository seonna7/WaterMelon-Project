using UnityEngine;

namespace Game
{
    namespace Action
    {
        public struct MoveResult
        {
            public bool Success;

            public MoveFailReason FailReason;

            public Vector2Int From;

            public Vector2Int To;

            public static MoveResult CreateSuccess(Vector2Int from, Vector2Int to)
            {
                return new MoveResult
                {
                    Success = true,
                    FailReason = MoveFailReason.None,
                    From = from,
                    To = to
                };
            }

            public static MoveResult CreateFail(MoveFailReason failReason, Vector2Int from, Vector2Int to)
            {
                return new MoveResult
                {
                    Success = false,
                    FailReason = failReason,
                    From = from,
                    To = to
                };
            }
        }
    }
}

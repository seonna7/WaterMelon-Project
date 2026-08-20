using Game.GamePlay;
using UnityEngine;

namespace Game.Action
{
    public struct PushResult
    {
        public bool Success;

        public ChessPiece Target;

        public Vector2Int StartPosition;

        public Vector2Int EndPosition;

        public int RequestedDistance;

        public int MovedDistance;

        public bool PushedOut;

        public bool Blocked;

        public static PushResult CreateSuccess(
            ChessPiece target,
            Vector2Int startPosition,
            Vector2Int endPosition,
            int requestedDistance,
            int movedDistance,
            bool pushedOut,
            bool blocked)
        {
            return new PushResult
            {
                Success = true,
                Target = target,

                StartPosition = startPosition,
                EndPosition = endPosition,

                RequestedDistance = requestedDistance,
                MovedDistance = movedDistance,

                PushedOut = pushedOut,
                Blocked = blocked
            };
        }

        public static PushResult CreateFail(
            ChessPiece target,
            Vector2Int position,
            int requestedDistance)
        {
            return new PushResult
            {
                Success = false,
                Target = target,

                StartPosition = position,
                EndPosition = position,

                RequestedDistance = requestedDistance,
                MovedDistance = 0,

                PushedOut = false,
                Blocked = false
            };
        }
    }
}

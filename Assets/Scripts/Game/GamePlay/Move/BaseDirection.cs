using UnityEngine;

namespace Game
{
    namespace GamePlay
    {
        public enum BaseDirection
        {
            Up = 0,

            Down = 1,

            Left = 2,

            Right = 3
        }

        public static class BaseDirectionExtensions
        {
            public static Vector2Int ToVector2Int(this BaseDirection dir)
            {
                switch (dir)
                {
                    case BaseDirection.Up:
                        return Vector2Int.up;

                    case BaseDirection.Down:
                        return Vector2Int.down;

                    case BaseDirection.Left:
                        return Vector2Int.left;

                    case BaseDirection.Right:
                        return Vector2Int.right;

                    default:
                        return Vector2Int.zero;
                }
            }
        }
    }
}

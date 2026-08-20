using Game.GamePlay.Grid;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay.Fog
{
    /*
     * 플레이어와 AI가 공통으로 사용하는
     * 시야 판정 인터페이스다.
     */
    public interface IVisionSystem
    {
        bool CanSee(
            ChessPiece observer,
            ChessPiece target);

        bool CanSeePosition(
            ChessPiece observer,
            Vector2Int targetPosition);

        List<Vector2Int> GetVisiblePositions(
            ChessPiece observer);
    }

    /*
     * 유닛의 시야 거리와 장애물 차단을 계산한다.
     *
     * 현재 지원:
     * - 기본 시야 거리
     * - 맨해튼 거리 또는 원형 거리
     * - 그리드 범위 검사
     * - 장애물에 의한 시야 차단
     *
     * 추후 연결:
     * - BushStealthResolver
     * - FogOfWarSystem
     * - 유닛별 시야 거리
     * - 노출/은신 상태효과
     */
    public sealed class VisionSystem : MonoBehaviour,
        IVisionSystem
    {
        [Header("References")]
        [SerializeField]
        private GridManager gridManager;

        [Header("Vision Range")]
        [SerializeField]
        [Min(0)]
        private int defaultVisionRange = 4;

        [Tooltip(
            "켜면 원형에 가까운 유클리드 거리, " +
            "끄면 십자 형태의 맨해튼 거리를 사용합니다."
        )]
        [SerializeField]
        private bool useEuclideanDistance = true;

        [Header("Line Of Sight")]
        [SerializeField]
        private bool useLineOfSight = true;

        [Tooltip(
            "관찰자가 서 있는 칸의 시야 차단 속성을 " +
            "무시합니다."
        )]
        [SerializeField]
        private bool ignoreObserverCell = true;

        [Tooltip(
            "대상 칸 자체가 시야 차단 지형이어도 " +
            "그 칸까지는 보이도록 합니다."
        )]
        [SerializeField]
        private bool allowBlockedTargetCell = true;

        /*
         * 부쉬나 은신 효과의 최종 판정을
         * 외부 시스템에 위임한다.
         *
         * BushStealthResolver가 작성되면 연결한다.
         */
        private System.Func<
            ChessPiece,
            ChessPiece,
            bool> targetVisibilityRule;

        /*
         * 특정 위치에 대한 추가 가시 판정이다.
         *
         * Fog 또는 특수 지형 시스템에서 사용할 수 있다.
         */
        private System.Func<
            ChessPiece,
            Vector2Int,
            bool> positionVisibilityRule;

        private readonly List<Vector2Int>
            visiblePositionBuffer = new();

        private void Awake()
        {
            if (gridManager == null)
            {
                gridManager =
                    FindFirstObjectByType<
                        GridManager>();
            }
        }

        public bool CanSee(
            ChessPiece observer,
            ChessPiece target)
        {
            if (!IsValidObserver(observer) ||
                !IsValidTarget(observer, target))
            {
                return false;
            }

            if (!CanSeePosition(
                    observer,
                    target.GridPosition))
            {
                return false;
            }

            /*
             * 거리와 장애물 판정을 통과한 뒤
             * 부쉬·은신 규칙을 적용한다.
             */
            if (targetVisibilityRule != null &&
                !targetVisibilityRule.Invoke(
                    observer,
                    target))
            {
                return false;
            }

            return true;
        }

        public bool CanSeePosition(
            ChessPiece observer,
            Vector2Int targetPosition)
        {
            if (!IsValidObserver(observer) ||
                gridManager == null ||
                !gridManager.IsInsideGrid(
                    targetPosition))
            {
                return false;
            }

            Vector2Int observerPosition =
                observer.GridPosition;

            if (!IsInsideVisionRange(
                    observerPosition,
                    targetPosition,
                    GetVisionRange(observer)))
            {
                return false;
            }

            if (useLineOfSight &&
                !HasLineOfSight(
                    observerPosition,
                    targetPosition))
            {
                return false;
            }

            if (positionVisibilityRule != null &&
                !positionVisibilityRule.Invoke(
                    observer,
                    targetPosition))
            {
                return false;
            }

            return true;
        }

        public List<Vector2Int>
            GetVisiblePositions(
                ChessPiece observer)
        {
            visiblePositionBuffer.Clear();

            if (!IsValidObserver(observer) ||
                gridManager == null)
            {
                return new List<Vector2Int>();
            }

            int visionRange =
                GetVisionRange(observer);

            Vector2Int center =
                observer.GridPosition;

            int minimumX =
                Mathf.Max(
                    0,
                    center.x - visionRange
                );

            int maximumX =
                Mathf.Min(
                    gridManager.GridWidth - 1,
                    center.x + visionRange
                );

            int minimumY =
                Mathf.Max(
                    0,
                    center.y - visionRange
                );

            int maximumY =
                Mathf.Min(
                    gridManager.GridHeight - 1,
                    center.y + visionRange
                );

            for (int x = minimumX;
                 x <= maximumX;
                 x++)
            {
                for (int y = minimumY;
                     y <= maximumY;
                     y++)
                {
                    Vector2Int position =
                        new Vector2Int(x, y);

                    if (!CanSeePosition(
                            observer,
                            position))
                    {
                        continue;
                    }

                    visiblePositionBuffer.Add(
                        position
                    );
                }
            }

            /*
             * 내부 버퍼가 외부에서 변경되지 않도록
             * 복사본을 반환한다.
             */
            return new List<Vector2Int>(
                visiblePositionBuffer
            );
        }

        /*
         * 현재는 모든 유닛이 기본 시야 거리를 사용한다.
         *
         * ChessPiece에 VisionRange가 추가되면
         * 해당 값을 반환하도록 수정하면 된다.
         */
        public int GetVisionRange(
            ChessPiece observer)
        {
            if (observer == null)
                return 0;

            return Mathf.Max(
                0,
                defaultVisionRange
            );
        }

        public void SetDefaultVisionRange(
            int visionRange)
        {
            defaultVisionRange =
                Mathf.Max(
                    0,
                    visionRange
                );
        }

        /*
         * BushStealthResolver 연결용이다.
         *
         * 예:
         *
         * visionSystem.SetTargetVisibilityRule(
         *     bushStealthResolver.IsTargetVisible
         * );
         */
        public void SetTargetVisibilityRule(
            System.Func<
                ChessPiece,
                ChessPiece,
                bool> visibilityRule)
        {
            targetVisibilityRule =
                visibilityRule;
        }

        public void ClearTargetVisibilityRule()
        {
            targetVisibilityRule = null;
        }

        public void SetPositionVisibilityRule(
            System.Func<
                ChessPiece,
                Vector2Int,
                bool> visibilityRule)
        {
            positionVisibilityRule =
                visibilityRule;
        }

        public void ClearPositionVisibilityRule()
        {
            positionVisibilityRule = null;
        }

        private bool IsInsideVisionRange(
            Vector2Int observerPosition,
            Vector2Int targetPosition,
            int visionRange)
        {
            if (useEuclideanDistance)
            {
                Vector2Int difference =
                    targetPosition -
                    observerPosition;

                int squaredDistance =
                    difference.x *
                    difference.x +
                    difference.y *
                    difference.y;

                return squaredDistance <=
                       visionRange *
                       visionRange;
            }

            int manhattanDistance =
                Mathf.Abs(
                    observerPosition.x -
                    targetPosition.x
                ) +
                Mathf.Abs(
                    observerPosition.y -
                    targetPosition.y
                );

            return manhattanDistance <=
                   visionRange;
        }

        /*
         * Bresenham 선 알고리즘으로
         * 관찰자와 목표 사이의 셀을 검사한다.
         */
        private bool HasLineOfSight(
            Vector2Int start,
            Vector2Int end)
        {
            if (start == end)
                return true;

            int x = start.x;
            int y = start.y;

            int deltaX =
                Mathf.Abs(
                    end.x - start.x
                );

            int deltaY =
                Mathf.Abs(
                    end.y - start.y
                );

            int stepX =
                start.x < end.x
                    ? 1
                    : -1;

            int stepY =
                start.y < end.y
                    ? 1
                    : -1;

            int error =
                deltaX - deltaY;

            while (true)
            {
                Vector2Int current =
                    new Vector2Int(
                        x,
                        y
                    );

                bool isStart =
                    current == start;

                bool isEnd =
                    current == end;

                bool shouldCheckCell =
                    true;

                if (isStart &&
                    ignoreObserverCell)
                {
                    shouldCheckCell = false;
                }

                if (isEnd &&
                    allowBlockedTargetCell)
                {
                    shouldCheckCell = false;
                }

                if (shouldCheckCell &&
                    gridManager.BlocksVision(
                        current))
                {
                    return false;
                }

                if (isEnd)
                    break;

                int doubledError =
                    error * 2;

                if (doubledError >
                    -deltaY)
                {
                    error -= deltaY;
                    x += stepX;
                }

                if (doubledError <
                    deltaX)
                {
                    error += deltaX;
                    y += stepY;
                }
            }

            return true;
        }

        private static bool IsValidObserver(
            ChessPiece observer)
        {
            return observer != null &&
                   !observer.IsDead &&
                   observer.IsPlaced;
        }

        private static bool IsValidTarget(
            ChessPiece observer,
            ChessPiece target)
        {
            return target != null &&
                   !target.IsDead &&
                   target.IsPlaced &&
                   target != observer;
        }
    }
}
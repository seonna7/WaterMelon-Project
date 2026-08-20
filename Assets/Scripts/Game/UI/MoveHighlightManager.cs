using Game.GamePlay.Grid;
using System.Collections.Generic;
using UnityEngine;

namespace Game.UI
{
    public class MoveHighlightManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private GridManager grid;

        [SerializeField]
        private GridRenderer gridRenderer;

        /*
         * 전체 하이라이트 위치
         */
        private readonly HashSet<Vector2Int>
            highlightedPositions = new();

        /*
         * 일반 이동 위치
         */
        private readonly HashSet<Vector2Int>
            movePositions = new();

        /*
         * 직접공격 위치
         */
        private readonly HashSet<Vector2Int>
            attackPositions = new();

        private readonly List<GridHighlightData>
            highlightData = new();

        public IReadOnlyCollection<Vector2Int>
            HighlightedPositions =>
                highlightedPositions;

        public IReadOnlyCollection<Vector2Int>
            MovePositions =>
                movePositions;

        public IReadOnlyCollection<Vector2Int>
            AttackPositions =>
                attackPositions;

        private void Awake()
        {
            if (grid == null)
            {
                grid =
                    FindFirstObjectByType<
                        GridManager>();
            }

            if (gridRenderer == null)
            {
                gridRenderer =
                    FindFirstObjectByType<
                        GridRenderer>();
            }
        }

        /*
         * 기존 코드 호환용.
         *
         * 이 함수를 호출하면 전달된 모든 위치를
         * 일반 이동 위치로 처리한다.
         */
        public void ShowHighlights(
            IReadOnlyList<Vector2Int> positions)
        {
            ClearHighlights();

            if (positions == null ||
                grid == null ||
                gridRenderer == null)
            {
                return;
            }

            for (int i = 0;
                 i < positions.Count;
                 i++)
            {
                AddMoveHighlight(
                    positions[i]
                );
            }

            ApplyHighlights();
        }

        /*
         * 이동 위치와 직접공격 위치를
         * 별도로 전달받는 새 함수.
         */
        public void ShowHighlights(
            IEnumerable<Vector2Int> moves,
            IEnumerable<Vector2Int> attacks)
        {
            ClearHighlights();

            if (grid == null ||
                gridRenderer == null)
            {
                return;
            }

            /*
             * 공격 위치를 먼저 등록한다.
             *
             * 동일 위치가 Move와 Attack 양쪽에 있다면
             * Attack 표시를 우선하기 위해서다.
             */
            if (attacks != null)
            {
                foreach (Vector2Int position
                         in attacks)
                {
                    AddAttackHighlight(
                        position
                    );
                }
            }

            if (moves != null)
            {
                foreach (Vector2Int position
                         in moves)
                {
                    /*
                     * 직접공격 위치가 이미 등록된 경우
                     * 이동 하이라이트를 추가하지 않는다.
                     */
                    if (attackPositions.Contains(
                            position))
                    {
                        continue;
                    }

                    AddMoveHighlight(
                        position
                    );
                }
            }

            ApplyHighlights();
        }

        private void AddMoveHighlight(
            Vector2Int position)
        {
            if (grid == null)
                return;

            if (!grid.IsInsideGrid(
                    position))
            {
                return;
            }

            if (!highlightedPositions.Add(
                    position))
            {
                return;
            }

            movePositions.Add(
                position
            );

            /*
             * 현재 프로젝트에서 기존 이동 표시가
             * ValidSkill 타입을 사용하고 있으므로
             * 그대로 유지한다.
             */
            highlightData.Add(
                new GridHighlightData(
                    position,
                    GridHighlightType.ValidSkill
                )
            );
        }

        private void AddAttackHighlight(
            Vector2Int position)
        {
            if (grid == null)
                return;

            if (!grid.IsInsideGrid(
                    position))
            {
                return;
            }

            /*
             * 이미 이동칸으로 등록되어 있었다면
             * 공격칸을 우선한다.
             */
            if (movePositions.Remove(
                    position))
            {
                highlightedPositions.Remove(
                    position
                );

                for (int i =
                         highlightData.Count - 1;
                     i >= 0;
                     i--)
                {
                    if (highlightData[i]
                            .GridPosition ==
                        position)
                    {
                        highlightData.RemoveAt(
                            i
                        );
                    }
                }
            }

            if (!highlightedPositions.Add(
                    position))
            {
                return;
            }

            attackPositions.Add(
                position
            );

            /*
             * 공격 가능 위치.
             *
             * 현재 GridHighlightType 중
             * ValidSkill과 다른 표현을 위해
             * InvalidSkill을 사용한다.
             *
             * GridRenderer에서 InvalidSkill을
             * 빨간색 머티리얼로 설정하면 된다.
             */
            highlightData.Add(
                new GridHighlightData(
                    position,
                    GridHighlightType.InvalidSkill
                )
            );
        }

        private void ApplyHighlights()
        {
            gridRenderer?
                .SetHighlights(
                    highlightData
                );

            Debug.Log(
                $"[MoveHighlightManager] " +
                $"Highlight | " +
                $"Move={movePositions.Count} | " +
                $"Attack={attackPositions.Count}"
            );
        }

        public void ClearHighlights()
        {
            highlightedPositions.Clear();
            movePositions.Clear();
            attackPositions.Clear();
            highlightData.Clear();

            gridRenderer?
                .ClearHighlights();
        }

        public bool IsHighlightedPosition(
            Vector2Int position)
        {
            return highlightedPositions.Contains(
                position
            );
        }

        public bool IsMovePosition(
            Vector2Int position)
        {
            return movePositions.Contains(
                position
            );
        }

        public bool IsAttackPosition(
            Vector2Int position)
        {
            return attackPositions.Contains(
                position
            );
        }
    }
}
using Game.Core;
using Game.GamePlay.Grid;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay.Fog
{
    public enum FogVisibilityState
    {
        Unexplored,
        Explored,
        Visible
    }

    /*
     * AI가 시야 정보를 사용하는 방식이다.
     *
     * Individual:
     * 각 유닛은 자신이 직접 본 정보만 사용한다.
     *
     * TeamShared:
     * 같은 팀 유닛들의 시야 정보를 모두 공유한다.
     */
    public enum AIVisibilityMode
    {
        Individual,
        TeamShared
    }

    public sealed class FogOfWarSystem
        : MonoBehaviour
    {
        /*
         * 팀 전체가 공유하는 시야 데이터다.
         */
        private sealed class TeamFogData
        {
            public readonly HashSet<Vector2Int>
                VisiblePositions = new();

            public readonly HashSet<Vector2Int>
                ExploredPositions = new();

            public void ClearCurrentVisibility()
            {
                VisiblePositions.Clear();
            }

            public void ClearAll()
            {
                VisiblePositions.Clear();
                ExploredPositions.Clear();
            }
        }

        /*
         * 유닛 하나가 직접 확보한 시야 데이터다.
         *
         * Individual AI에서 사용한다.
         */
        private sealed class ObserverFogData
        {
            public ChessPiece Observer;

            public readonly HashSet<Vector2Int>
                VisiblePositions = new();

            public readonly HashSet<Vector2Int>
                ExploredPositions = new();

            public void ClearCurrentVisibility()
            {
                VisiblePositions.Clear();
            }

            public void ClearAll()
            {
                VisiblePositions.Clear();
                ExploredPositions.Clear();
            }
        }

        [Header("References")]
        [SerializeField]
        private GridManager gridManager;

        [SerializeField]
        private VisionSystem visionSystem;

        [SerializeField]
        private GameManager gameManager;

        [Header("AI Visibility")]
        [SerializeField]
        private AIVisibilityMode aiVisibilityMode =
            AIVisibilityMode.Individual;

        [Tooltip(
            "개별 AI 시야에서도 과거에 탐색한 위치를 " +
            "유닛별로 기억할지 결정합니다."
        )]
        [SerializeField]
        private bool individualAIKeepsExploredData = true;

        [Header("Update")]
        [SerializeField]
        private bool refreshEveryFrame;

        [SerializeField]
        private bool refreshOnStart = true;

        [Header("Debug")]
        [SerializeField]
        private bool logRefreshResult;

        /*
         * White와 Black 각각의 팀 공유 시야다.
         */
        private readonly Dictionary<
            PieceColor,
            TeamFogData> teamFogData =
                new();

        /*
         * 각 유닛이 직접 확보한 개별 시야다.
         */
        private readonly Dictionary<
            ChessPiece,
            ObserverFogData> observerFogData =
                new();

        private readonly List<ChessPiece>
            pieceBuffer = new();

        private readonly List<ChessPiece>
            observerCleanupBuffer = new();

        public AIVisibilityMode AIVisibilityMode =>
            aiVisibilityMode;

        public event System.Action<
            PieceColor> TeamVisibilityChanged;

        public event System.Action<
            ChessPiece> ObserverVisibilityChanged;

        private void Awake()
        {
            ResolveReferences();
            InitializeTeamData();
        }

        private void Start()
        {
            if (refreshOnStart)
            {
                RefreshAllVisibility();
            }
        }

        private void Update()
        {
            if (!refreshEveryFrame)
                return;

            RefreshAllVisibility();
        }

        private void ResolveReferences()
        {
            if (gridManager == null)
            {
                gridManager =
                    FindFirstObjectByType<
                        GridManager>();
            }

            if (visionSystem == null)
            {
                visionSystem =
                    FindFirstObjectByType<
                        VisionSystem>();
            }

            if (gameManager == null)
            {
                gameManager =
                    FindFirstObjectByType<
                        GameManager>();
            }
        }

        private void InitializeTeamData()
        {
            EnsureTeamData(
                PieceColor.White
            );

            EnsureTeamData(
                PieceColor.Black
            );
        }

        #region Refresh

        /*
         * 팀 공유 시야와 모든 유닛의 개별 시야를
         * 한 번에 갱신한다.
         */
        public void RefreshAllVisibility()
        {
            RefreshTeamVisibility(
                PieceColor.White
            );

            RefreshTeamVisibility(
                PieceColor.Black
            );

            CleanupInvalidObserverData();
        }

        /*
         * 이전 코드와의 호환성을 위한 메서드다.
         */
        public void RefreshAllTeams()
        {
            RefreshAllVisibility();
        }

        /*
         * 해당 팀의 모든 유닛 개별 시야를 먼저 갱신한 뒤,
         * 이 결과를 합쳐 팀 공유 시야를 만든다.
         */
        public void RefreshTeamVisibility(
            PieceColor teamColor)
        {
            if (gridManager == null ||
                visionSystem == null)
            {
                return;
            }

            TeamFogData teamData =
                EnsureTeamData(
                    teamColor
                );

            teamData.ClearCurrentVisibility();

            CollectAlivePlacedPieces(
                teamColor,
                pieceBuffer
            );

            for (int i = 0;
                 i < pieceBuffer.Count;
                 i++)
            {
                ChessPiece observer =
                    pieceBuffer[i];

                RefreshObserverVisibility(
                    observer,
                    notifyEvent: false
                );

                ObserverFogData observerData =
                    EnsureObserverData(
                        observer
                    );

                foreach (Vector2Int position
                         in observerData
                             .VisiblePositions)
                {
                    teamData.VisiblePositions.Add(
                        position
                    );

                    teamData.ExploredPositions.Add(
                        position
                    );
                }
            }

            TeamVisibilityChanged?.Invoke(
                teamColor
            );

            if (logRefreshResult)
            {
                Debug.Log(
                    $"[FogOfWarSystem] " +
                    $"Team Refresh | " +
                    $"Team={teamColor} | " +
                    $"Observers={pieceBuffer.Count} | " +
                    $"Visible=" +
                    $"{teamData.VisiblePositions.Count} | " +
                    $"Explored=" +
                    $"{teamData.ExploredPositions.Count}"
                );
            }
        }

        /*
         * 유닛 하나의 직접 시야만 갱신한다.
         */
        public void RefreshObserverVisibility(
            ChessPiece observer)
        {
            RefreshObserverVisibility(
                observer,
                notifyEvent: true
            );
        }

        private void RefreshObserverVisibility(
            ChessPiece observer,
            bool notifyEvent)
        {
            if (!IsValidObserver(observer) ||
                visionSystem == null ||
                gridManager == null)
            {
                RemoveObserverData(observer);
                return;
            }

            ObserverFogData observerData =
                EnsureObserverData(
                    observer
                );

            observerData.ClearCurrentVisibility();

            List<Vector2Int> visiblePositions =
                visionSystem.GetVisiblePositions(
                    observer
                );

            for (int i = 0;
                 i < visiblePositions.Count;
                 i++)
            {
                Vector2Int position =
                    visiblePositions[i];

                if (!gridManager.IsInsideGrid(
                        position))
                {
                    continue;
                }

                observerData.VisiblePositions.Add(
                    position
                );

                if (individualAIKeepsExploredData)
                {
                    observerData
                        .ExploredPositions
                        .Add(position);
                }
            }

            /*
             * 자기 위치는 반드시 보이는 것으로 처리한다.
             */
            observerData.VisiblePositions.Add(
                observer.GridPosition
            );

            if (individualAIKeepsExploredData)
            {
                observerData.ExploredPositions.Add(
                    observer.GridPosition
                );
            }

            if (notifyEvent)
            {
                ObserverVisibilityChanged?.Invoke(
                    observer
                );
            }

            if (logRefreshResult)
            {
                Debug.Log(
                    $"[FogOfWarSystem] " +
                    $"Observer Refresh | " +
                    $"Observer={observer.name} | " +
                    $"Visible=" +
                    $"{observerData.VisiblePositions.Count}"
                );
            }
        }

        #endregion

        #region Team Visibility

        public FogVisibilityState
            GetTeamVisibilityState(
                PieceColor viewerTeam,
                Vector2Int position)
        {
            if (gridManager == null ||
                !gridManager.IsInsideGrid(
                    position))
            {
                return FogVisibilityState
                    .Unexplored;
            }

            TeamFogData data =
                EnsureTeamData(
                    viewerTeam
                );

            if (data.VisiblePositions.Contains(
                    position))
            {
                return FogVisibilityState.Visible;
            }

            if (data.ExploredPositions.Contains(
                    position))
            {
                return FogVisibilityState.Explored;
            }

            return FogVisibilityState.Unexplored;
        }

        /*
         * 기존 코드와의 호환성용 메서드다.
         */
        public FogVisibilityState
            GetVisibilityState(
                PieceColor viewerTeam,
                Vector2Int position)
        {
            return GetTeamVisibilityState(
                viewerTeam,
                position
            );
        }

        public bool IsTeamPositionVisible(
            PieceColor viewerTeam,
            Vector2Int position)
        {
            return EnsureTeamData(
                viewerTeam
            ).VisiblePositions.Contains(
                position
            );
        }

        public bool IsPositionVisible(
            PieceColor viewerTeam,
            Vector2Int position)
        {
            return IsTeamPositionVisible(
                viewerTeam,
                position
            );
        }

        public bool IsTeamPositionExplored(
            PieceColor viewerTeam,
            Vector2Int position)
        {
            return EnsureTeamData(
                viewerTeam
            ).ExploredPositions.Contains(
                position
            );
        }

        public bool IsPositionExplored(
            PieceColor viewerTeam,
            Vector2Int position)
        {
            return IsTeamPositionExplored(
                viewerTeam,
                position
            );
        }

        /*
         * 같은 팀 유닛 중 하나라도 대상을 실제로 볼 수 있으면
         * 팀 전체가 대상을 발견한 것으로 처리한다.
         *
         * VisionSystem.CanSee()를 사용하므로
         * 장애물과 부쉬 은신까지 반영된다.
         */
        public bool CanTeamSeePiece(
            PieceColor viewerTeam,
            ChessPiece target)
        {
            if (!IsValidTarget(target) ||
                visionSystem == null)
            {
                return false;
            }

            CollectAlivePlacedPieces(
                viewerTeam,
                pieceBuffer
            );

            for (int i = 0;
                 i < pieceBuffer.Count;
                 i++)
            {
                ChessPiece observer =
                    pieceBuffer[i];

                if (visionSystem.CanSee(
                        observer,
                        target))
                {
                    return true;
                }
            }

            return false;
        }

        public IReadOnlyCollection<Vector2Int>
            GetTeamVisiblePositions(
                PieceColor teamColor)
        {
            return EnsureTeamData(
                teamColor
            ).VisiblePositions;
        }

        public IReadOnlyCollection<Vector2Int>
            GetTeamExploredPositions(
                PieceColor teamColor)
        {
            return EnsureTeamData(
                teamColor
            ).ExploredPositions;
        }

        #endregion

        #region Individual Visibility

        public FogVisibilityState
            GetObserverVisibilityState(
                ChessPiece observer,
                Vector2Int position)
        {
            if (!IsValidObserver(observer) ||
                gridManager == null ||
                !gridManager.IsInsideGrid(
                    position))
            {
                return FogVisibilityState
                    .Unexplored;
            }

            ObserverFogData data =
                EnsureObserverData(
                    observer
                );

            if (data.VisiblePositions.Contains(
                    position))
            {
                return FogVisibilityState.Visible;
            }

            if (data.ExploredPositions.Contains(
                    position))
            {
                return FogVisibilityState.Explored;
            }

            return FogVisibilityState.Unexplored;
        }

        public bool IsObserverPositionVisible(
            ChessPiece observer,
            Vector2Int position)
        {
            if (!IsValidObserver(observer))
                return false;

            return EnsureObserverData(
                observer
            ).VisiblePositions.Contains(
                position
            );
        }

        /*
         * 특정 유닛이 대상을 직접 볼 수 있는지 확인한다.
         *
         * 위치가 보인다는 것과 부쉬 속 유닛이 보인다는 것은
         * 다르므로 반드시 VisionSystem.CanSee()를 사용한다.
         */
        public bool CanObserverSeePiece(
            ChessPiece observer,
            ChessPiece target)
        {
            if (!IsValidObserver(observer) ||
                !IsValidTarget(target) ||
                visionSystem == null)
            {
                return false;
            }

            return visionSystem.CanSee(
                observer,
                target
            );
        }

        public IReadOnlyCollection<Vector2Int>
            GetObserverVisiblePositions(
                ChessPiece observer)
        {
            if (!IsValidObserver(observer))
            {
                return System.Array.Empty<
                    Vector2Int>();
            }

            return EnsureObserverData(
                observer
            ).VisiblePositions;
        }

        public IReadOnlyCollection<Vector2Int>
            GetObserverExploredPositions(
                ChessPiece observer)
        {
            if (!IsValidObserver(observer))
            {
                return System.Array.Empty<
                    Vector2Int>();
            }

            return EnsureObserverData(
                observer
            ).ExploredPositions;
        }

        #endregion

        #region AI Visibility Policy

        /*
         * 현재 설정된 AI 시야 정책에 따라
         * 대상 유닛을 볼 수 있는지 반환한다.
         *
         * EnemyPerception이 이 메서드를 사용하면
         * 개별 시야와 팀 공유 시야를 Inspector 설정으로
         * 전환할 수 있다.
         */
        public bool CanAISeePiece(
            ChessPiece observer,
            ChessPiece target)
        {
            if (!IsValidObserver(observer) ||
                !IsValidTarget(target))
            {
                return false;
            }

            switch (aiVisibilityMode)
            {
                case AIVisibilityMode.Individual:
                    return CanObserverSeePiece(
                        observer,
                        target
                    );

                case AIVisibilityMode.TeamShared:
                    return CanTeamSeePiece(
                        observer.Color,
                        target
                    );

                default:
                    return false;
            }
        }

        public bool CanAISeePosition(
            ChessPiece observer,
            Vector2Int position)
        {
            if (!IsValidObserver(observer))
                return false;

            switch (aiVisibilityMode)
            {
                case AIVisibilityMode.Individual:
                    return IsObserverPositionVisible(
                        observer,
                        position
                    );

                case AIVisibilityMode.TeamShared:
                    return IsTeamPositionVisible(
                        observer.Color,
                        position
                    );

                default:
                    return false;
            }
        }

        public IReadOnlyCollection<Vector2Int>
            GetAIVisiblePositions(
                ChessPiece observer)
        {
            if (!IsValidObserver(observer))
            {
                return System.Array.Empty<
                    Vector2Int>();
            }

            switch (aiVisibilityMode)
            {
                case AIVisibilityMode.Individual:
                    return GetObserverVisiblePositions(
                        observer
                    );

                case AIVisibilityMode.TeamShared:
                    return GetTeamVisiblePositions(
                        observer.Color
                    );

                default:
                    return System.Array.Empty<
                        Vector2Int>();
            }
        }

        public void SetAIVisibilityMode(
            AIVisibilityMode visibilityMode)
        {
            aiVisibilityMode =
                visibilityMode;
        }

        #endregion

        #region Clear

        public void ClearTeamFog(
            PieceColor teamColor)
        {
            EnsureTeamData(
                teamColor
            ).ClearAll();

            TeamVisibilityChanged?.Invoke(
                teamColor
            );
        }

        public void ClearObserverFog(
            ChessPiece observer)
        {
            if (observer == null)
                return;

            if (!observerFogData.TryGetValue(
                    observer,
                    out ObserverFogData data))
            {
                return;
            }

            data.ClearAll();

            ObserverVisibilityChanged?.Invoke(
                observer
            );
        }

        public void ClearAllFog()
        {
            foreach (TeamFogData data
                     in teamFogData.Values)
            {
                data.ClearAll();
            }

            foreach (ObserverFogData data
                     in observerFogData.Values)
            {
                data.ClearAll();
            }

            TeamVisibilityChanged?.Invoke(
                PieceColor.White
            );

            TeamVisibilityChanged?.Invoke(
                PieceColor.Black
            );
        }

        #endregion

        #region Data Management

        private TeamFogData EnsureTeamData(
            PieceColor teamColor)
        {
            if (teamFogData.TryGetValue(
                    teamColor,
                    out TeamFogData data))
            {
                return data;
            }

            data = new TeamFogData();

            teamFogData.Add(
                teamColor,
                data
            );

            return data;
        }

        private ObserverFogData
            EnsureObserverData(
                ChessPiece observer)
        {
            if (observerFogData.TryGetValue(
                    observer,
                    out ObserverFogData data))
            {
                return data;
            }

            data =
                new ObserverFogData
                {
                    Observer = observer
                };

            observerFogData.Add(
                observer,
                data
            );

            return data;
        }

        private void RemoveObserverData(
            ChessPiece observer)
        {
            if (observer == null)
                return;

            observerFogData.Remove(
                observer
            );
        }

        private void CleanupInvalidObserverData()
        {
            observerCleanupBuffer.Clear();

            foreach (KeyValuePair<
                         ChessPiece,
                         ObserverFogData> pair
                     in observerFogData)
            {
                ChessPiece observer =
                    pair.Key;

                if (!IsValidObserver(observer))
                {
                    observerCleanupBuffer.Add(
                        observer
                    );
                }
            }

            for (int i = 0;
                 i < observerCleanupBuffer.Count;
                 i++)
            {
                observerFogData.Remove(
                    observerCleanupBuffer[i]
                );
            }

            observerCleanupBuffer.Clear();
        }

        private static void
            CollectAlivePlacedPieces(
                PieceColor teamColor,
                List<ChessPiece> result)
        {
            result.Clear();

            ChessPiece[] scenePieces =
                FindObjectsByType<ChessPiece>(
                    FindObjectsSortMode.None
                );

            for (int i = 0;
                 i < scenePieces.Length;
                 i++)
            {
                ChessPiece piece =
                    scenePieces[i];

                if (!IsValidObserver(piece) ||
                    piece.Color != teamColor)
                {
                    continue;
                }

                result.Add(piece);
            }
        }

        private static bool IsValidObserver(
            ChessPiece observer)
        {
            return observer != null &&
                   !observer.IsDead &&
                   observer.IsPlaced;
        }

        private static bool IsValidTarget(
            ChessPiece target)
        {
            return target != null &&
                   !target.IsDead &&
                   target.IsPlaced;
        }

        #endregion
    }
}
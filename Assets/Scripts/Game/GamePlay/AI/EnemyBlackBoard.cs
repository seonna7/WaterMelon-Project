using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay.AI
{
    /*
     * 적 AI의 현재 행동 상태다.
     *
     * 추후 EnemyStateMachine이 이 상태를 관리한다.
     */
    public enum EnemyAIState
    {
        None,

        Idle,

        Patrol,

        Investigate,

        Search,

        Combat,

        Retreat,

        Guard,

        Disabled,

        Dead
    }

    /*
     * 하나의 적 유닛이 판단에 사용하는
     * 런타임 정보를 보관한다.
     *
     * 이 클래스는:
     *
     * - 시야를 직접 계산하지 않는다.
     * - 행동을 직접 결정하지 않는다.
     * - 행동을 직접 실행하지 않는다.
     *
     * 다른 AI 시스템이 계산한 결과를 저장하고
     * 공유하는 역할만 담당한다.
     */
    public sealed class EnemyBlackboard
    {
        private readonly List<ChessPiece>
            visibleOpponents = new();

        #region Core

        public ChessPiece Actor
        {
            get;
        }

        public EnemyMemory Memory
        {
            get;
        }

        public EnemyPerception Perception
        {
            get;
        }

        #endregion

        #region Turn

        public int CurrentTurnNumber
        {
            get;
            private set;
        }
        public EnemyPatrolRuntime PatrolRuntime
        {
            get;
            private set;
        }

        public bool HasPatrolRoute =>
            PatrolRuntime != null &&
            PatrolRuntime.HasRoute;

        public bool IsPatrolCompleted =>
            PatrolRuntime != null &&
            PatrolRuntime.IsCompleted;


        public PieceColor CurrentTurnColor
        {
            get;
            private set;
        }

        public bool IsActorsTurn =>
            Actor != null &&
            Actor.Color == CurrentTurnColor;

        #endregion

        #region State

        public EnemyAIState CurrentState
        {
            get;
            private set;
        }

        public EnemyAIState PreviousState
        {
            get;
            private set;
        }

        public int StateEnteredTurn
        {
            get;
            private set;
        }

        public int TurnsInCurrentState =>
            Mathf.Max(
                0,
                CurrentTurnNumber -
                StateEnteredTurn
            );

        #endregion

        #region Target

        public ChessPiece CurrentTarget
        {
            get;
            private set;
        }

        public Vector2Int TargetPosition
        {
            get;
            private set;
        }

        public bool HasCurrentTarget =>
            CurrentTarget != null &&
            !CurrentTarget.IsDead &&
            CurrentTarget.IsPlaced;

        public bool HasTargetPosition
        {
            get;
            private set;
        }

        public bool IsTargetCurrentlyVisible
        {
            get;
            private set;
        }

        public int LastTargetSeenTurn
        {
            get;
            private set;
        }

        #endregion

        #region Action

        public EnemyAIAction SelectedAction
        {
            get;
            private set;
        }

        public EnemyAIExecutionResult
            LastExecutionResult
        {
            get;
            private set;
        }

        public bool HasSelectedAction =>
            SelectedAction.IsValid;

        public bool LastActionSucceeded =>
            LastExecutionResult.Success;

        #endregion

        #region Runtime information

        public IReadOnlyList<ChessPiece>
            VisibleOpponents =>
                visibleOpponents;

        public bool HasVisibleOpponents =>
            visibleOpponents.Count > 0;

        public float HealthRatio
        {
            get;
            private set;
        }

        public float MissingHealthRatio =>
            1f - HealthRatio;

        public bool IsLowHealth
        {
            get;
            private set;
        }

        public int DistanceToCurrentTarget
        {
            get;
            private set;
        } = -1;

        public bool IsStunned
        {
            get;
            private set;
        }

        #endregion

        #region Configuration

        public float LowHealthThreshold
        {
            get;
            private set;
        }

        #endregion

        public EnemyBlackboard(
            ChessPiece actor,
            EnemyMemory memory,
            EnemyPerception perception,
            float lowHealthThreshold = 0.3f)
        {
            Actor = actor;

            Memory =
                memory ??
                new EnemyMemory();

            Perception =
                perception;

            LowHealthThreshold =
                Mathf.Clamp01(
                    lowHealthThreshold
                );

            CurrentState =
                EnemyAIState.Idle;

            PreviousState =
                EnemyAIState.None;

            SelectedAction =
                EnemyAIAction.CreateNone();

            TargetPosition = default;

            RefreshActorStatus();
        }

        /*
         * AI 판단 직전에 호출한다.
         *
         * EnemyPerception.UpdatePerception()이 먼저
         * 실행된 상태여야 한다.
         */
        public void Refresh(
            int currentTurnNumber,
            PieceColor currentTurnColor,
            bool isStunned = false)
        {
            CurrentTurnNumber =
                Mathf.Max(
                    0,
                    currentTurnNumber
                );

            CurrentTurnColor =
                currentTurnColor;

            IsStunned =
                isStunned;

            RefreshActorStatus();

            RefreshVisibleOpponents();

            RefreshTargetInformation();

            ValidateCurrentState();
        }

        /*
         * Actor의 체력과 생존 상태를 갱신한다.
         */
        private void RefreshActorStatus()
        {
            if (Actor == null ||
                Actor.MaxHP <= 0)
            {
                HealthRatio = 0f;
                IsLowHealth = true;

                return;
            }

            HealthRatio =
                Mathf.Clamp01(
                    (float)Actor.CurrentHP /
                    Actor.MaxHP
                );

            IsLowHealth =
                HealthRatio <=
                LowHealthThreshold;
        }

        /*
         * EnemyPerception이 계산한 현재 시야 대상을
         * Blackboard 내부 목록으로 복사한다.
         */
        private void RefreshVisibleOpponents()
        {
            visibleOpponents.Clear();

            if (Perception == null)
                return;

            IReadOnlyList<ChessPiece>
                perceivedOpponents =
                    Perception.VisibleOpponents;

            if (perceivedOpponents == null)
                return;

            for (int i = 0;
                 i < perceivedOpponents.Count;
                 i++)
            {
                ChessPiece target =
                    perceivedOpponents[i];

                if (!IsValidOpponent(target))
                    continue;

                if (!visibleOpponents.Contains(
                        target))
                {
                    visibleOpponents.Add(
                        target
                    );
                }
            }
        }

        /*
         * 현재 목표의 가시성, 위치, 거리를 갱신한다.
         */
        private void RefreshTargetInformation()
        {
            if (!HasCurrentTarget)
            {
                ClearInvalidTarget();
                return;
            }

            IsTargetCurrentlyVisible =
                Perception != null &&
                Perception.IsCurrentlyVisible(
                    CurrentTarget
                );

            if (IsTargetCurrentlyVisible)
            {
                TargetPosition =
                    CurrentTarget.GridPosition;

                HasTargetPosition = true;

                LastTargetSeenTurn =
                    CurrentTurnNumber;
            }
            else if (Memory != null &&
                     Memory.TryGetMemory(
                         CurrentTarget,
                         out EnemyMemory.TargetMemory
                             targetMemory))
            {
                TargetPosition =
                    targetMemory
                        .LastKnownPosition;

                HasTargetPosition = true;

                LastTargetSeenTurn =
                    targetMemory.LastSeenTurn;
            }

            DistanceToCurrentTarget =
                Actor != null &&
                HasTargetPosition
                    ? ManhattanDistance(
                        Actor.GridPosition,
                        TargetPosition
                    )
                    : -1;
        }

        /*
         * 현재 목표를 지정한다.
         */
        public bool SetTarget(
            ChessPiece target)
        {
            if (!IsValidOpponent(target))
                return false;

            CurrentTarget = target;

            TargetPosition =
                target.GridPosition;

            HasTargetPosition = true;

            IsTargetCurrentlyVisible =
                Perception != null &&
                Perception.IsCurrentlyVisible(
                    target
                );

            if (IsTargetCurrentlyVisible)
            {
                LastTargetSeenTurn =
                    CurrentTurnNumber;
            }
            else if (Memory != null &&
                     Memory.TryGetMemory(
                         target,
                         out EnemyMemory.TargetMemory
                             targetMemory))
            {
                TargetPosition =
                    targetMemory
                        .LastKnownPosition;

                LastTargetSeenTurn =
                    targetMemory.LastSeenTurn;
            }

            DistanceToCurrentTarget =
                Actor != null
                    ? ManhattanDistance(
                        Actor.GridPosition,
                        TargetPosition
                    )
                    : -1;

            return true;
        }

        /*
         * 유닛 참조 없이 조사할 위치만 지정한다.
         *
         * 마지막 목격 위치, 소음 위치,
         * 순찰 위치 등에 사용할 수 있다.
         */
        public void SetTargetPosition(
            Vector2Int position)
        {
            CurrentTarget = null;

            TargetPosition = position;

            HasTargetPosition = true;

            IsTargetCurrentlyVisible =
                false;

            DistanceToCurrentTarget =
                Actor != null
                    ? ManhattanDistance(
                        Actor.GridPosition,
                        position
                    )
                    : -1;
        }

        public void ClearTarget()
        {
            CurrentTarget = null;

            TargetPosition = default;

            HasTargetPosition = false;

            IsTargetCurrentlyVisible =
                false;

            LastTargetSeenTurn = 0;

            DistanceToCurrentTarget = -1;
        }

        private void ClearInvalidTarget()
        {
            if (CurrentTarget != null &&
                CurrentTarget.IsDead)
            {
                Memory?.ForgetTarget(
                    CurrentTarget
                );
            }

            CurrentTarget = null;

            IsTargetCurrentlyVisible =
                false;

            DistanceToCurrentTarget =
                HasTargetPosition &&
                Actor != null
                    ? ManhattanDistance(
                        Actor.GridPosition,
                        TargetPosition
                    )
                    : -1;
        }

        /*
         * AI 상태를 변경한다.
         */
        public bool ChangeState(
            EnemyAIState newState)
        {
            if (CurrentState == newState)
                return false;

            PreviousState =
                CurrentState;

            CurrentState =
                newState;

            StateEnteredTurn =
                CurrentTurnNumber;

            Debug.Log(
                $"[EnemyBlackboard] " +
                $"State Changed | " +
                $"Actor={GetActorName()} | " +
                $"Previous={PreviousState} | " +
                $"Current={CurrentState}"
            );

            return true;
        }

        /*
         * 현재 상태가 유닛 상태와 맞지 않으면
         * 강제로 정리한다.
         */
        private void ValidateCurrentState()
        {
            if (Actor == null ||
                Actor.IsDead)
            {
                ChangeState(
                    EnemyAIState.Dead
                );

                return;
            }

            if (IsStunned)
            {
                ChangeState(
                    EnemyAIState.Disabled
                );

                return;
            }

            if (CurrentState ==
                EnemyAIState.Dead)
            {
                return;
            }

            if (CurrentState ==
                    EnemyAIState.Disabled &&
                !IsStunned)
            {
                ChangeState(
                    HasVisibleOpponents
                        ? EnemyAIState.Combat
                        : EnemyAIState.Idle
                );
            }
        }

        public void SetSelectedAction(
            EnemyAIAction action)
        {
            SelectedAction = action;
        }

        public void ClearSelectedAction()
        {
            SelectedAction =
                EnemyAIAction.CreateNone();
        }

        public void SetExecutionResult(
            EnemyAIExecutionResult result)
        {
            LastExecutionResult = result;
        }

        public void SetLowHealthThreshold(
            float threshold)
        {
            LowHealthThreshold =
                Mathf.Clamp01(
                    threshold
                );

            RefreshActorStatus();
        }

        /*
         * AI가 제거되거나 새 게임을 시작할 때 호출한다.
         */
        public void ClearRuntimeData(
            bool clearMemory = false)
        {
            visibleOpponents.Clear();

            ClearTarget();

            ClearSelectedAction();

            LastExecutionResult = default;

            IsStunned = false;

            CurrentTurnNumber = 0;

            CurrentState =
                Actor != null &&
                Actor.IsDead
                    ? EnemyAIState.Dead
                    : EnemyAIState.Idle;

            PreviousState =
                EnemyAIState.None;

            StateEnteredTurn = 0;

            if (clearMemory)
            {
                Memory?.Clear();
            }
        }

        private bool IsValidOpponent(
            ChessPiece target)
        {
            return Actor != null &&
                   target != null &&
                   Actor != target &&
                   !Actor.IsDead &&
                   Actor.IsPlaced &&
                   !target.IsDead &&
                   target.IsPlaced &&
                   Actor.Color !=
                   target.Color;
        }

        private string GetActorName()
        {
            return Actor != null
                ? Actor.name
                : "None";
        }

        private static int ManhattanDistance(
            Vector2Int first,
            Vector2Int second)
        {
            return Mathf.Abs(
                       first.x - second.x
                   ) +
                   Mathf.Abs(
                       first.y - second.y
                   );
        }

        public bool RefreshPatrolTarget()
        {
            if (PatrolRuntime == null ||
                !PatrolRuntime.HasRoute ||
                PatrolRuntime.IsCompleted)
            {
                return false;
            }

            if (PatrolRuntime.IsWaiting)
            {
                return false;
            }

            if (!PatrolRuntime
                    .TryGetCurrentPosition(
                        out Vector2Int position))
            {
                return false;
            }

            SetTargetPosition(
                position
            );

            return true;
        }
        public void SetPatrolRoute(
    EnemyPatrolRoute route)
        {
            if (route == null ||
                !route.HasValidRoute)
            {
                PatrolRuntime = null;
                return;
            }

            PatrolRuntime =
                new EnemyPatrolRuntime(
                    route
                );
        }
    }
}
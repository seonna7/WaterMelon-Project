using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay.AI
{
    /*
     * EnemyBlackboard의 정보를 바탕으로
     * 적 AI의 현재 상태를 결정한다.
     *
     * 이 클래스는:
     *
     * - 시야를 계산하지 않는다.
     * - 행동을 실행하지 않는다.
     * - 이동 위치를 계산하지 않는다.
     *
     * 오직 현재 상황에 적절한 AI 상태를
     * 선택하고 Blackboard에 반영한다.
     */
    public sealed class EnemyStateMachine
    {
        private readonly EnemyThreatAnalyzer
            threatAnalyzer;

        private readonly EnemyStateSettings
            settings;

        public EnemyStateMachine()
            : this(
                new EnemyThreatAnalyzer(),
                EnemyStateSettings.CreateDefault()
            )
        {
        }

        public EnemyStateMachine(
            EnemyThreatAnalyzer threatAnalyzer,
            EnemyStateSettings settings)
        {
            this.threatAnalyzer =
                threatAnalyzer ??
                new EnemyThreatAnalyzer();

            this.settings = settings;
        }

        /*
         * Blackboard를 평가하고
         * 현재 상태를 갱신한다.
         */
        public EnemyAIState UpdateState(
            EnemyBlackboard blackboard)
        {
            if (blackboard == null)
            {
                return EnemyAIState.None;
            }

            EnemyAIState nextState =
                DetermineState(
                    blackboard
                );

            blackboard.ChangeState(
                nextState
            );

            return nextState;
        }

        /*
         * 상태만 계산하고 Blackboard를 변경하지 않는다.
         *
         * 디버깅이나 사전 판단에 사용할 수 있다.
         */
        public EnemyAIState DetermineState(
            EnemyBlackboard blackboard)
        {
            if (blackboard == null)
            {
                return EnemyAIState.None;
            }

            ChessPiece actor =
                blackboard.Actor;

            /*
             * 가장 높은 우선순위:
             * 유닛이 없거나 사망한 상태
             */
            if (actor == null ||
                actor.IsDead)
            {
                return EnemyAIState.Dead;
            }

            /*
             * 기절 등으로 행동이 차단된 상태
             */
            if (blackboard.IsStunned)
            {
                return EnemyAIState.Disabled;
            }

            /*
             * 현재 보이는 상대를 우선 목표로 갱신한다.
             */
            if (blackboard.HasVisibleOpponents)
            {
                UpdateVisibleTarget(
                    blackboard
                );

                /*
                 * 체력이 낮고 적이 보이면
                 * 교전보다 후퇴를 우선한다.
                 */
                if (ShouldRetreat(
                        blackboard))
                {
                    return EnemyAIState.Retreat;
                }

                return EnemyAIState.Combat;
            }

            /*
             * 직접 보이는 적은 없지만
             * 기억에 남은 상대가 있다.
             */
            if (TryUpdateRememberedTarget(
                    blackboard))
            {
                /*
                 * 마지막 목격 위치에 도착했다면
                 * 주변 수색 상태로 전환한다.
                 */
                if (blackboard
                        .HasTargetPosition &&
                    blackboard
                        .DistanceToCurrentTarget <=
                    settings
                        .SearchArrivalDistance)
                {
                    return EnemyAIState.Search;
                }

                return EnemyAIState.Investigate;
            }

            /*
             * 현재 목표와 기억이 모두 없으면
             * 순찰 또는 대기 상태로 돌아간다.
             */
            blackboard.ClearTarget();

            if (settings.UsePatrolWhenNoTarget &&
                blackboard.HasPatrolRoute &&
                !blackboard.IsPatrolCompleted)
            {
                return EnemyAIState.Patrol;
            }

            return EnemyAIState.Idle;
        }

        /*
         * 현재 보이는 적 중 위협도가 가장 높은 대상을
         * Blackboard의 CurrentTarget으로 지정한다.
         */
        private void UpdateVisibleTarget(
            EnemyBlackboard blackboard)
        {
            IReadOnlyList<ChessPiece>
                visibleOpponents =
                    blackboard.VisibleOpponents;

            if (visibleOpponents == null ||
                visibleOpponents.Count == 0)
            {
                return;
            }

            if (threatAnalyzer
                    .TryGetHighestThreatTarget(
                        blackboard.Actor,
                        visibleOpponents,
                        out ChessPiece target,
                        out float threatScore))
            {
                blackboard.SetTarget(
                    target
                );

                if (settings.LogTargetChanges)
                {
                    Debug.Log(
                        $"[EnemyStateMachine] " +
                        $"Visible Target Selected | " +
                        $"Actor=" +
                        $"{GetPieceName(blackboard.Actor)} | " +
                        $"Target=" +
                        $"{GetPieceName(target)} | " +
                        $"Threat={threatScore:F2}"
                    );
                }

                return;
            }

            /*
             * 위협도 평가에 실패했을 때의
             * 안전한 기본 처리다.
             */
            for (int i = 0;
                 i < visibleOpponents.Count;
                 i++)
            {
                ChessPiece candidate =
                    visibleOpponents[i];

                if (!IsValidOpponent(
                        blackboard.Actor,
                        candidate))
                {
                    continue;
                }

                blackboard.SetTarget(
                    candidate
                );

                return;
            }
        }

        /*
         * 현재 보이는 적이 없을 때
         * Memory에 남아 있는 가장 가까운 상대를
         * 목표로 설정한다.
         */
        private bool TryUpdateRememberedTarget(
            EnemyBlackboard blackboard)
        {
            EnemyMemory memory =
                blackboard.Memory;

            ChessPiece actor =
                blackboard.Actor;

            if (memory == null ||
                actor == null)
            {
                return false;
            }

            if (!memory
                    .TryGetNearestRememberedTarget(
                        actor.GridPosition,
                        out EnemyMemory.TargetMemory
                            rememberedTarget))
            {
                return false;
            }

            if (rememberedTarget == null ||
                !rememberedTarget.IsValid)
            {
                return false;
            }

            /*
             * Target 참조는 유지하되,
             * Blackboard가 Memory의 마지막 위치를
             * TargetPosition으로 사용하도록 한다.
             */
            if (!blackboard.SetTarget(
                    rememberedTarget.Target))
            {
                blackboard.SetTargetPosition(
                    rememberedTarget
                        .LastKnownPosition
                );
            }

            if (settings.LogTargetChanges)
            {
                Debug.Log(
                    $"[EnemyStateMachine] " +
                    $"Remembered Target Selected | " +
                    $"Actor=" +
                    $"{GetPieceName(actor)} | " +
                    $"Target=" +
                    $"LastPosition=" +
                    $"{rememberedTarget.LastKnownPosition} | " +
                    $"LastSeenTurn=" +
                    $"{rememberedTarget.LastSeenTurn}"
                );
            }

            return true;
        }

        /*
         * 후퇴 상태로 전환할지 판단한다.
         */
        private bool ShouldRetreat(
            EnemyBlackboard blackboard)
        {
            if (!settings.EnableRetreat)
                return false;

            if (!blackboard.IsLowHealth)
                return false;

            /*
             * 적이 보이거나 추적 중인 목표가 있을 때만
             * 후퇴 상태를 사용한다.
             */
            return blackboard
                       .HasVisibleOpponents ||
                   blackboard
                       .HasCurrentTarget ||
                   blackboard
                       .HasTargetPosition;
        }

        private static bool IsValidOpponent(
            ChessPiece actor,
            ChessPiece target)
        {
            return actor != null &&
                   target != null &&
                   actor != target &&
                   !actor.IsDead &&
                   actor.IsPlaced &&
                   !target.IsDead &&
                   target.IsPlaced &&
                   actor.Color !=
                   target.Color;
        }

        private static string GetPieceName(
            ChessPiece piece)
        {
            return piece != null
                ? piece.name
                : "None";
        }
    }

    /*
     * 상태 전환에 사용하는 설정값이다.
     *
     * 추후 적 성향별 ScriptableObject로 옮길 수 있다.
     */
    public readonly struct EnemyStateSettings
    {
        /*
         * 저체력일 때 Retreat 상태를 사용할지 여부다.
         */
        public bool EnableRetreat
        {
            get;
        }

        /*
         * 목표의 마지막 위치와 이 거리 이하라면
         * Investigate에서 Search로 전환한다.
         */
        public int SearchArrivalDistance
        {
            get;
        }

        /*
         * 목표가 없을 때 Patrol을 사용할지,
         * Idle을 사용할지 결정한다.
         */
        public bool UsePatrolWhenNoTarget
        {
            get;
        }

        public bool LogTargetChanges
        {
            get;
        }

        public EnemyStateSettings(
            bool enableRetreat,
            int searchArrivalDistance,
            bool usePatrolWhenNoTarget,
            bool logTargetChanges)
        {
            EnableRetreat =
                enableRetreat;

            SearchArrivalDistance =
                Mathf.Max(
                    0,
                    searchArrivalDistance
                );

            UsePatrolWhenNoTarget =
                usePatrolWhenNoTarget;

            LogTargetChanges =
                logTargetChanges;
        }

        public static EnemyStateSettings
            CreateDefault()
        {
            return new EnemyStateSettings(
                enableRetreat: true,
                searchArrivalDistance: 0,
                usePatrolWhenNoTarget: false,
                logTargetChanges: false
            );
        }
    }
}
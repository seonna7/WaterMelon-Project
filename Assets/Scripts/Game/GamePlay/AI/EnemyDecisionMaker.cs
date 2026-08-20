using Game.GamePlay.Grid;
using Game.GamePlay.Skill;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay.AI
{
    /*
     * Blackboard와 현재 AI 상태를 기반으로
     * 실행 가능한 행동 후보를 생성한다.
     *
     * 실제 행동 실행은 EnemyActionExecutor가 담당한다.
     */
    public sealed class EnemyDecisionMaker
    {
        private readonly GridManager gridManager;

        private readonly EnemyThreatAnalyzer
            threatAnalyzer;

        private readonly EnemyPathFinder
            pathFinder;

        private readonly EnemyUtilityEvaluator
            utilityEvaluator;

        private readonly List<EnemyAIAction>
            actionCandidates = new();

        public EnemyDecisionMaker(
            GridManager gridManager)
            : this(
                gridManager,
                new EnemyThreatAnalyzer(),
                null,
                null
            )
        {
        }

        public EnemyDecisionMaker(
            GridManager gridManager,
            EnemyThreatAnalyzer threatAnalyzer,
            EnemyPathFinder pathFinder,
            EnemyUtilityEvaluator utilityEvaluator)
        {
            this.gridManager = gridManager;

            this.threatAnalyzer =
                threatAnalyzer ??
                new EnemyThreatAnalyzer();

            this.pathFinder =
                pathFinder ??
                new EnemyPathFinder(
                    gridManager
                );

            this.utilityEvaluator =
                utilityEvaluator ??
                new EnemyUtilityEvaluator(
                    EnemyUtilityWeights
                        .CreateDefault(),
                    this.threatAnalyzer
                );
        }

        public EnemyAIAction DecideAction(
            EnemyBlackboard blackboard)
        {
            if (!IsValidBlackboard(
                    blackboard))
            {
                return EnemyAIAction.CreateNone();
            }

            actionCandidates.Clear();

            switch (blackboard.CurrentState)
            {
                case EnemyAIState.Combat:
                    CollectCombatActions(
                        blackboard
                    );
                    break;

                case EnemyAIState.Retreat:
                    CollectRetreatActions(
                        blackboard
                    );
                    break;

                case EnemyAIState.Investigate:
                    CollectInvestigateActions(
                        blackboard
                    );
                    break;

                case EnemyAIState.Search:
                    CollectSearchActions(
                        blackboard
                    );
                    break;

                case EnemyAIState.Patrol:
                    CollectPatrolActions(
                        blackboard
                    );
                    break;

                case EnemyAIState.Guard:
                    CollectGuardActions(
                        blackboard
                    );
                    break;

                case EnemyAIState.Disabled:
                case EnemyAIState.Dead:
                case EnemyAIState.Idle:
                case EnemyAIState.None:
                default:
                    break;
            }

            /*
             * 어떤 상태에서도 행동 후보가 없으면
             * 정상적으로 대기할 수 있어야 한다.
             */
            actionCandidates.Add(
                EnemyAIAction.CreateWait(
                    blackboard.Actor
                )
            );

            EnemyAIAction selectedAction =
                utilityEvaluator.SelectBestAction(
                    actionCandidates
                );

            blackboard.SetSelectedAction(
                selectedAction
            );

            Debug.Log(
                $"[EnemyDecisionMaker] " +
                $"Actor={blackboard.Actor.name} | " +
                $"State={blackboard.CurrentState} | " +
                $"Selected={selectedAction}"
            );

            return selectedAction;
        }

        /*
         * 디버깅용 후보 목록 반환.
         */
        public List<EnemyAIAction>
            EvaluateAllActions(
                EnemyBlackboard blackboard)
        {
            if (!IsValidBlackboard(
                    blackboard))
            {
                return new List<EnemyAIAction>();
            }

            actionCandidates.Clear();

            switch (blackboard.CurrentState)
            {
                case EnemyAIState.Combat:
                    CollectCombatActions(
                        blackboard
                    );
                    break;

                case EnemyAIState.Retreat:
                    CollectRetreatActions(
                        blackboard
                    );
                    break;

                case EnemyAIState.Investigate:
                    CollectInvestigateActions(
                        blackboard
                    );
                    break;

                case EnemyAIState.Search:
                    CollectSearchActions(
                        blackboard
                    );
                    break;

                case EnemyAIState.Patrol:
                    CollectPatrolActions(
                        blackboard
                    );
                    break;

                case EnemyAIState.Guard:
                    CollectGuardActions(
                        blackboard
                    );
                    break;
            }

            actionCandidates.Add(
                EnemyAIAction.CreateWait(
                    blackboard.Actor
                )
            );

            return utilityEvaluator
                .EvaluateAndSort(
                    actionCandidates
                );
        }

        #region State Actions

        private void CollectCombatActions(
            EnemyBlackboard blackboard)
        {
            CollectDirectAttackActions(
                blackboard
            );

            CollectSkillActions(
                blackboard,
                SkillSlot.Skill1
            );

            CollectSkillActions(
                blackboard,
                SkillSlot.Skill2
            );

            CollectCombatMoveActions(
                blackboard
            );
        }

        /*
         * 후퇴 상태에서는 공격보다
         * 위험 대상으로부터 멀어지는 이동을 우선한다.
         *
         * 탈출할 위치가 없을 경우에만 공격·스킬 후보도
         * 생성해 완전히 무력해지는 것을 막는다.
         */
        private void CollectRetreatActions(
            EnemyBlackboard blackboard)
        {
            ChessPiece actor =
                blackboard.Actor;

            if (blackboard.HasTargetPosition &&
                pathFinder.TryFindMoveAway(
                    actor,
                    blackboard.TargetPosition,
                    out Vector2Int retreatPosition))
            {
                actionCandidates.Add(
                    EnemyAIAction.CreateMove(
                        actor,
                        retreatPosition,
                        150f
                    )
                );

                return;
            }

            CollectDirectAttackActions(
                blackboard
            );

            CollectSkillActions(
                blackboard,
                SkillSlot.Skill1
            );

            CollectSkillActions(
                blackboard,
                SkillSlot.Skill2
            );
        }

        /*
         * 마지막 목격 위치까지 이동한다.
         */
        private void CollectInvestigateActions(
            EnemyBlackboard blackboard)
        {
            if (!blackboard.HasTargetPosition)
                return;

            ChessPiece actor =
                blackboard.Actor;

            if (pathFinder.TryFindMoveToward(
                    actor,
                    blackboard.TargetPosition,
                    out Vector2Int movePosition))
            {
                actionCandidates.Add(
                    EnemyAIAction.CreateMove(
                        actor,
                        movePosition,
                        70f
                    )
                );

                return;
            }

            if (pathFinder
                    .TryFindClosestReachableMove(
                        actor,
                        blackboard.TargetPosition,
                        out movePosition))
            {
                actionCandidates.Add(
                    EnemyAIAction.CreateMove(
                        actor,
                        movePosition,
                        45f
                    )
                );
            }
        }

        /*
         * 마지막 목격 위치에 도착한 뒤
         * 그 주변을 수색한다.
         */
        private void CollectSearchActions(
            EnemyBlackboard blackboard)
        {
            if (!blackboard.HasTargetPosition)
                return;

            if (pathFinder.TryFindSearchMove(
                    blackboard.Actor,
                    blackboard.TargetPosition,
                    out Vector2Int searchPosition))
            {
                actionCandidates.Add(
                    EnemyAIAction.CreateMove(
                        blackboard.Actor,
                        searchPosition,
                        35f
                    )
                );
            }
        }

        /*
         * 실제 Patrol 경로는 아직 작성되지 않았다.
         * 현재는 대기 행동만 사용한다.
         *
         * 다음 Patrol 시스템에서 순찰 지점을 Blackboard에
         * 제공하도록 확장한다.
         */
        private void CollectPatrolActions(
            EnemyBlackboard blackboard)
        {
            if (blackboard == null ||
                blackboard.Actor == null ||
                blackboard.PatrolRuntime == null)
            {
                return;
            }

            EnemyPatrolRuntime patrol =
                blackboard.PatrolRuntime;

            if (!patrol.HasRoute ||
                patrol.IsCompleted)
            {
                return;
            }

            /*
             * 순찰 지점에서 대기 중이면
             * 이번 턴은 Wait만 사용한다.
             */
            if (patrol.IsWaiting)
            {
                patrol.ConsumeWaitTurn();
                return;
            }

            if (!patrol.TryGetCurrentPosition(
                    out Vector2Int patrolPosition))
            {
                return;
            }

            ChessPiece actor =
                blackboard.Actor;

            /*
             * 현재 순찰 지점에 이미 도착한 경우
             * 다음 지점으로 진행한다.
             */
            if (actor.GridPosition ==
                patrolPosition)
            {
                patrol.HandleArrival();

                if (patrol.IsWaiting ||
                    patrol.IsCompleted)
                {
                    return;
                }

                if (!patrol.TryGetCurrentPosition(
                        out patrolPosition))
                {
                    return;
                }
            }

            if (pathFinder.TryFindMoveToward(
                    actor,
                    patrolPosition,
                    out Vector2Int movePosition))
            {
                actionCandidates.Add(
                    EnemyAIAction.CreateMove(
                        actor,
                        movePosition,
                        25f
                    )
                );

                return;
            }

            if (pathFinder
                    .TryFindClosestReachableMove(
                        actor,
                        patrolPosition,
                        out movePosition))
            {
                actionCandidates.Add(
                    EnemyAIAction.CreateMove(
                        actor,
                        movePosition,
                        10f
                    )
                );
            }
        }

        /*
         * Guard는 목표 위치 주변을 벗어나지 않는 상태다.
         * 아직 경계 반경 설정이 없으므로 현재는
         * 공격과 스킬만 생성한다.
         */
        private void CollectGuardActions(
            EnemyBlackboard blackboard)
        {
            CollectDirectAttackActions(
                blackboard
            );

            CollectSkillActions(
                blackboard,
                SkillSlot.Skill1
            );

            CollectSkillActions(
                blackboard,
                SkillSlot.Skill2
            );
        }

        #endregion

        #region Direct Attack

        private void CollectDirectAttackActions(
            EnemyBlackboard blackboard)
        {
            ChessPiece actor =
                blackboard.Actor;

            EnemyPerception perception =
                blackboard.Perception;

            List<Vector2Int> positions =
                actor.GetDirectAttackPositions(
                    gridManager
                );

            if (positions == null)
                return;

            for (int i = 0;
                 i < positions.Count;
                 i++)
            {
                ChessPiece target =
                    gridManager.GetPieceAt(
                        positions[i]
                    );

                if (!IsValidOpponent(
                        actor,
                        target))
                {
                    continue;
                }

                if (perception == null ||
                    !perception.IsCurrentlyVisible(
                        target))
                {
                    continue;
                }

                float threatScore =
                    threatAnalyzer.EvaluateThreat(
                        actor,
                        target
                    );

                actionCandidates.Add(
                    EnemyAIAction
                        .CreateDirectAttack(
                            actor,
                            target,
                            threatScore
                        )
                );
            }
        }

        #endregion

        #region Skill

        private void CollectSkillActions(
            EnemyBlackboard blackboard,
            SkillSlot skillSlot)
        {
            ChessPiece actor =
                blackboard.Actor;

            EnemyPerception perception =
                blackboard.Perception;

            SkillStrategy skill =
                actor.GetSkill(
                    skillSlot
                );

            if (skill == null)
                return;

            SkillContext baseContext =
                new SkillContext(
                    actor,
                    gridManager
                );

            List<Vector2Int> positions =
                skill.GetTargetablePositions(
                    baseContext
                );

            if (positions == null)
                return;

            for (int i = 0;
                 i < positions.Count;
                 i++)
            {
                Vector2Int position =
                    positions[i];

                if (!gridManager.IsInsideGrid(
                        position))
                {
                    continue;
                }

                ChessPiece target =
                    gridManager.GetPieceAt(
                        position
                    );

                /*
                 * 적 유닛 대상은 현재 실제로 보일 때만
                 * 스킬 후보로 포함한다.
                 */
                if (target != null &&
                    target.Color != actor.Color &&
                    (perception == null ||
                     !perception.IsCurrentlyVisible(
                         target)))
                {
                    continue;
                }

                SkillContext context =
                    new SkillContext(
                        actor,
                        gridManager,
                        target,
                        position
                    );

                if (!skill.CanApply(
                        context,
                        position))
                {
                    continue;
                }

                float preliminaryScore =
                    EvaluateSkillTarget(
                        actor,
                        target,
                        position
                    );

                actionCandidates.Add(
                    EnemyAIAction.CreateSkill(
                        actor,
                        skillSlot,
                        position,
                        target,
                        preliminaryScore
                    )
                );
            }
        }

        private float EvaluateSkillTarget(
            ChessPiece actor,
            ChessPiece target,
            Vector2Int position)
        {
            if (target == null)
            {
                return EvaluatePositionSkill(
                    actor,
                    position
                );
            }

            if (target.Color != actor.Color)
            {
                return threatAnalyzer
                    .EvaluateThreat(
                        actor,
                        target
                    );
            }

            return GetMissingHealthRatio(
                       target
                   ) * 100f;
        }

        private float EvaluatePositionSkill(
            ChessPiece actor,
            Vector2Int position)
        {
            float score = 0f;

            for (int x = -1;
                 x <= 1;
                 x++)
            {
                for (int y = -1;
                     y <= 1;
                     y++)
                {
                    if (x == 0 &&
                        y == 0)
                    {
                        continue;
                    }

                    Vector2Int nearbyPosition =
                        position +
                        new Vector2Int(
                            x,
                            y
                        );

                    if (!gridManager.IsInsideGrid(
                            nearbyPosition))
                    {
                        continue;
                    }

                    ChessPiece nearbyPiece =
                        gridManager.GetPieceAt(
                            nearbyPosition
                        );

                    if (nearbyPiece != null &&
                        !nearbyPiece.IsDead &&
                        nearbyPiece.Color !=
                        actor.Color)
                    {
                        score += 10f;
                    }
                }
            }

            return score;
        }

        #endregion

        #region Combat Movement

        private void CollectCombatMoveActions(
            EnemyBlackboard blackboard)
        {
            ChessPiece actor =
                blackboard.Actor;

            IReadOnlyList<ChessPiece> targets =
                blackboard.VisibleOpponents;

            if (targets == null ||
                targets.Count == 0)
            {
                return;
            }

            List<EnemyThreatResult> results =
                threatAnalyzer.EvaluateAll(
                    actor,
                    targets
                );

            for (int i = 0;
                 i < results.Count;
                 i++)
            {
                EnemyThreatResult result =
                    results[i];

                if (!IsValidOpponent(
                        actor,
                        result.Target))
                {
                    continue;
                }

                if (pathFinder.TryFindMoveToward(
                        actor,
                        result.Target.GridPosition,
                        out Vector2Int movePosition))
                {
                    actionCandidates.Add(
                        EnemyAIAction.CreateMove(
                            actor,
                            movePosition,
                            result.ThreatScore
                        )
                    );

                    continue;
                }

                if (pathFinder
                        .TryFindClosestReachableMove(
                            actor,
                            result.Target.GridPosition,
                            out movePosition))
                {
                    actionCandidates.Add(
                        EnemyAIAction.CreateMove(
                            actor,
                            movePosition,
                            result.ThreatScore *
                            0.5f
                        )
                    );
                }
            }
        }

        #endregion

        #region Validation

        private static bool IsValidBlackboard(
            EnemyBlackboard blackboard)
        {
            return blackboard != null &&
                   blackboard.Actor != null &&
                   !blackboard.Actor.IsDead &&
                   blackboard.Actor.IsPlaced &&
                   !blackboard.Actor.IsMoving;
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
                   actor.Color != target.Color;
        }

        private static float GetMissingHealthRatio(
            ChessPiece piece)
        {
            if (piece == null ||
                piece.MaxHP <= 0)
            {
                return 0f;
            }

            return 1f -
                   Mathf.Clamp01(
                       (float)piece.CurrentHP /
                       piece.MaxHP
                   );
        }

        #endregion
    }
}
using Game.Action;
using Game.Core;
using System.Collections;
using UnityEngine;

namespace Game.GamePlay.AI
{
    /*
     * EnemyAIAction을 실제 게임 행동으로 실행한다.
     *
     * 담당 기능:
     * - 이동 실행
     * - 직접 공격 실행
     * - 스킬 실행
     * - 대기 처리
     * - 이동 애니메이션 완료 대기
     *
     * 이 클래스는 행동을 결정하지 않는다.
     * EnemyDecisionMaker가 결정한 행동만 실행한다.
     */
    public sealed class EnemyActionExecutor
    {
        private readonly GameManager gameManager;

        private readonly PieceActionController
            actionController;

        public bool IsExecuting
        {
            get;
            private set;
        }

        public EnemyActionExecutor(
            GameManager gameManager)
        {
            this.gameManager =
                gameManager;

            actionController =
                gameManager != null
                    ? gameManager
                        .PieceActionController
                    : null;
        }

        /*
         * EnemyTurnController에서 코루틴으로 호출한다.
         *
         * 사용 예:
         *
         * yield return actionExecutor.Execute(
         *     selectedAction,
         *     result =>
         *     {
         *         Debug.Log(result.Success);
         *     }
         * );
         */
        public IEnumerator Execute(
            EnemyAIAction action,
            System.Action<
                EnemyAIExecutionResult>
                onComplete = null)
        {
            if (IsExecuting)
            {
                onComplete?.Invoke(
                    EnemyAIExecutionResult
                        .CreateFailed(
                            action,
                            "이미 다른 AI 행동을 실행 중입니다."
                        )
                );

                yield break;
            }

            IsExecuting = true;

            EnemyAIExecutionResult result;

            if (!ValidateAction(
                    action,
                    out string failReason))
            {
                result =
                    EnemyAIExecutionResult
                        .CreateFailed(
                            action,
                            failReason
                        );

                CompleteExecution(
                    result,
                    onComplete
                );

                yield break;
            }

            switch (action.ActionType)
            {
                case EnemyAIActionType.Move:
                    yield return ExecuteMove(
                        action,
                        completedResult =>
                        {
                            result =
                                completedResult;
                        }
                    );

                    /*
                     * 코루틴 내부 콜백에서 할당하기 위한
                     * 임시 초기화 문제가 생길 수 있으므로,
                     * 실제 반환은 전용 실행 메서드를 사용한다.
                     */
                    break;

                case EnemyAIActionType.DirectAttack:
                    result =
                        ExecuteDirectAttack(
                            action
                        );

                    CompleteExecution(
                        result,
                        onComplete
                    );

                    yield break;

                case EnemyAIActionType.UseSkill:
                    result =
                        ExecuteSkill(
                            action
                        );

                    CompleteExecution(
                        result,
                        onComplete
                    );

                    yield break;

                case EnemyAIActionType.Wait:
                    result =
                        ExecuteWait(
                            action
                        );

                    CompleteExecution(
                        result,
                        onComplete
                    );

                    yield break;

                default:
                    result =
                        EnemyAIExecutionResult
                            .CreateFailed(
                                action,
                                "지원하지 않는 AI 행동입니다."
                            );

                    CompleteExecution(
                        result,
                        onComplete
                    );

                    yield break;
            }

            /*
             * Move는 코루틴을 사용하므로
             * 별도 경로로 처리한다.
             */
            EnemyAIExecutionResult moveResult =
                lastMoveResult;

            CompleteExecution(
                moveResult,
                onComplete
            );
        }

        private EnemyAIExecutionResult
            lastMoveResult;

        private IEnumerator ExecuteMove(
            EnemyAIAction action,
            System.Action<
                EnemyAIExecutionResult>
                onComplete)
        {
            ChessPiece actor =
                action.Actor;

            ActionResult actionResult =
                actionController.TryMovePiece(
                    actor,
                    action.TargetPosition
                );

            if (!actionResult.Success)
            {
                lastMoveResult =
                    EnemyAIExecutionResult
                        .CreateFailed(
                            action,
                            "이동 실행에 실패했습니다."
                        );

                onComplete?.Invoke(
                    lastMoveResult
                );

                yield break;
            }

            Debug.Log(
                $"[EnemyActionExecutor] Move Start | " +
                $"Actor={actor.name} | " +
                $"Position={action.TargetPosition}"
            );

            /*
             * GridManager 또는 MoveResolver가
             * ChessPiece의 이동 애니메이션을 시작하면
             * IsMoving이 false가 될 때까지 대기한다.
             */
            if (actor.IsMoving)
            {
                yield return new WaitUntil(
                    () =>
                        actor == null ||
                        !actor.IsMoving
                );
            }

            if (actor == null)
            {
                lastMoveResult =
                    EnemyAIExecutionResult
                        .CreateFailed(
                            action,
                            "이동 중 유닛이 제거되었습니다."
                        );

                onComplete?.Invoke(
                    lastMoveResult
                );

                yield break;
            }

            Debug.Log(
                $"[EnemyActionExecutor] Move Complete | " +
                $"Actor={actor.name} | " +
                $"Position={actor.GridPosition}"
            );

            lastMoveResult =
                EnemyAIExecutionResult
                    .CreateSuccess(
                        action
                    );

            onComplete?.Invoke(
                lastMoveResult
            );
        }

        private EnemyAIExecutionResult
            ExecuteDirectAttack(
                EnemyAIAction action)
        {
            ChessPiece actor =
                action.Actor;

            ChessPiece target =
                action.TargetPiece;

            ActionResult actionResult =
                actionController.TryAttackPiece(
                    actor,
                    target
                );

            if (!actionResult.Success)
            {
                return EnemyAIExecutionResult
                    .CreateFailed(
                        action,
                        "직접 공격 실행에 실패했습니다."
                    );
            }

            Debug.Log(
                $"[EnemyActionExecutor] Direct Attack | " +
                $"Actor={actor.name} | " +
                $"Target={target.name}"
            );

            return EnemyAIExecutionResult
                .CreateSuccess(
                    action
                );
        }

        private EnemyAIExecutionResult
            ExecuteSkill(
                EnemyAIAction action)
        {
            ChessPiece actor =
                action.Actor;

            ActionResult actionResult =
                actionController.TryUseSkill(
                    actor,
                    action.SkillSlot,
                    action.TargetPiece,
                    action.TargetPosition
                );

            if (!actionResult.Success)
            {
                return EnemyAIExecutionResult
                    .CreateFailed(
                        action,
                        "스킬 실행에 실패했습니다."
                    );
            }

            string targetName =
                action.TargetPiece != null
                    ? action.TargetPiece.name
                    : "Position Target";

            Debug.Log(
                $"[EnemyActionExecutor] Skill | " +
                $"Actor={actor.name} | " +
                $"Skill={action.SkillSlot} | " +
                $"Target={targetName} | " +
                $"Position={action.TargetPosition}"
            );

            return EnemyAIExecutionResult
                .CreateSuccess(
                    action
                );
        }

        private static EnemyAIExecutionResult
            ExecuteWait(
                EnemyAIAction action)
        {
            Debug.Log(
                $"[EnemyActionExecutor] Wait | " +
                $"Actor={action.Actor.name}"
            );

            return EnemyAIExecutionResult
                .CreateSuccess(
                    action
                );
        }

        private bool ValidateAction(
            EnemyAIAction action,
            out string failReason)
        {
            failReason = string.Empty;

            if (gameManager == null)
            {
                failReason =
                    "GameManager가 없습니다.";

                return false;
            }

            if (actionController == null)
            {
                failReason =
                    "PieceActionController가 없습니다.";

                return false;
            }

            if (!action.IsValid)
            {
                failReason =
                    "유효하지 않은 AI 행동입니다.";

                return false;
            }

            ChessPiece actor =
                action.Actor;

            if (actor == null)
            {
                failReason =
                    "행동 유닛이 없습니다.";

                return false;
            }

            if (actor.IsDead)
            {
                failReason =
                    "행동 유닛이 사망했습니다.";

                return false;
            }

            if (!actor.IsPlaced)
            {
                failReason =
                    "행동 유닛이 그리드에 없습니다.";

                return false;
            }

            if (actor.IsMoving)
            {
                failReason =
                    "행동 유닛이 이미 이동 중입니다.";

                return false;
            }

            switch (action.ActionType)
            {
                case EnemyAIActionType.DirectAttack:
                    if (action.TargetPiece == null)
                    {
                        failReason =
                            "공격 대상이 없습니다.";

                        return false;
                    }

                    if (action.TargetPiece.IsDead)
                    {
                        failReason =
                            "공격 대상이 이미 사망했습니다.";

                        return false;
                    }

                    break;

                case EnemyAIActionType.UseSkill:
                    if (actor.GetSkill(
                            action.SkillSlot) == null)
                    {
                        failReason =
                            "선택한 슬롯에 스킬이 없습니다.";

                        return false;
                    }

                    break;
            }

            return true;
        }

        private void CompleteExecution(
            EnemyAIExecutionResult result,
            System.Action<
                EnemyAIExecutionResult>
                onComplete)
        {
            IsExecuting = false;

            onComplete?.Invoke(
                result
            );
        }
    }

    /*
     * AI 행동 실행 결과다.
     *
     * 기존 ActionResult를 외부에 그대로 노출하지 않고,
     * AI 시스템에서 필요한 정보만 보관한다.
     */
    public readonly struct
        EnemyAIExecutionResult
    {
        public bool Success
        {
            get;
        }

        public EnemyAIAction Action
        {
            get;
        }

        public string FailReason
        {
            get;
        }

        private EnemyAIExecutionResult(
            bool success,
            EnemyAIAction action,
            string failReason)
        {
            Success = success;
            Action = action;
            FailReason =
                failReason ??
                string.Empty;
        }

        public static EnemyAIExecutionResult
            CreateSuccess(
                EnemyAIAction action)
        {
            return new EnemyAIExecutionResult(
                true,
                action,
                string.Empty
            );
        }

        public static EnemyAIExecutionResult
            CreateFailed(
                EnemyAIAction action,
                string failReason)
        {
            return new EnemyAIExecutionResult(
                false,
                action,
                failReason
            );
        }

        public override string ToString()
        {
            return
                $"Success={Success}, " +
                $"{Action}, " +
                $"FailReason={FailReason}";
        }
    }
}
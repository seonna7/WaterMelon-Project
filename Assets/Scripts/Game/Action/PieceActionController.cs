using Game.Core;
using Game.GamePlay;
using Game.GamePlay.Attack;
using Game.GamePlay.Fog;
using Game.GamePlay.Skill;
using UnityEngine;

namespace Game.Action
{
    public class PieceActionController
    {
        private readonly ActionContext context;

        private readonly ActionCostResolver
            costResolver;

        private readonly MoveResolver
            moveResolver;

        private readonly AttackResolver
            attackResolver;

        private readonly SkillResolver
            skillResolver;

        /*
         * 부쉬 은신 및 안개 시스템은
         * 플레이어와 AI 행동에 공통으로 적용한다.
         */
        private readonly BushStealthResolver
            bushStealthResolver;

        private readonly FogOfWarSystem
            fogOfWarSystem;

        public PieceActionController(
            ActionContext actionContext)
        {
            context = actionContext;

            costResolver =
                new ActionCostResolver();

            moveResolver =
                new MoveResolver();

            attackResolver =
                new AttackResolver();

            skillResolver =
                new SkillResolver();

            /*
             * PieceActionController는 MonoBehaviour가 아니므로
             * Inspector에서 직접 연결할 수 없다.
             *
             * 현재는 씬에서 시스템을 찾아 저장한다.
             * 추후 ActionContext에 참조를 추가하면
             * 생성자 주입 방식으로 변경할 수 있다.
             */
            bushStealthResolver =
                Object.FindFirstObjectByType<
                    BushStealthResolver>();

            fogOfWarSystem =
                Object.FindFirstObjectByType<
                    FogOfWarSystem>();
        }

        public ActionResult TryMovePiece(
            ChessPiece piece,
            Vector2Int targetPos)
        {
            ActionResult validationResult =
                ValidateCommonAction(piece);

            if (!validationResult.Success)
                return validationResult;

            if (!context.Grid.IsInsideGrid(
                    targetPos))
            {
                return ActionResult.CreateFail(
                    ActionFailReason
                        .InvalidTargetPosition
                );
            }

            int moveCost =
                costResolver.GetMoveCost(
                    piece,
                    context.Grid,
                    targetPos
                );

            PlayerRuntimeData currentPlayer =
                context.TurnManager
                    .GetCurrentPlayer();

            if (!currentPlayer.CanSpendGem(
                    moveCost))
            {
                return ActionResult.CreateFail(
                    ActionFailReason.NotEnoughGem
                );
            }

            MoveResult moveResult =
                moveResolver.TryMove(
                    piece,
                    context.Grid,
                    targetPos
                );

            if (!moveResult.Success)
            {
                return ActionResult.CreateFail(
                    ActionFailReason.InvalidMove
                );
            }

            currentPlayer.SpendGem(
                moveCost
            );

            /*
             * GridManager의 논리적 위치는 이동 성공 시
             * 즉시 변경되므로 여기서 시야를 갱신한다.
             *
             * 화면상 이동 애니메이션 완료 후 다시
             * 갱신해도 문제없다.
             */
            RefreshFogVisibility();

            Debug.Log(
                $"[Action] Move | " +
                $"Piece={piece.movementType} | " +
                $"Team={piece.Color} | " +
                $"From={moveResult.From} | " +
                $"To={moveResult.To} | " +
                $"Cost={moveCost}"
            );

            return ActionResult.CreateSuccess(
                moveCost
            );
        }

        public ActionResult TryAttackPiece(
            ChessPiece attacker,
            ChessPiece target)
        {
            /*
             * =========================================
             * 공통 기본 검증
             * =========================================
             *
             * 현재 Turn/Gem 시스템 테스트 전이므로
             * Phase/Turn 관련 검증은 ValidateCommonAction에서
             * 임시 비활성화되어 있다.
             */
            ActionResult validationResult =
                ValidateCommonAction(
                    attacker
                );

            if (!validationResult.Success)
            {
                Debug.LogWarning(
                    $"[Action] Attack Validation Fail | " +
                    $"Reason={validationResult.FailReason}"
                );

                return validationResult;
            }

            if (target == null)
            {
                return ActionResult.CreateFail(
                    ActionFailReason.NoTarget
                );
            }

            if (target.IsDead)
            {
                return ActionResult.CreateFail(
                    ActionFailReason.PieceIsDead
                );
            }

            if (target.Color ==
                attacker.Color)
            {
                return ActionResult.CreateFail(
                    ActionFailReason.SameTeamTarget
                );
            }

            /*
             * =========================================
             * TODO:
             * Turn/Gem 시스템 완성 후 복구
             * =========================================
             */

            // int attackCost =
            //     costResolver.GetAttackCost(
            //         attacker,
            //         context.Grid,
            //         target.GridPosition
            //     );

            // PlayerRuntimeData currentPlayer =
            //     context.TurnManager
            //         .GetCurrentPlayer();

            // if (!currentPlayer.CanSpendGem(
            //         attackCost))
            // {
            //     return ActionResult.CreateFail(
            //         ActionFailReason.NotEnoughGem
            //     );
            // }

            /*
             * =========================================
             * 실제 직접공격 실행
             * =========================================
             */
            AttackResult attackResult =
                attackResolver.Resolve(
                    attacker,
                    target,
                    context.Grid
                );

            if (!attackResult.Success)
            {
                Debug.LogWarning(
                    $"[Action] AttackResolver Fail | " +
                    $"Reason={attackResult.FailReason} | " +
                    $"Attacker={attacker.name} | " +
                    $"Target={target.name}"
                );

                return ActionResult.CreateFail(
                    ActionFailReason.InvalidAttack
                );
            }

            /*
             * TODO:
             * Gem 시스템 완성 후 다시 활성화
             */

            // currentPlayer.SpendGem(
            //     attackCost
            // );

            /*
             * 공격했으므로 은신 해제.
             */
            RevealActionUser(
                attacker
            );

            /*
             * 공격/Push 이후 시야 갱신.
             */
            RefreshFogVisibility();

            /*
             * 승리 조건 검사.
             */
            context.GameManager?
                .CheckWinCondition();

            Debug.Log(
                $"[Action] DirectAttack Success | " +
                $"Attacker={attacker.name} | " +
                $"ActionType={attacker.ActionType} | " +
                $"Target={target.name} | " +
                $"Killed={attackResult.TargetKilled}"
            );

            /*
             * 지금은 비용이 없으므로 0.
             */
            return ActionResult.CreateSuccess(
                0
            );
        }
        public ActionResult TryUseSkill(
            ChessPiece caster,
            SkillSlot skillSlot,
            ChessPiece targetPiece = null,
            Vector2Int targetPosition = default)
        {
            ActionResult validationResult =
                ValidateCommonAction(caster);

            if (!validationResult.Success)
                return validationResult;

            SkillStrategy skill =
                caster.GetSkill(
                    skillSlot
                );

            if (skill == null)
            {
                return ActionResult.CreateFail(
                    ActionFailReason.InvalidAttack
                );
            }

            int skillCost =
                skill.ActionPointCost;

            PlayerRuntimeData currentPlayer =
                context.TurnManager
                    .GetCurrentPlayer();

            if (!currentPlayer.CanSpendGem(
                    skillCost))
            {
                return ActionResult.CreateFail(
                    ActionFailReason.NotEnoughGem
                );
            }

            SkillResult skillResult =
                skillResolver.Resolve(
                    caster,
                    skillSlot,
                    context.Grid,
                    targetPiece,
                    targetPosition
                );

            if (!skillResult.Success)
            {
                return ActionResult.CreateFail(
                    ActionFailReason.InvalidAttack
                );
            }

            currentPlayer.SpendGem(
                skillCost
            );

            /*
             * 부쉬 안에서 스킬을 사용한 시전자를
             * 노출 상태로 만든다.
             *
             * 설치형 스킬, 이동 스킬, 소환 스킬 등으로
             * 시야가 달라질 수 있으므로 전체 갱신한다.
             */
            RevealActionUser(
                caster
            );

            RefreshFogVisibility();

            context.GameManager
                .CheckWinCondition();

            Debug.Log(
                $"[Action] Skill | " +
                $"Piece={caster.movementType} | " +
                $"Team={caster.Color} | " +
                $"Skill={skill.SkillName} | " +
                $"Slot={skillSlot} | " +
                $"TargetPosition={targetPosition} | " +
                $"Cost={skillCost}"
            );

            return ActionResult.CreateSuccess(
                skillCost
            );
        }

        /*
         * 이동, 공격, 스킬에서 공통으로 사용하는
         * 기본 행동 가능 여부 검사다.
         */
        private ActionResult ValidateCommonAction(
            ChessPiece piece)
        {
            if (context == null ||
                context.GameManager == null)
            {
                return ActionResult.CreateFail(
                    ActionFailReason.InvalidPhase
                );
            }

            //if (!context.GameManager
            //        .PhaseManager
            //        .IsPhase(
            //            GamePhase.Battle))
            //{
            //    return ActionResult.CreateFail(
            //        ActionFailReason.InvalidPhase
            //    );
            //}

            if (piece == null)
            {
                return ActionResult.CreateFail(
                    ActionFailReason.PieceIsNull
                );
            }

            if (piece.IsDead)
            {
                return ActionResult.CreateFail(
                    ActionFailReason.PieceIsDead
                );
            }

            if (!piece.IsPlaced)
            {
                return ActionResult.CreateFail(
                    ActionFailReason
                        .InvalidTargetPosition
                );
            }

            //if (piece.IsMoving)
            //{
            //    return ActionResult.CreateFail(
            //        ActionFailReason.InvalidMove
            //    );
            //}

            //if (piece.Color !=
            //    context.TurnManager
            //        .CurrentTurnColor)
            //{
            //    return ActionResult.CreateFail(
            //        ActionFailReason
            //            .NotPlayersTurn
            //    );
            //}

            return ActionResult.CreateSuccess(
                0
            );
        }

        /*
         * 공격 또는 스킬 사용자를 부쉬에서 노출한다.
         *
         * 부쉬 밖 유닛을 RevealTarget에 전달해도
         * 게임 진행에는 문제가 없지만,
         * 불필요한 노출 데이터를 방지하기 위해
         * 현재 부쉬 안에 있을 때만 등록한다.
         */
        private void RevealActionUser(
            ChessPiece piece)
        {
            if (piece == null ||
                bushStealthResolver == null)
            {
                return;
            }

            if (!bushStealthResolver
                    .IsInBush(piece))
            {
                return;
            }

            bushStealthResolver
                .RevealTarget(piece);
        }

        /*
         * 양 팀 시야를 모두 갱신한다.
         *
         * 한 유닛의 행동으로 상대 팀 시야도 달라질 수 있어
         * 행동 성공 후에는 전체 갱신이 안전하다.
         */
        private void RefreshFogVisibility()
        {
            fogOfWarSystem?
                .RefreshAllVisibility();
        }
    }
}
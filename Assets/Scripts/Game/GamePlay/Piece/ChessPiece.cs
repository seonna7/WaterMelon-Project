using Game.GamePlay.Grid;
using Game.GamePlay.Prefabs.Effects;
using Game.GamePlay.Skill;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.GamePlay
{
    public abstract class ChessPiece : MonoBehaviour
    {
        private static readonly Vector2Int InvalidGridPosition =
            new Vector2Int(-1, -1);

        [Header("View")]
        [SerializeField]
        private ChessPieceHighlight selectable;

        [Header("Move Animation")]
        [SerializeField]
        private float moveDuration = 0.45f;

        [SerializeField]
        private float jumpHeight = 0.8f;

        [field: SerializeField]
        public PieceColor Color
        {
            get;
            protected set;
        }
        public MovementType movementType { get; protected set; }

        public ActionType ActionType { get; protected set; }

        [Header("Grid State")]
        [SerializeField]
        private Vector2Int gridPosition =
            new Vector2Int(-1, -1);

        public Vector2Int GridPosition =>
            gridPosition;

        public bool IsPlaced =>
            gridPosition != InvalidGridPosition;

        public int MaxHP { get; protected set; }

        public int CurrentHP { get; protected set; }

        public int AttackPower { get; protected set; }

        public bool IsDead =>
            CurrentHP <= 0;

        public bool IsMoving =>
            isMoving;

        public ChessPieceHighlight Selectable =>
            selectable;

        protected IChessMoveStrategy moveStrategy;
        public string MoveStrategyName =>
    moveStrategy != null
        ? moveStrategy.GetType().Name
        : "None";

        public IChessMoveStrategy moveStrategyProperty
        {
            get => moveStrategy;
        }

        protected SkillStrategy skill1;

        protected SkillStrategy skill2;

        public SkillStrategy Skill1 =>
            skill1;

        public SkillStrategy Skill2 =>
            skill2;

        private bool isMoving;

        protected virtual void Awake()
        {
            if (selectable == null)
            {
                selectable =
                    GetComponentInChildren<ChessPieceHighlight>();
            }

            selectable?.SetHighlight(false);
        }

        public virtual void Initialize(
            PieceColor color)
        {
            Color = color;

            CurrentHP = MaxHP;

            selectable?.SetHighlight(false);
        }
        public virtual void Initialize(
            PieceColor color,
            Vector2Int spawnPosition)
        {
            Color = color;
            gridPosition = spawnPosition;

            CurrentHP = MaxHP;

            selectable?.SetHighlight(false);
        }


        #region Movement

        public virtual List<Vector2Int> GetPossibleMoves(
            GridManager gridManager)
        {
            if (moveStrategy == null ||
                gridManager == null ||
                !IsPlaced ||
                IsDead ||
                IsMoving)
            {
                return new List<Vector2Int>();
            }

            return moveStrategy.GetAvailableMoves(
                this,
                gridManager
            );
        }

        public void SetMoveStrategy(
            IChessMoveStrategy strategy)
        {
            moveStrategy = strategy;
        }

        #endregion Movement

        #region Skills

        public SkillStrategy GetSkill(
            SkillSlot skillSlot)
        {
            return skillSlot switch
            {
                SkillSlot.Skill1 => skill1,
                SkillSlot.Skill2 => skill2,
                _ => null
            };
        }

        public void SetSkill(
            SkillSlot skillSlot,
            SkillStrategy strategy)
        {
            switch (skillSlot)
            {
                case SkillSlot.Skill1:
                    skill1 = strategy;
                    break;

                case SkillSlot.Skill2:
                    skill2 = strategy;
                    break;

                default:
                    Debug.LogWarning(
                        $"{name}: 유효하지 않은 스킬 슬롯입니다."
                    );
                    break;
            }
        }

        public virtual List<Vector2Int> GetDirectAttackPositions(GridManager gridManager)
        {
            if (moveStrategy == null ||
                gridManager == null ||
                !IsPlaced ||
                IsDead ||
                IsMoving)
            {
                return new List<Vector2Int>();
            }

            return moveStrategy.GetDirectAttackPositions(
                this,
                gridManager
            );
        }

        public bool HasSkill(
            SkillSlot skillSlot)
        {
            return GetSkill(skillSlot) != null;
        }

        public SkillResult UseSkill(
            SkillSlot skillSlot,
            SkillContext context)
        {
            if (IsDead)
            {
                return SkillResult.CreateFail(
                    this,
                    "사망한 말은 스킬을 사용할 수 없습니다."
                );
            }

            if (!IsPlaced)
            {
                return SkillResult.CreateFail(
                    this,
                    "보드에 배치되지 않은 말입니다."
                );
            }

            if (IsMoving)
            {
                return SkillResult.CreateFail(
                    this,
                    "이동 중에는 스킬을 사용할 수 없습니다."
                );
            }

            SkillStrategy selectedSkill =
                GetSkill(skillSlot);

            if (selectedSkill == null)
            {
                return SkillResult.CreateFail(
                    this,
                    "해당 슬롯에 스킬이 없습니다."
                );
            }

            if (context.Caster != this)
            {
                return SkillResult.CreateFail(
                    this,
                    "스킬 시전자 정보가 일치하지 않습니다."
                );
            }

            if (!selectedSkill.CanUse(context))
            {
                return SkillResult.CreateFail(
                    this,
                    "현재 조건에서는 스킬을 사용할 수 없습니다."
                );
            }

            return selectedSkill.Execute(context);
        }

        public List<Vector2Int> GetSkillTargetablePositions(
            SkillSlot skillSlot,
            SkillContext context)
        {
            SkillStrategy selectedSkill =
                GetSkill(skillSlot);

            if (selectedSkill == null ||
                IsDead ||
                !IsPlaced ||
                IsMoving)
            {
                return new List<Vector2Int>();
            }

            return selectedSkill.GetTargetablePositions(
                context
            );
        }

        #endregion Skills

        #region Grid Position

        internal void SetGridPosition(
    Vector2Int newGridPosition)
        {
            gridPosition =
                newGridPosition;
        }

        internal void ClearGridPosition()
        {
            gridPosition =
                InvalidGridPosition;
        }

        #endregion Grid Position

        #region Highlight

        public void SetHighlight(bool enable)
        {
            if (IsDead && enable)
                return;

            selectable?.SetHighlight(enable);
        }

        #endregion Highlight

        #region Combat

        /*
         * 이 메서드는 체스 말의 이동 규칙을 이용한
         * 직접 공격에 사용한다.
         *
         * 공격 가능 거리, 행동력 소모,
         * ActionType 효과 적용 여부는
         * AttackSystem 또는 AttackResolver가 판단한다.
         */

        public virtual bool Attack(
            ChessPiece target)
        {
            if (target == null ||
                target == this ||
                target.Color == Color ||
                target.IsDead ||
                IsDead)
            {
                return false;
            }

            target.TakeDamage(AttackPower);

            return true;
        }

        public virtual void TakeDamage(
            int damage)
        {
            if (damage <= 0 || IsDead)
                return;

            CurrentHP = Mathf.Max(
                CurrentHP - damage,
                0
            );

            if (IsDead)
            {
                Die();
            }
        }

        public virtual void Heal(
            int amount)
        {
            if (amount <= 0 || IsDead)
                return;

            CurrentHP = Mathf.Min(
                CurrentHP + amount,
                MaxHP
            );
        }

        protected virtual void Die()
        {
            SetHighlight(false);

            Debug.Log($"{name} 사망");

            /*
             * 여기서는 GridPosition을 지우거나
             * GameObject를 비활성화하지 않는다.
             *
             * GridManager가 셀 점유를 먼저 해제한 후
             * AttackSystem, SkillSystem 또는 별도의
             * DeathSystem이 오브젝트를 처리해야 한다.
             */
        }

        #endregion Combat

        #region Move Animation

        public void MoveToWorldPosition(
            Vector3 targetWorldPosition,
            System.Action onComplete = null)
        {
            if (isMoving || IsDead)
                return;

            StartCoroutine(
                MoveRoutine(
                    targetWorldPosition,
                    onComplete
                )
            );
        }

        private IEnumerator MoveRoutine(
            Vector3 targetWorldPosition,
            System.Action onComplete)
        {
            isMoving = true;

            Vector3 startPosition =
                transform.position;

            float elapsed = 0f;

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(
                    elapsed / moveDuration
                );

                float smoothT = Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );

                Vector3 currentPosition =
                    Vector3.Lerp(
                        startPosition,
                        targetWorldPosition,
                        smoothT
                    );

                float heightOffset =
                    Mathf.Sin(t * Mathf.PI) *
                    jumpHeight;

                currentPosition.y += heightOffset;

                transform.position =
                    currentPosition;

                yield return null;
            }

            transform.position =
                targetWorldPosition;

            isMoving = false;

            onComplete?.Invoke();
        }

        #endregion Move Animation
    }
}

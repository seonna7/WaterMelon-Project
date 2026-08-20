using Game.CameraSystem;
using Game.GamePlay.Skill;
using Game.UI;
using UnityEngine;
using UnityEngine.EventSystems;


namespace Game.GamePlay.Selection
{
    public class ChessPieceClickSelector : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private Camera targetCamera;

        [SerializeField]
        private CameraCommandManager cameraCommandManager;

        [SerializeField]
        private PieceActionMenuUI pieceActionMenuUI;

        [SerializeField]
        private MoveTargetSelector moveTargetSelector;

        [SerializeField]
        private SkillTargetSelector skillTargetSelector;

        [Header("Raycast")]
        [SerializeField]
        private LayerMask pieceLayerMask;

        [SerializeField]
        private float rayDistance = 100f;

        private ChessPiece currentSelectedPiece;

        private bool isActionMenuOpen;

        private void Awake()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            cameraCommandManager =
                FindFirstObjectByType<
                    CameraCommandManager>();

            if (pieceActionMenuUI != null)
            {
                pieceActionMenuUI.BindMoveAction(
                    OnClickMove
                );

                pieceActionMenuUI.BindSkill1Action(
                    OnClickSkill1
                );

                pieceActionMenuUI.BindSkill2Action(
                    OnClickSkill2
                );

                pieceActionMenuUI.Hide();
            }

            currentSelectedPiece = null;
            isActionMenuOpen = false;
        }

        private void Update()
        {
            if (!Input.GetMouseButtonDown(0) &&
                !Input.GetMouseButtonDown(1))
            {
                return;
            }

            if (moveTargetSelector != null &&
                moveTargetSelector.IsMoveMode)
            {
                Debug.Log(
                    "[ClickSelector] MoveMode라서 입력 차단");

                return;
            }

            if (skillTargetSelector != null &&
                skillTargetSelector.IsSkillMode)
            {
                Debug.Log(
                    "[ClickSelector] SkillMode라서 입력 차단");

                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                Debug.Log(
                    "[ClickSelector] 좌클릭 입력 감지");

                if (isActionMenuOpen &&
                    IsPointerOverUI())
                {
                    Debug.Log(
                        "[ClickSelector] UI 클릭이라서 선택 생략");

                    return;
                }

                TrySelectPiece();
            }

            if (Input.GetMouseButtonDown(1))
            {
                ClearSelection();
            }
        }

        private bool IsPointerOverUI()
        {
            return EventSystem.current != null &&
                   EventSystem.current
                       .IsPointerOverGameObject();
        }

        private void TrySelectPiece()
        {
            if (targetCamera == null)
            {
                Debug.LogWarning(
                    "[ClickSelector] " +
                    "Target Camera가 없습니다."
                );

                return;
            }

            Ray ray =
                targetCamera.ScreenPointToRay(
                    Input.mousePosition
                );

            bool hitPiece = Physics.Raycast(
                ray,
                out RaycastHit hit,
                rayDistance,
                pieceLayerMask,
                QueryTriggerInteraction.Collide
            );

            if (hitPiece)
            {
                ChessPiece clickedPiece =
                    hit.collider
                        .GetComponentInParent<
                            ChessPiece
                        >();

                if (clickedPiece != null)
                {
                    SelectPiece(clickedPiece);
                    return;
                }
            }

            ClearSelection();
        }

        private void SelectPiece(
            ChessPiece piece)
        {
            if (piece == null ||
                piece.IsDead ||
                !piece.IsPlaced)
            {
                return;
            }

            if (currentSelectedPiece == piece)
            {
                isActionMenuOpen = true;

                currentSelectedPiece.SetHighlight(
                    true
                );

                if (pieceActionMenuUI != null)
                {
                    pieceActionMenuUI.Show(
                        currentSelectedPiece
                    );
                }

                return;
            }

            if (currentSelectedPiece != null)
            {
                currentSelectedPiece.SetHighlight(
                    false
                );
            }

            currentSelectedPiece = piece;
            isActionMenuOpen = true;

            currentSelectedPiece.SetHighlight(
                true
            );

            if (pieceActionMenuUI != null)
            {
                pieceActionMenuUI.Show(
                    currentSelectedPiece
                );
            }

            cameraCommandManager?.ShowPiece(currentSelectedPiece.transform);



            Debug.Log(
                $"[ClickSelector] 선택 변경: " +
                $"{currentSelectedPiece.name}"
            );
        }

        public void ClearSelection()
        {
            if (currentSelectedPiece != null)
            {
                currentSelectedPiece.SetHighlight(
                    false
                );
            }

            currentSelectedPiece = null;
            isActionMenuOpen = false;

            if (pieceActionMenuUI != null)
            {
                pieceActionMenuUI.Hide();
            }
            cameraCommandManager?.ReturnToGrid();

            Debug.Log(
                "[ClickSelector] 선택 해제"
            );
        }

        private void OnClickMove()
        {
            if (currentSelectedPiece == null)
                return;

            if (moveTargetSelector == null)
            {
                Debug.LogWarning(
                    "[ClickSelector] " +
                    "MoveTargetSelector가 " +
                    "연결되지 않았습니다."
                );

                return;
            }

            ChessPiece pieceToMove =
                currentSelectedPiece;

            isActionMenuOpen = false;

            if (pieceActionMenuUI != null)
            {
                pieceActionMenuUI.Hide();
            }

            pieceToMove.SetHighlight(false);

            currentSelectedPiece = null;

            cameraCommandManager?.ShowMoveRange(
    pieceToMove.transform,
    () =>
    {
        moveTargetSelector.EnterMoveMode(
            pieceToMove
        );
    });

            Debug.Log(
                $"[ActionUI] Move clicked: " +
                $"{pieceToMove.name}"
            );
        }

        private void OnClickSkill1()
        {
            EnterSkillTargetMode(
                SkillSlot.Skill1
            );
        }

        private void OnClickSkill2()
        {
            EnterSkillTargetMode(
                SkillSlot.Skill2
            );
        }

        private void EnterSkillTargetMode(
            SkillSlot skillSlot)
        {
            if (currentSelectedPiece == null)
                return;

            if (skillTargetSelector == null)
            {
                Debug.LogWarning(
                    "[ClickSelector] " +
                    "SkillTargetSelector가 " +
                    "연결되지 않았습니다."
                );

                return;
            }

            if (!currentSelectedPiece.HasSkill(
                    skillSlot))
            {
                Debug.LogWarning(
                    $"[ClickSelector] " +
                    $"{skillSlot}에 스킬이 없습니다."
                );

                return;
            }

            ChessPiece caster =
                currentSelectedPiece;

            isActionMenuOpen = false;

            if (pieceActionMenuUI != null)
            {
                pieceActionMenuUI.Hide();
            }

            caster.SetHighlight(false);

            currentSelectedPiece = null;

            cameraCommandManager?.ShowSkillRange(
                caster.transform,
                () =>
                {
                    skillTargetSelector.EnterSkillMode(
                        caster,
                        skillSlot
                    );
                });

            Debug.Log(
                $"[ActionUI] {skillSlot} clicked: " +
                $"{caster.name}"
            );
        }
    }
}
using UnityEngine;

namespace Game
{
    namespace GamePlay
    {
        namespace Prefabs
        {
            namespace Effects
            {
                public class ChessPieceHoverSelector : MonoBehaviour
                {
                    [SerializeField] private Camera targetCamera;

                    [SerializeField] private LayerMask pieceLayerMask;

                    [SerializeField] private float rayDistance = 100f;

                    private ChessPieceHighlight currentHovered;

                    private void Awake()
                    {
                        if (targetCamera == null)
                        {
                            targetCamera = Camera.main;
                        }
                    }

                    private void Update()
                    {
                        UpdateHover();
                    }

                    private void UpdateHover()
                    {
                        if (targetCamera == null)
                            return;

                        Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);

                        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, pieceLayerMask))
                        {
                            Debug.Log("Raycast Hit: " + hit.collider.name);

                            ChessPieceHighlight hovered =
                                hit.collider.GetComponentInParent<ChessPieceHighlight>();

                            if (hovered != null)
                            {
                                Debug.Log("Hovered Piece: " + hovered.name);
                                SetHoveredObject(hovered);
                                return;
                            }
                        }

                        ClearHover();
                    }

                    private void SetHoveredObject(ChessPieceHighlight newHovered)
                    {
                        if (currentHovered == newHovered)
                            return;

                        if (currentHovered != null)
                        {
                            currentHovered.SetHighlight(false);
                        }

                        currentHovered = newHovered;

                        if (currentHovered != null)
                        {
                            currentHovered.SetHighlight(true);
                        }
                    }

                    private void ClearHover()
                    {
                        if (currentHovered != null)
                        {
                            currentHovered.SetHighlight(false);
                            currentHovered = null;
                        }
                    }

                    public ChessPieceHighlight GetCurrentHovered()
                    {
                        return currentHovered;
                    }
                }
            }
        }
    }
}

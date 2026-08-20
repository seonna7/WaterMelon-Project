#if UNITY_EDITOR
using Game.GamePlay.Placement;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Placement
{
    [CustomEditor(typeof(PieceSpawnPreviewController))]
    public sealed class PieceSpawnPreviewControllerEditor
        : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            PieceSpawnPreviewController controller =
                (PieceSpawnPreviewController)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField(
                "Piece Spawn Preview",
                EditorStyles.boldLabel);

            if (GUILayout.Button(
                    "SpawnData에서 프리뷰 생성"))
            {
                controller.RebuildPreviewFromData();
            }

            if (GUILayout.Button(
                    "현재 Scene 배치를 SpawnData에 저장"))
            {
                controller.SynchronizeSceneToData();
            }

            if (GUILayout.Button(
                    "프리뷰 제거"))
            {
                controller.ClearPreview();
            }
        }
    }
}
#endif
using UnityEditor;

namespace ArtUnityWorkshop.Editor
{
    [CustomEditor(typeof(CharacterStats))]
    public sealed class CharacterStatsEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            using (new EditorGUI.DisabledScope(true))
                EditorGUILayout.IntField("현재 체력 (실행 중)", ((CharacterStats)target).CurrentHealth);
        }
    }
}

using UnityEditor;

namespace ArtUnityWorkshop.Editor
{
    [CustomEditor(typeof(BattleCoordinator))]
    public sealed class BattleCoordinatorEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var battle = (BattleCoordinator)target;
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.Toggle("전투 중", battle.IsBattleActive);
                EditorGUILayout.EnumPopup("진행 상태", battle.Phase);
                EditorGUILayout.IntField("라운드", battle.RoundNumber);
                EditorGUILayout.FloatField("남은 선택 시간(초)", battle.RemainingTime);
                EditorGUILayout.ObjectField("플레이어", battle.Player, typeof(Combatant), true);
                EditorGUILayout.ObjectField("상대 적", battle.Opponent, typeof(Combatant), true);
            }
        }
    }
}

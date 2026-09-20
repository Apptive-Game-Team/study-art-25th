using UnityEngine;

namespace ArtUnityWorkshop
{
    public sealed class InputLegend : MonoBehaviour
    {
        [SerializeField, InspectorName("입력 연결"), Tooltip("현재 Binding을 조작 안내에 표시할 InputRouter입니다.")]
        private InputRouter input;
        private void OnGUI()
        {
            if (input == null) return;
            var style = new GUIStyle(GUI.skin.box) { fontSize = 18, alignment = TextAnchor.MiddleLeft };
            GUI.Box(new Rect(16, 16, Mathf.Min(620, Screen.width - 32), 44),
                "  " + gameObject.scene.name + "   |   Move: " + input.BindingLabel("MoveLeft") + " / " + input.BindingLabel("MoveRight") +
                "   |   Interact: " + input.BindingLabel("Interact"), style);
            var battle = BattleCoordinator.FindInScene(gameObject.scene);
            if (battle == null || !battle.IsBattleActive) return;
            string prompt = battle.Phase == BattlePhase.StopLeft ? "Stop LEFT" :
                battle.Phase == BattlePhase.StopRight ? "Stop RIGHT" :
                battle.Phase == BattlePhase.Selecting ? "Choose / Confirm" : battle.Phase.ToString();
            GUI.Box(new Rect(16, 70, Mathf.Min(620, Screen.width - 32), 140),
                $"  Round {battle.RoundNumber} | {prompt}\n" +
                $"  Enemy: {battle.EnemyLeft} / {battle.EnemyRight}   HP {battle.Opponent.Stats.CurrentHealth}\n" +
                $"  Player: {battle.PlayerLeft} / {battle.PlayerRight}   HP {battle.Player.Stats.CurrentHealth}\n" +
                $"  Selected: {(battle.SelectedIndex == 0 ? "LEFT" : "RIGHT")} | {battle.RemainingTime:F1}s\n" +
                (battle.Phase == BattlePhase.Resolving ? $"  Result: {(battle.Result == 0 ? "Draw" : battle.Result > 0 ? "Win" : "Lose")}" : ""), style);
        }
    }
}

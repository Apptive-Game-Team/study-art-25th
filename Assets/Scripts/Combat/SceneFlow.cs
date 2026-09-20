using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace ArtUnityWorkshop
{
    [DefaultExecutionOrder(200)]
    public sealed class SceneFlow : MonoBehaviour
    {
        [SerializeField, InspectorName("상호작용 안내"), Tooltip("가장 가까운 대상과 현재 Interact Binding을 표시합니다.")]
        private TMP_Text prompt;
        [SerializeField, InspectorName("패배 화면"), Tooltip("Death 완료 후 표시할 UI입니다.")]
        private GameObject defeatPanel;
        [SerializeField, InspectorName("마을 귀환 버튼"), Tooltip("마우스 또는 Interact로 누릅니다. 자동 선택됩니다.")]
        private Button returnButton;
        [SerializeField, InspectorName("승리 보상 표현"), Tooltip("적 처치 위치에 잠시 표시하는 Prefab입니다. 트로피 획득은 자동입니다.")]
        private GameObject rewardPrefab;
        public Combatant Player { get; private set; }
        private InputRouter input;
        private BattleCoordinator battle;
        private InteractionPoint[] points;
        private EnemyMovement[] enemies;
        private int interactFrame = -1, defeatFrame = -1;
        private bool interactedInBattle;
        private Vector3 rewardPosition;

        private void Start()
        {
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                if (input == null) input = root.GetComponentInChildren<InputRouter>();
                var movement = root.GetComponentInChildren<PlayerMovement>();
                if (movement != null) Player = movement.GetComponent<Combatant>();
            }
            if (Player == null || input == null)
            { Debug.LogError("SceneFlow: Scene에 Player와 InputRouter를 배치해 주세요.", this); enabled = false; return; }
            battle = BattleCoordinator.FindInScene(gameObject.scene);
            var allPoints = new System.Collections.Generic.List<InteractionPoint>();
            var allEnemies = new System.Collections.Generic.List<EnemyMovement>();
            foreach (var root in gameObject.scene.GetRootGameObjects())
            { allPoints.AddRange(root.GetComponentsInChildren<InteractionPoint>(true)); allEnemies.AddRange(root.GetComponentsInChildren<EnemyMovement>(true)); }
            points = allPoints.ToArray(); enemies = allEnemies.ToArray();
            if (GameSession.RespawnPending)
            { Player.Stats.RestoreHealth(GameSession.RespawnHealth(Player.Stats.MaxHealth)); GameSession.RespawnPending = false; }
            else if (GameSession.PlayerHealth >= 0) Player.Stats.RestoreHealth(GameSession.PlayerHealth);
            bool survivors = false;
            foreach (int health in GameSession.Enemies.Values) survivors |= health > 0;
            if (gameObject.scene.name == "Battle" && !survivors) GameSession.Enemies.Clear();
            foreach (var enemy in enemies)
            {
                int health;
                if (!GameSession.Enemies.TryGetValue(EnemyKey(enemy), out health)) continue;
                if (health <= 0) enemy.gameObject.SetActive(false);
                else enemy.GetComponent<CharacterStats>().RestoreHealth(health);
            }
            if (defeatPanel != null) defeatPanel.SetActive(false);
            if (returnButton != null) returnButton.onClick.AddListener(ReturnAfterDefeat);
            input.Interacted += OnInteract;
            if (battle != null) { battle.BattleStarted += OnBattleStarted; battle.BattleFinished += OnFinished; }
            GameSession.Transitioning = false;
        }
        private void OnBattleStarted() => rewardPosition = battle.Opponent.transform.position;
        private void OnInteract()
        {
            interactFrame = Time.frameCount;
            interactedInBattle = battle != null && battle.IsBattleActive;
        }
        private static string EnemyKey(EnemyMovement enemy)
        {
            var identity = enemy.GetComponent<EnemyIdentity>();
            return identity != null ? identity.Id : enemy.name;
        }
        private void OnFinished(bool won)
        {
            if (won)
            {
                GameSession.TrophyOwned = true;
                if (rewardPrefab != null) Destroy(Instantiate(rewardPrefab, rewardPosition + Vector3.up, Quaternion.identity), 2);
            }
            else
            {
                defeatFrame = Time.frameCount;
                if (defeatPanel != null) defeatPanel.SetActive(true);
                if (returnButton != null && EventSystem.current != null) EventSystem.current.SetSelectedGameObject(returnButton.gameObject);
            }
        }
        private void LateUpdate()
        {
            if (Player == null || GameSession.Transitioning) return;
            if (battle != null && battle.IsBattleActive)
            {
                if (prompt != null) prompt.text = "";
                if (battle.Phase == BattlePhase.Defeated && interactFrame == Time.frameCount && Time.frameCount > defeatFrame)
                    ReturnAfterDefeat();
                return;
            }
            InteractionPoint nearest = null;
            float distance = float.MaxValue;
            foreach (var point in points)
            {
                if (point == null || !point.isActiveAndEnabled) continue;
                float d = Mathf.Abs(point.transform.position.x - Player.transform.position.x);
                if (d <= point.Range && d < distance) { distance = d; nearest = point; }
            }
            if (prompt != null) prompt.text = nearest != null ? $"{input.BindingLabel("Interact")}  {nearest.Label}" : "";
            if (nearest != null && interactFrame == Time.frameCount && !interactedInBattle) nearest.Use(this);
        }
        public void ReturnAfterDefeat()
        {
            if (battle == null || battle.Phase != BattlePhase.Defeated || Time.frameCount <= defeatFrame) return;
            GameSession.RespawnPending = true;
            Travel("Town", true);
        }
        public bool Travel(string destination, bool afterDefeat = false)
        {
            if (GameSession.Transitioning || Player == null ||
                (battle != null && battle.IsBattleActive && !(afterDefeat && battle.Phase == BattlePhase.Defeated))) return false;
            if (!Application.CanStreamedLevelBeLoaded(destination))
            { Debug.LogWarning("목적 Scene이 Build Settings에 없습니다: " + destination, this); return false; }
            GameSession.PlayerHealth = Player.Stats.CurrentHealth;
            if (gameObject.scene.name == "Battle")
                foreach (var enemy in enemies)
                    if (enemy != null) GameSession.Enemies[EnemyKey(enemy)] = enemy.gameObject.activeSelf ? enemy.GetComponent<CharacterStats>().CurrentHealth : 0;
            GameSession.Transitioning = true;
            SceneManager.LoadSceneAsync(destination);
            return true;
        }
        private void OnDestroy()
        {
            if (input != null) input.Interacted -= OnInteract;
            if (battle != null) { battle.BattleStarted -= OnBattleStarted; battle.BattleFinished -= OnFinished; }
            if (returnButton != null) returnButton.onClick.RemoveListener(ReturnAfterDefeat);
        }
    }
}

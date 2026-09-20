using UnityEngine;

namespace ArtUnityWorkshop
{
    [RequireComponent(typeof(CircleCollider2D))]
    public sealed class BattleTrigger : MonoBehaviour
    {
        [SerializeField, InspectorName("소유한 적"), Tooltip("이 감지 영역을 가진 적 루트의 Combatant입니다.")]
        private Combatant owner;

        private void Awake()
        {
            if (owner == null) owner = GetComponentInParent<Combatant>();
            GetComponent<CircleCollider2D>().isTrigger = true;
            if (owner == null)
            {
                Debug.LogWarning("전투 감지 영역을 적 Combatant의 자식에 배치해 주세요.", this);
                enabled = false;
            }
        }

        private void OnTriggerEnter2D(Collider2D other) => TryContact(other);
        private void OnTriggerStay2D(Collider2D other) => TryContact(other);

        private void TryContact(Collider2D other)
        {
            if (!isActiveAndEnabled || owner == null) return;
            var player = other.GetComponentInParent<PlayerMovement>();
            if (player == null || !player.isActiveAndEnabled) return;
            var battle = BattleCoordinator.FindInScene(gameObject.scene);
            if (battle != null) battle.TryBegin(player.GetComponent<Combatant>(), owner);
        }
    }
}

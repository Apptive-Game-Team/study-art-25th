using UnityEngine;

namespace ArtUnityWorkshop
{
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody2D), typeof(CharacterStats))]
    public sealed class EnemyMovement : MonoBehaviour
    {
        [SerializeField, InspectorName("순찰 범위"), Tooltip("시작 위치를 기준으로 한 좌우 X 거리입니다. Unity unit 단위입니다.")]
        private Vector2 patrolOffsets = new Vector2(-2, 2);
        [SerializeField, InspectorName("오른쪽으로 시작"), Tooltip("켜면 오른쪽, 끄면 왼쪽으로 순찰을 시작합니다.")]
        private bool startRight = true;
        private Rigidbody2D body;
        private CharacterStats stats;
        private CharacterAnimationPresenter presenter;
        private RigidbodyConstraints2D previousConstraints;
        private float left, right;
        private int direction;
        public bool IsBattleLocked { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            stats = GetComponent<CharacterStats>();
            presenter = GetComponent<CharacterAnimationPresenter>();
            left = body.position.x + Mathf.Min(patrolOffsets.x, patrolOffsets.y);
            right = body.position.x + Mathf.Max(patrolOffsets.x, patrolOffsets.y);
            direction = startRight ? 1 : -1;
        }

        private void OnEnable()
        {
            var battle = BattleCoordinator.FindInScene(gameObject.scene);
            if (battle != null && battle.IsBattleActive) SetBattleLocked(true);
        }

        private void FixedUpdate()
        {
            if (IsBattleLocked) { body.linearVelocity = Vector2.zero; return; }
            // Physics integration can finish a fraction short of the exact endpoint.
            if (body.position.x <= left + .01f) direction = 1;
            else if (body.position.x >= right - .01f) direction = -1;
            float velocity = stats.IsAlive && right > left ? direction * stats.MoveSpeed : 0;
            // Clamp the next physics step to the patrol interval, even at high speeds.
            float next = Mathf.Clamp(body.position.x + velocity * Time.fixedDeltaTime, left, right);
            body.linearVelocity = new Vector2(stats.IsAlive ? (next - body.position.x) / Time.fixedDeltaTime : 0, body.linearVelocity.y);
            if (presenter != null) presenter.SetMovement(velocity != 0 ? direction : 0);
        }

        public void SetBattleLocked(bool locked)
        {
            if (IsBattleLocked == locked) return;
            if (body == null) body = GetComponent<Rigidbody2D>();
            if (locked) previousConstraints = body.constraints;
            IsBattleLocked = locked;
            body.linearVelocity = Vector2.zero;
            body.constraints = locked ? RigidbodyConstraints2D.FreezeAll : previousConstraints;
            if (presenter != null) presenter.SetMovement(0);
        }

        private void OnDisable()
        {
            if (body != null) body.linearVelocity = Vector2.zero;
        }
    }
}

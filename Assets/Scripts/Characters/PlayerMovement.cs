using UnityEngine;

namespace ArtUnityWorkshop
{
    [DisallowMultipleComponent, RequireComponent(typeof(Rigidbody2D), typeof(CharacterStats), typeof(Combatant))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField, InspectorName("입력 연결"), Tooltip("비워 두면 같은 Scene의 InputRouter를 연결합니다.")]
        private InputRouter inputRouter;
        private Rigidbody2D body;
        private CharacterStats stats;
        private CharacterAnimationPresenter presenter;
        private RigidbodyConstraints2D previousConstraints;
        public bool IsBattleLocked { get; private set; }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            stats = GetComponent<CharacterStats>();
            presenter = GetComponent<CharacterAnimationPresenter>();
            if (inputRouter == null)
                foreach (var root in gameObject.scene.GetRootGameObjects())
                {
                    inputRouter = root.GetComponentInChildren<InputRouter>();
                    if (inputRouter != null) break;
                }
            if (inputRouter == null)
            {
                Debug.LogWarning("이 Scene에 InputRouter를 배치하거나 입력 연결을 지정해 주세요.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            var battle = BattleCoordinator.FindInScene(gameObject.scene);
            if (battle != null && battle.IsBattleActive) SetBattleLocked(true);
        }

        private void FixedUpdate()
        {
            int direction = IsBattleLocked || !stats.IsAlive || inputRouter == null ? 0 : inputRouter.Horizontal;
            body.linearVelocity = IsBattleLocked ? Vector2.zero : new Vector2(direction * stats.MoveSpeed, body.linearVelocity.y);
            if (presenter != null) presenter.SetMovement(direction);
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
            if (body != null) body.linearVelocity = IsBattleLocked ? Vector2.zero : new Vector2(0, body.linearVelocity.y);
            if (presenter != null) presenter.SetMovement(0);
        }
    }
}

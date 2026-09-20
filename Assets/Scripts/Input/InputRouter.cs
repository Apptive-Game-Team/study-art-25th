using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ArtUnityWorkshop
{
    [DisallowMultipleComponent]
    public sealed class InputRouter : MonoBehaviour
    {
        [Header("입력 설정")]
        [SerializeField, InspectorName("입력 액션"), Tooltip("MoveLeft, MoveRight, Interact가 있는 입력 에셋입니다. 키는 이 에셋의 Binding에서 변경합니다.")]
        private InputActionAsset inputActions;

        private InputAction left;
        private InputAction right;
        private InputAction interact;
        public InputActionAsset RuntimeActions { get; private set; }
        public event Action Interacted;
        public int Horizontal => isActiveAndEnabled && left != null && right != null
            ? (right.IsPressed() ? 1 : 0) - (left.IsPressed() ? 1 : 0) : 0;

        private void Awake()
        {
            if (inputActions == null)
            {
                Debug.LogError("입력 액션 에셋을 연결해 주세요.", this);
                enabled = false;
                return;
            }
            RuntimeActions = Instantiate(inputActions);
            left = RuntimeActions.FindAction("Gameplay/MoveLeft");
            right = RuntimeActions.FindAction("Gameplay/MoveRight");
            interact = RuntimeActions.FindAction("Gameplay/Interact");
            if (left == null || right == null || interact == null)
            {
                Debug.LogError("Gameplay의 MoveLeft, MoveRight, Interact 액션을 확인해 주세요.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (interact == null) return;
            interact.performed += OnInteract;
            RuntimeActions.Enable();
        }

        private void OnDisable()
        {
            if (interact != null) interact.performed -= OnInteract;
            if (RuntimeActions != null) RuntimeActions.Disable();
        }

        private void OnDestroy()
        {
            if (RuntimeActions != null) Destroy(RuntimeActions);
        }

        private void OnInteract(InputAction.CallbackContext context) => Interacted?.Invoke();

        public string BindingLabel(string actionName) =>
            RuntimeActions != null ? RuntimeActions.FindAction("Gameplay/" + actionName)?.GetBindingDisplayString() ?? "—" : "—";
    }
}

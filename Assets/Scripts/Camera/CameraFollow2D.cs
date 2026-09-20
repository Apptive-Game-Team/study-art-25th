using UnityEngine;

namespace ArtUnityWorkshop
{
    [RequireComponent(typeof(Camera))]
    public sealed class CameraFollow2D : MonoBehaviour
    {
        [SerializeField, InspectorName("따라갈 대상"), Tooltip("현재 Scene의 Player를 연결합니다.")]
        private Transform target;
        [SerializeField, InspectorName("좌우 이동 범위"), Tooltip("카메라 중심의 최소·최대 X 좌표입니다. Unity unit 단위입니다.")]
        private Vector2 horizontalLimits = new Vector2(-8, 8);

        private void LateUpdate()
        {
            if (target == null) return;
            var position = transform.position;
            position.x = Mathf.Clamp(target.position.x, horizontalLimits.x, horizontalLimits.y);
            transform.position = position;
        }
    }
}

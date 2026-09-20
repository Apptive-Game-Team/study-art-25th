using UnityEngine;

namespace ArtUnityWorkshop
{
    public sealed class EnemyIdentity : MonoBehaviour
    {
        [SerializeField, InspectorName("적 저장 ID"), Tooltip("Battle Scene에서 적마다 다른 이름을 지정하세요. 실행 중 체력·처치 상태를 복원할 때 사용합니다.")]
        private string enemyId = "Enemy";
        public string Id => string.IsNullOrWhiteSpace(enemyId) ? gameObject.name : enemyId;
    }
}

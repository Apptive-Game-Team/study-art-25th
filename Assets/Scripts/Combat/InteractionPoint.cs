using UnityEngine;

namespace ArtUnityWorkshop
{
    public enum InteractionKind { Portal, Healing, Trophy }
    public sealed class InteractionPoint : MonoBehaviour
    {
        [SerializeField, InspectorName("상호작용 종류"), Tooltip("포탈, 전량 회복, 트로피 포즈 변경 중 선택합니다.")]
        private InteractionKind kind;
        [SerializeField, InspectorName("안내 문구"), Tooltip("가까이 접근했을 때 표시할 이름입니다.")]
        private string label = "상호작용";
        [SerializeField, Min(.1f), InspectorName("사용 거리"), Tooltip("캐릭터와의 가로 거리입니다. Unity unit, 최소 0.1입니다.")]
        private float range = 1.5f;
        [SerializeField, InspectorName("목적 Scene"), Tooltip("포탈이 이동할 Build Settings의 Scene 이름입니다.")]
        private string destination = "Town";
        [SerializeField, InspectorName("트로피 전시"), Tooltip("포즈를 변경할 TrophyDisplay입니다.")]
        private TrophyDisplay trophy;
        public string Label => label;
        public float Range => Mathf.Max(.1f, range);
        public void Use(SceneFlow flow)
        {
            if (kind == InteractionKind.Portal) flow.Travel(destination);
            else if (kind == InteractionKind.Healing) flow.Player.Stats.RestoreHealth(flow.Player.Stats.MaxHealth);
            else if (trophy != null) trophy.NextPose();
        }
    }
}

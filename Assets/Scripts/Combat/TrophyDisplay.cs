using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace ArtUnityWorkshop
{
    [ExecuteAlways]
    public sealed class TrophyDisplay : MonoBehaviour
    {
        [SerializeField, InspectorName("강제 전시"), Tooltip("획득 여부와 별개로 Edit Mode와 Play Mode에서 표시합니다.")]
        private bool displayImmediately;

        [SerializeField, InspectorName("Humanoid 모델"), Tooltip("유효한 Humanoid Avatar가 있는 모델 Prefab입니다.")]
        private GameObject modelPrefab;

        [SerializeField, InspectorName("모델 부모"), Tooltip("전용 트로피 카메라 앞의 ModelRoot입니다.")]
        private Transform modelRoot;

        [SerializeField, InspectorName("모델 위치"), Tooltip("모델의 로컬 위치입니다. Unity unit 단위입니다.")]
        private Vector3 modelOffset = new Vector3(0, 1.1f, 0);

        [SerializeField, Min(.01f), InspectorName("모델 크기"), Tooltip("모델 배율입니다. 최소 0.01입니다.")]
        private float modelScale = 1;

        [SerializeField, InspectorName("회전 속도"), Tooltip("전시 중 모델의 Y축 회전 속도입니다.")]
        private float rotationSpeed = 30f;

        [SerializeField, InspectorName("포즈 Clip"), Tooltip("Humanoid Clip을 순서대로 재생합니다. Root Motion은 사용하지 않습니다.")]
        private AnimationClip[] poses;

        [SerializeField, InspectorName("전시 이미지"), Tooltip("Render Texture를 표시하는 World Space Canvas입니다.")]
        private GameObject displayImage;

        [SerializeField, HideInInspector]
        private GameObject instance, builtPrefab;

        private PlayableGraph graph;
        private int poseIndex;
        private AnimationClip currentClip;

        public int PoseIndex => poseIndex;

        public bool IsDisplayed =>
            displayImmediately ||
            (Application.isPlaying && GameSession.TrophyOwned);

        private void Update()
        {
            if (modelRoot == null || !gameObject.scene.IsValid())
                return;

            bool visible = IsDisplayed;

            if (displayImage != null)
                displayImage.SetActive(visible);

            if (modelRoot.gameObject.activeSelf != visible)
                modelRoot.gameObject.SetActive(visible);

            if (!visible || modelPrefab == null)
                return;

            // 모델 생성
            if (instance == null || builtPrefab != modelPrefab)
            {
                ClearGraph();

                if (instance != null)
                {
                    if (Application.isPlaying)
                        Destroy(instance);
                    else
                        DestroyImmediate(instance);
                }

                instance = Instantiate(modelPrefab, modelRoot);
                instance.name = "TrophyModel";
                builtPrefab = modelPrefab;

                // 초기 방향
                instance.transform.localRotation =
                    Quaternion.Euler(0, 180, 0);

                foreach (var t in instance.GetComponentsInChildren<Transform>(true))
                    t.gameObject.layer = modelRoot.gameObject.layer;
            }

            instance.transform.localPosition = modelOffset;
            instance.transform.localScale =
                Vector3.one * Mathf.Max(.01f, modelScale);

            // 전시 중 회전
            if (Application.isPlaying)
            {
                instance.transform.Rotate(
                    Vector3.up,
                    rotationSpeed * Time.deltaTime,
                    Space.Self
                );
            }

            // 포즈 선택
            var clip =
                poses != null && poses.Length > 0
                    ? poses[poseIndex % poses.Length]
                    : null;

            if (clip == null)
            {
                ClearGraph();
                return;
            }

            // 포즈 적용
            if (!graph.IsValid() || currentClip != clip)
            {
                ClearGraph();

                var animator = instance.GetComponentInChildren<Animator>();

                if (animator == null ||
                    animator.avatar == null ||
                    !animator.avatar.isValid ||
                    !animator.avatar.isHuman)
                {
                    return;
                }

                animator.applyRootMotion = false;

                graph = PlayableGraph.Create("TrophyPose");
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);

                var playable =
                    AnimationClipPlayable.Create(graph, clip);

                playable.SetTime(clip.length * .5);
                playable.SetSpeed(0);

                AnimationPlayableOutput
                    .Create(graph, "Pose", animator)
                    .SetSourcePlayable(playable);

                graph.Play();
                currentClip = clip;
            }

            graph.Evaluate(0);
        }

        public void NextPose()
        {
            if (!IsDisplayed ||
                poses == null ||
                poses.Length == 0)
                return;

            poseIndex = (poseIndex + 1) % poses.Length;
            currentClip = null;
        }

        private void ClearGraph()
        {
            if (graph.IsValid())
                graph.Destroy();

            currentClip = null;
        }

        private void OnDisable()
        {
            ClearGraph();
        }
    }
}
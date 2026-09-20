using System;
using System.Collections;
using UnityEngine;

namespace ArtUnityWorkshop
{
    public sealed class CharacterAnimationPresenter : MonoBehaviour
    {
        [SerializeField, InspectorName("애니메이터"), Tooltip("Visual의 Animator입니다. 상태 이름은 Idle, Move, Attack, Hit, Death입니다.")]
        private Animator animator;
        [SerializeField, InspectorName("외형 루트"), Tooltip("방향 전환과 사망 시 숨김을 적용할 자식 Visual입니다. 이 Component가 있는 루트는 지정하지 마세요.")]
        private GameObject visualRoot;
        [SerializeField, Min(0.01f), InspectorName("사망 대체 대기 시간"), Tooltip("Clip 누락·정지 시 기다릴 최대 시간(초)입니다. 정상 재생 중인 Clip은 끝까지 기다립니다.")]
        private float deathFallbackDuration = 1;
        private Coroutine routine;
        private Action deathCompleted;
        private bool dying;
        public bool IsPresenting => routine != null;

        public void SetMovement(int direction)
        {
            if (dying || !isActiveAndEnabled) return;
            if (direction != 0 && visualRoot != null && visualRoot != gameObject && visualRoot.transform.IsChildOf(transform))
            {
                var scale = visualRoot.transform.localScale;
                scale.x = Mathf.Abs(scale.x) * (direction > 0 ? 1 : -1);
                visualRoot.transform.localScale = scale;
            }
            SetMoving(direction != 0);
        }

        public void SetMoving(bool moving)
        {
            if (dying || routine != null || !isActiveAndEnabled) return;
            string state = moving ? "Move" : "Idle";
            if (CanPlay(state) && !animator.GetCurrentAnimatorStateInfo(0).IsName(state)) animator.Play(state, 0, 0);
        }

        public void PlayAction(string state)
        {
            if (dying || !isActiveAndEnabled) return;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(PlayAndWait(state, false));
        }

        public void PlayDeath(Action completed)
        {
            if (dying) return;
            dying = true;
            deathCompleted = completed;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(PlayAndWait("Death", true));
        }

        private bool CanPlay(string state) => animator != null && animator.isActiveAndEnabled &&
            animator.runtimeAnimatorController != null && animator.HasState(0, Animator.StringToHash("Base Layer." + state));

        private IEnumerator PlayAndWait(string state, bool death)
        {
            // Newly spawned Animators need their first initialization frame.
            yield return null;
            float stalled = 0;
            float previous = -1;
            bool available = CanPlay(state);
            if (available) animator.Play(state, 0, 0);
            yield return null;
            while (stalled < Mathf.Max(.01f, deathFallbackDuration))
            {
                if (available && CanPlay(state))
                {
                    var info = animator.GetCurrentAnimatorStateInfo(0);
                    if (!info.IsName(state)) break;
                    if (info.normalizedTime >= 1) break;
                    if (info.normalizedTime > previous && animator.GetCurrentAnimatorClipInfoCount(0) > 0)
                    {
                        stalled = 0;
                        previous = info.normalizedTime;
                    }
                    else stalled += Time.unscaledDeltaTime;
                }
                else stalled += Time.unscaledDeltaTime;
                yield return null;
            }
            routine = null;
            if (death) FinishDeath();
            else if (CanPlay("Idle")) animator.Play("Idle", 0, 0);
        }

        private void FinishDeath()
        {
            if (visualRoot != null && visualRoot != gameObject && visualRoot.transform.IsChildOf(transform)) visualRoot.SetActive(false);
            var callback = deathCompleted;
            deathCompleted = null;
            callback?.Invoke();
        }

        private void OnDisable()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            if (dying) FinishDeath();
        }
    }
}

using System;
using UnityEngine;

namespace ArtUnityWorkshop
{
    [RequireComponent(typeof(CharacterStats))]
    public sealed class Combatant : MonoBehaviour
    {
        [SerializeField, InspectorName("애니메이션 표현"), Tooltip("공격·피격·사망을 표시합니다. 없어도 피해 판정은 동작합니다.")]
        private CharacterAnimationPresenter animationPresenter;
        [SerializeField, InspectorName("피격 파티클"), Tooltip("피해가 발생하면 생성합니다. Loop를 꺼도 켜도 최대 10초 후 정리합니다.")]
        private ParticleSystem hitParticlePrefab;
        [SerializeField, InspectorName("피격 위치"), Tooltip("비어 있으면 캐릭터 위치에 표시합니다.")]
        private Transform hitEffectAnchor;
        private CharacterStats stats;
        private long lastAttackSequence = -1;
        private bool deathReported;
        public CharacterStats Stats => stats;
        public long NextAttackSequence => lastAttackSequence + 1;
        public event Action Died;
        public event Action DeathPresentationCompleted;

        private void Awake() => stats = GetComponent<CharacterStats>();
        private void OnEnable() => stats.HealthChanged += OnHealthChanged;
        private void OnDisable() => stats.HealthChanged -= OnHealthChanged;

        // The battle coordinator supplies an increasing sequence per attacker's lifetime.
        public bool TryAttack(Combatant target, long sequence)
        {
            if (!isActiveAndEnabled || !stats.IsAlive || deathReported || target == null || target == this ||
                !target.isActiveAndEnabled || !target.stats.IsAlive || sequence < 0 || sequence <= lastAttackSequence) return false;
            lastAttackSequence = sequence;
            if (animationPresenter != null) animationPresenter.PlayAction("Attack");
            target.TakeDamage(stats.AttackPower);
            return true;
        }

        public int TakeDamage(int amount)
        {
            if (!isActiveAndEnabled || deathReported) return 0;
            int applied = stats.TakeDamage(amount);
            if (applied > 0 && hitParticlePrefab != null)
            {
                var effect = Instantiate(hitParticlePrefab, hitEffectAnchor != null ? hitEffectAnchor.position : transform.position, Quaternion.identity);
                var main = effect.main;
                main.loop = false;
                effect.Play(true);
                Destroy(effect.gameObject, Mathf.Clamp(main.duration + main.startLifetime.constantMax + .5f, .1f, 10));
            }
            if (applied > 0 && stats.IsAlive && animationPresenter != null) animationPresenter.PlayAction("Hit");
            return applied;
        }

        private void OnHealthChanged()
        {
            if (stats.IsAlive || deathReported) return;
            deathReported = true;
            Died?.Invoke();
            if (animationPresenter != null && animationPresenter.isActiveAndEnabled)
                animationPresenter.PlayDeath(FinishDeathPresentation);
            else FinishDeathPresentation();
        }

        private void FinishDeathPresentation()
        {
            foreach (var collider in GetComponentsInChildren<Collider2D>(true))
                collider.enabled = false;
            var body = GetComponent<Rigidbody2D>();
            if (body != null) body.simulated = false;
            DeathPresentationCompleted?.Invoke();
        }
    }
}

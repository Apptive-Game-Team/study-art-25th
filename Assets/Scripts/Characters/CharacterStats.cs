using System;
using UnityEngine;

namespace ArtUnityWorkshop
{
    public sealed class CharacterStats : MonoBehaviour
    {
        [SerializeField, Min(1), InspectorName("최대 체력"), Tooltip("새 게임 시작 체력입니다. 1 이상의 정수입니다.")]
        private int maxHealth = 100;
        [SerializeField, Min(0), InspectorName("공격력"), Tooltip("한 번 공격할 때 줄 체력입니다. 0 이상의 정수입니다.")]
        private int attackPower = 10;
        [SerializeField, Min(0), InspectorName("이동 속도"), Tooltip("초당 이동하는 Unity unit입니다. 0 이상입니다.")]
        private float moveSpeed = 4;
        private int currentHealth;
        public int MaxHealth => Mathf.Max(1, maxHealth);
        public int CurrentHealth => currentHealth;
        public int AttackPower => Mathf.Max(0, attackPower);
        public float MoveSpeed => Mathf.Max(0, moveSpeed);
        public bool IsAlive => currentHealth > 0;
        public event Action HealthChanged;

        private void Awake() => currentHealth = MaxHealth;
        private void OnValidate()
        {
            maxHealth = Mathf.Max(1, maxHealth);
            attackPower = Mathf.Max(0, attackPower);
            moveSpeed = Mathf.Max(0, moveSpeed);
        }

        public int TakeDamage(int amount)
        {
            int damage = Mathf.Min(currentHealth, Mathf.Max(0, amount));
            if (damage == 0) return 0;
            currentHealth -= damage;
            HealthChanged?.Invoke();
            return damage;
        }

        public void RestoreHealth(int health)
        {
            int next = Mathf.Clamp(health, 0, MaxHealth);
            if (next == currentHealth) return;
            currentHealth = next;
            HealthChanged?.Invoke();
        }
    }
}

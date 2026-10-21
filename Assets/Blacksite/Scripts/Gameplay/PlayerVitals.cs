using UnityEngine;

namespace Blacksite.Gameplay
{
    public sealed class PlayerVitals : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maxHealth = 100;
        [SerializeField, Min(0f)] private float recoveryDelay = 6f;
        [SerializeField, Min(0f)] private float recoveryPerSecond = 3f;
        private float lastDamageAt = -100f;
        private float damageFlashUntil;
        private float recoveryAccumulated;

        public int Health { get; private set; }
        public int MaxHealth => maxHealth;
        public bool IsDead => Health <= 0;
        public float DamageFlash => Mathf.Clamp01((damageFlashUntil - Time.time) / .45f);
        public event System.Action Died;

        private void Awake() => Health = maxHealth;

        private void Update()
        {
            if (IsDead || Health >= maxHealth || Time.time - lastDamageAt < recoveryDelay) return;
            recoveryAccumulated += recoveryPerSecond * Time.deltaTime;
            int points = Mathf.FloorToInt(recoveryAccumulated);
            if (points <= 0) return;
            recoveryAccumulated -= points;
            Health = Mathf.Min(maxHealth, Health + points);
        }

        public void TakeDamage(int amount, string source)
        {
            if (amount <= 0 || IsDead) return;
            Health = Mathf.Max(0, Health - amount);
            lastDamageAt = Time.time;
            recoveryAccumulated = 0f;
            damageFlashUntil = Time.time + .45f;
            Debug.Log($"BLACKSITE_DAMAGE source={source} amount={amount} health={Health}");
            if (IsDead) Died?.Invoke();
        }
    }
}

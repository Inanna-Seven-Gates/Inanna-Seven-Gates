using System;
using UnityEngine;

namespace Inanna.Combat
{
    /// <summary>Dados de um golpe. Imutável: quem causa o dano preenche, quem recebe só lê.</summary>
    public readonly struct DamageInfo
    {
        public readonly float Amount;
        public readonly Vector3 SourcePosition;
        /// <summary>Direção horizontal do golpe (de quem bateu para quem apanhou).</summary>
        public readonly Vector3 Direction;
        public readonly float Knockback;
        public readonly GameObject Source;

        public DamageInfo(float amount, Vector3 sourcePosition, Vector3 direction, float knockback, GameObject source)
        {
            Amount = amount;
            SourcePosition = sourcePosition;
            Direction = direction;
            Knockback = knockback;
            Source = source;
        }
    }

    /// <summary>Qualquer coisa que pode levar dano</summary>
    public interface IDamageable
    {
        bool IsDead { get; }
        void TakeDamage(DamageInfo info);
    }

    /// <summary>Vida genérica.</summary>
    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] float maxHealth = 3f;
        [Tooltip("Tempo de invulnerabilidade após levar dano (útil para o jogador).")]
        [SerializeField] float invulnerabilityTime = 0f;

        public float Max => maxHealth;
        public float Current { get; private set; }
        public bool IsDead { get; private set; }

        /// <summary>Quando true, ignora todo dano.</summary>
        public bool Invulnerable { get; set; }

        public event Action<DamageInfo> Damaged;
        public event Action Died;
        /// <summary>(vidaAtual, vidaMaxima)</summary>
        public event Action<float, float> Changed;

        float _invulnerableUntil;

        void Awake() => Current = maxHealth;

        public void TakeDamage(DamageInfo info)
        {
            if (IsDead || Invulnerable || Time.time < _invulnerableUntil) return;

            Current = Mathf.Max(0f, Current - info.Amount);
            _invulnerableUntil = Time.time + invulnerabilityTime;

            Damaged?.Invoke(info);
            Changed?.Invoke(Current, maxHealth);

            if (Current <= 0f)
            {
                IsDead = true;
                Died?.Invoke();
            }
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            Current = Mathf.Min(maxHealth, Current + amount);
            Changed?.Invoke(Current, maxHealth);
        }

        public void ResetHealth()
        {
            IsDead = false;
            Current = maxHealth;
            Changed?.Invoke(Current, maxHealth);
        }
    }
}

using UnityEngine;
using Inanna.Combat;

namespace Inanna.Levels
{
    /// <summary>
    /// Armadilha: tira uma fração da vida máxima de quem entra no trigger (layer Player).
    /// A da entrada da P2 tira quase tudo, uma vez só, sem matar. As outras rearmam depois de um tempo.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TrapZone : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] float damageFractionOfMax = 0.15f;
        [Tooltip("Nunca deixa a vida abaixo de 1 (armadilha de susto, não de morte).")]
        [SerializeField] bool nonLethal;
        [SerializeField] bool singleUse;
        [SerializeField, Min(0.1f)] float rearmSeconds = 1.5f;
        [SerializeField] LayerMask playerMask;

        float _readyAt;
        bool _spent;

        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
            playerMask = LayerMask.GetMask("Player");
        }

        void Awake() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerStay(Collider other)
        {
            if (_spent || Time.time < _readyAt || (playerMask.value & (1 << other.gameObject.layer)) == 0) return;
            var health = other.GetComponentInParent<Health>();
            if (!health || health.IsDead) return;

            float amount = health.Max * damageFractionOfMax;
            if (nonLethal) amount = Mathf.Min(amount, health.Current - 1f);
            if (amount > 0f)
            {
                var direction = other.transform.position - transform.position;
                direction.y = 0f;
                health.TakeDamage(new DamageInfo(amount, transform.position, direction.normalized, 0f, gameObject));
            }

            _readyAt = Time.time + rearmSeconds;
            _spent = singleUse;
        }
    }
}

using System;
using UnityEngine;
using Inanna.Combat;

namespace Inanna.Levels
{
    /// <summary>
    /// Armadilha: tira uma fração da vida máxima de quem entra no trigger (layer Player).
    /// A da entrada da P2 tira quase tudo, uma vez só, sem matar. As outras rearmam depois de um tempo.
    /// Chão de armadilha com pedras seguras (sala de armadilhas da P2): os <see cref="safeAreas"/> não dão dano,
    /// e só machuca quem fica fora delas por mais de <see cref="graceSeconds"/> (atravessar com o dash não machuca).
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
        [Tooltip("Áreas seguras dentro da armadilha (ex.: pedras). Vale o ponto central da Inanna visto de cima.")]
        [SerializeField] Collider[] safeAreas = Array.Empty<Collider>();
        [Tooltip("Tempo seguido fora das áreas seguras antes de machucar. 0 = na hora.")]
        [SerializeField, Min(0f)] float graceSeconds;

        float _readyAt;
        float _exposedSince = -1f;
        bool _spent;

        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
            playerMask = LayerMask.GetMask("Player");
        }

        void Awake() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerExit(Collider other)
        {
            if ((playerMask.value & (1 << other.gameObject.layer)) != 0) _exposedSince = -1f;
        }

        void OnTriggerStay(Collider other)
        {
            // O Unity manda avisos de trigger até para componente desligado (ex.: TrapShutdown): ignora.
            if (!isActiveAndEnabled || _spent || (playerMask.value & (1 << other.gameObject.layer)) == 0) return;

            if (IsOnSafeArea(other.bounds.center))
            {
                _exposedSince = -1f;
                return;
            }
            if (_exposedSince < 0f) _exposedSince = Time.time;
            if (Time.time - _exposedSince < graceSeconds || Time.time < _readyAt) return;

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

        // Forma exata da área (vale para pedra girada), olhando de cima: o ponto é levado à altura da área.
        bool IsOnSafeArea(Vector3 point)
        {
            foreach (var area in safeAreas)
            {
                if (!area) continue;
                var probe = new Vector3(point.x, area.bounds.center.y, point.z);
                if ((area.ClosestPoint(probe) - probe).sqrMagnitude < 0.0001f) return true;
            }
            return false;
        }
    }
}

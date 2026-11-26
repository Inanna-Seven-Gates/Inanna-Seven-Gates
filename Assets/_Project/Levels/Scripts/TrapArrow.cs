using UnityEngine;
using Inanna.Combat;

namespace Inanna.Levels
{
    /// <summary>
    /// Flecha de armadilha: anda reto, acerta o primeiro IDamageable na frente (Inanna ou inimigo) e some ao bater
    /// em parede, chão ou depois do alcance. Criada pelo <see cref="ArrowLauncher"/>.
    /// </summary>
    public class TrapArrow : MonoBehaviour
    {
        const float HitRadius = 0.2f;

        Vector3 _direction;
        float _speed;
        float _remaining;
        float _damageFractionOfMax;
        LayerMask _hitMask;
        LayerMask _blockMask;
        GameObject _source;

        public void Launch(Vector3 direction, float speed, float maxDistance, float damageFractionOfMax,
            LayerMask hitMask, LayerMask blockMask, GameObject source)
        {
            _direction = direction.normalized;
            _speed = speed;
            _remaining = maxDistance;
            _damageFractionOfMax = damageFractionOfMax;
            _hitMask = hitMask;
            _blockMask = blockMask;
            _source = source;
            transform.rotation = Quaternion.LookRotation(_direction, Vector3.up);
        }

        void Update()
        {
            float step = Mathf.Min(_speed * Time.deltaTime, _remaining);
            var mask = _hitMask | _blockMask;
            if (Physics.SphereCast(transform.position, HitRadius, _direction, out var hit, step, mask, QueryTriggerInteraction.Ignore))
            {
                if ((_hitMask.value & (1 << hit.collider.gameObject.layer)) != 0) Damage(hit.collider);
                Destroy(gameObject);
                return;
            }

            transform.position += _direction * step;
            _remaining -= step;
            if (_remaining <= 0f) Destroy(gameObject);
        }

        void Damage(Collider target)
        {
            var damageable = target.GetComponentInParent<IDamageable>();
            if (damageable == null) return;
            var health = target.GetComponentInParent<Health>();
            float amount = health ? health.Max * _damageFractionOfMax : 1f;
            damageable.TakeDamage(new DamageInfo(amount, transform.position, _direction, 0f, _source));
        }
    }
}

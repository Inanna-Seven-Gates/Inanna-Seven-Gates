using UnityEngine;
using UnityEngine.Events;
using Inanna.Combat;

namespace Inanna.Levels
{
    /// <summary>
    /// Parede falsa (sala secreta): some no primeiro golpe. Fica na layer Enemy porque é a layer que os ataques
    /// do player procuram. Não tem Health, então não conta como inimigo para o CombatRoom.
    /// </summary>
    public class BreakableWall : MonoBehaviour, IDamageable
    {
        [SerializeField] UnityEvent onBroken = new UnityEvent();

        public bool IsDead { get; private set; }

        public void TakeDamage(DamageInfo info)
        {
            if (IsDead || info.Amount <= 0f) return;
            IsDead = true;
            onBroken.Invoke();
            gameObject.SetActive(false);
        }
    }
}

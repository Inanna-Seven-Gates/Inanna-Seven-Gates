using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Inanna.Combat;

namespace Inanna.Levels
{
    /// <summary>
    /// Sala de combate: quando a Inanna entra, as passagens trancam e só abrem quando todos os inimigos
    /// da sala morrem. Vai num objeto com um BoxCollider (trigger) um pouco menor que a sala, para a Inanna
    /// já estar longe da porta quando ela fecha. Inimigo = objeto com <see cref="Health"/> na layer Enemy
    /// dentro da sala (a alavanca, por exemplo, não conta).
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public class CombatRoom : MonoBehaviour
    {
        enum State { Waiting, Locked, Cleared }

        const float CheckInterval = 0.25f;

        [Tooltip("Componentes que implementam ILockable (ex.: RoomGate) nas passagens da sala.")]
        [SerializeField] MonoBehaviour[] gates = Array.Empty<MonoBehaviour>();
        [Tooltip("Quanto a sala vai além do trigger, em cada lado (x e z). A busca por inimigos usa a sala inteira.")]
        [SerializeField, Min(0f)] float roomMargin = 1.5f;
        [SerializeField] LayerMask playerMask;
        [SerializeField] LayerMask enemyMask;

        [Tooltip("Assim que a Inanna entra, antes de procurar inimigos (ex.: um spawner cria a onda aqui).")]
        [SerializeField] UnityEvent onPlayerEntered = new UnityEvent();
        [SerializeField] UnityEvent onLocked = new UnityEvent();
        [SerializeField] UnityEvent onCleared = new UnityEvent();

        readonly List<Health> _enemies = new List<Health>();
        readonly Collider[] _overlapBuffer = new Collider[64];
        BoxCollider _trigger;
        State _state = State.Waiting;

        public IEnumerable<ILockable> Gates
        {
            get
            {
                // "if (gate)" antes do "is": um componente destruído passa no "is".
                foreach (var gate in gates)
                    if (gate && gate is ILockable lockable)
                        yield return lockable;
            }
        }

        /// <summary>Área do trigger (onde a Inanna precisa estar para a sala fechar).</summary>
        public Bounds TriggerBounds => Trigger.bounds;

        /// <summary>A sala inteira: o trigger mais a margem dos lados.</summary>
        public Bounds RoomBounds
        {
            get
            {
                var bounds = TriggerBounds;
                bounds.Expand(new Vector3(roomMargin * 2f, 0f, roomMargin * 2f));
                return bounds;
            }
        }

        public bool IsCleared => _state == State.Cleared;

        BoxCollider Trigger => _trigger ? _trigger : _trigger = GetComponent<BoxCollider>();

        void Reset()
        {
            Trigger.isTrigger = true;
            playerMask = LayerMask.GetMask("Player");
            enemyMask = LayerMask.GetMask("Enemy");
        }

        void Awake() => Trigger.isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (_state != State.Waiting || (playerMask.value & (1 << other.gameObject.layer)) == 0) return;
            StartCoroutine(BeginEncounter());
        }

        IEnumerator BeginEncounter()
        {
            _state = State.Locked;
            onPlayerEntered.Invoke();
            // Espera um passo de física para pegar inimigos criados no onPlayerEntered.
            yield return new WaitForFixedUpdate();

            CollectEnemies();
            if (_enemies.Count == 0)
            {
                Clear();
                yield break;
            }

            foreach (var gate in Gates) gate.Lock();
            onLocked.Invoke();

            var wait = new WaitForSeconds(CheckInterval);
            while (HasLivingEnemies()) yield return wait;
            Clear();
        }

        void CollectEnemies()
        {
            _enemies.Clear();
            var room = RoomBounds;
            int count = Physics.OverlapBoxNonAlloc(room.center, room.extents, _overlapBuffer, Quaternion.identity, enemyMask, QueryTriggerInteraction.Collide);
            for (int i = 0; i < count; i++)
            {
                var health = _overlapBuffer[i].GetComponentInParent<Health>();
                if (health && !health.IsDead && !_enemies.Contains(health)) _enemies.Add(health);
            }
        }

        // Inimigo destruído também conta como derrotado, para a sala nunca ficar trancada para sempre.
        bool HasLivingEnemies()
        {
            foreach (var enemy in _enemies)
                if (enemy && !enemy.IsDead) return true;
            return false;
        }

        void Clear()
        {
            _state = State.Cleared;
            foreach (var gate in Gates) gate.Unlock();
            onCleared.Invoke();
        }

        void OnValidate()
        {
            for (int i = 0; i < gates.Length; i++)
            {
                if (gates[i] == null || gates[i] is ILockable) continue;
                Debug.LogWarning($"{name}: {gates[i].GetType().Name} não implementa ILockable e foi removido das passagens.", this);
                gates[i] = null;
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.5f, 0.1f, 0.8f);
            Gizmos.DrawWireCube(TriggerBounds.center, TriggerBounds.size);
            Gizmos.color = new Color(1f, 0.2f, 0.1f, 0.4f);
            Gizmos.DrawWireCube(RoomBounds.center, RoomBounds.size);
        }
    }
}

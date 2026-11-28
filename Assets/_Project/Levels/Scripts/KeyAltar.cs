using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Inanna.Levels
{
    /// <summary>
    /// Lugar onde a Inanna coloca as chaves (ex.: círculo da arena da P2). Quando ela entra no trigger com todas
    /// as chaves, dispara <see cref="onKeysPlaced"/> uma vez (ex.: começa a raid pelo CombatRoom.BeginEncounter).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class KeyAltar : MonoBehaviour
    {
        [Tooltip("Chaves exigidas (componentes que implementam ISwitch, ex.: KeyPickup).")]
        [SerializeField] MonoBehaviour[] requiredKeys = Array.Empty<MonoBehaviour>();
        [SerializeField] LayerMask playerMask;
        [SerializeField] UnityEvent onKeysPlaced = new UnityEvent();

        public bool KeysPlaced { get; private set; }

        bool HasAllKeys => requiredKeys.Where(k => k).OfType<ISwitch>().All(k => k.IsActivated);

        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
            playerMask = LayerMask.GetMask("Player");
        }

        void Awake() => GetComponent<Collider>().isTrigger = true;

        // OnTriggerStay: se ela entrou sem as chaves e voltou depois com elas, já parada no círculo, também conta.
        void OnTriggerStay(Collider other)
        {
            if (KeysPlaced || (playerMask.value & (1 << other.gameObject.layer)) == 0 || !HasAllKeys) return;
            KeysPlaced = true;
            onKeysPlaced.Invoke();
        }
    }
}

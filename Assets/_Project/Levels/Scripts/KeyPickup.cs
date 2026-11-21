using System;
using UnityEngine;
using UnityEngine.Events;

namespace Inanna.Levels
{
    /// <summary>
    /// Chave da fase (ex.: K1, K2). A Inanna pega ao encostar no trigger. Como <see cref="ISwitch"/>, pode abrir
    /// portas pelo <see cref="SwitchGate"/> e ser exigida pelo <see cref="KeyAltar"/>.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class KeyPickup : MonoBehaviour, ISwitch
    {
        [SerializeField] string keyName = "K1";
        [Tooltip("O que some quando a chave é pega (o modelo da chave).")]
        [SerializeField] GameObject visual;
        [SerializeField] LayerMask playerMask;
        [SerializeField] UnityEvent onCollected = new UnityEvent();

        public event Action<bool> StateChanged;

        public string KeyName => keyName;
        public bool IsActivated { get; private set; }

        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
            playerMask = LayerMask.GetMask("Player");
        }

        void Awake() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other)
        {
            if (IsActivated || (playerMask.value & (1 << other.gameObject.layer)) == 0) return;
            SetActivatedImmediate(true);
            onCollected.Invoke();
        }

        public void SetActivatedImmediate(bool activated)
        {
            if (visual) visual.SetActive(!activated);
            if (IsActivated == activated) return;
            IsActivated = activated;
            StateChanged?.Invoke(activated);
        }
    }
}

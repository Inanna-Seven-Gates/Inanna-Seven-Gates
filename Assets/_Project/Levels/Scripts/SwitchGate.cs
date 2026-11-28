using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Inanna.Levels
{
    /// <summary>
    /// Destranca passagens quando pelo menos <see cref="requiredCount"/> interruptores estão ligados.
    /// Exemplos: porta da K2 (2 de 2 alavancas), atalho da cripta (1 de 2 alavancas), atalho da casa (a K1).
    /// Depende só de <see cref="ISwitch"/> e <see cref="ILockable"/>.
    /// </summary>
    public class SwitchGate : MonoBehaviour
    {
        [Tooltip("Componentes que implementam ISwitch (DamageSwitch, KeyPickup...).")]
        [SerializeField] MonoBehaviour[] switches = Array.Empty<MonoBehaviour>();
        [SerializeField, Min(1)] int requiredCount = 1;
        [Tooltip("Componentes que implementam ILockable (ex.: RoomGate). Começam trancados.")]
        [SerializeField] MonoBehaviour[] lockables = Array.Empty<MonoBehaviour>();
        [SerializeField] UnityEvent onOpened = new UnityEvent();

        bool _open;

        public IEnumerable<ISwitch> Switches => switches.Where(s => s).OfType<ISwitch>();
        public IEnumerable<ILockable> Lockables => lockables.Where(l => l).OfType<ILockable>();
        public int RequiredCount => requiredCount;
        public bool IsOpen => _open;

        void Awake() => EvaluateImmediate();

        void OnEnable()
        {
            foreach (var item in Switches) item.StateChanged += OnSwitchChanged;
        }

        void OnDisable()
        {
            foreach (var item in Switches) item.StateChanged -= OnSwitchChanged;
        }

        void OnSwitchChanged(bool _) => Evaluate(animate: true);

        /// <summary>Aplica o estado na hora, sem animação. Serve para carregar save e para o validador.</summary>
        public void EvaluateImmediate() => Evaluate(animate: false);

        void Evaluate(bool animate)
        {
            bool open = Switches.Count(item => item.IsActivated) >= requiredCount;
            bool changed = open != _open;
            _open = open;

            foreach (var lockable in Lockables)
            {
                if (!animate) lockable.SetLockedImmediate(!open);
                else if (open) lockable.Unlock();
                else lockable.Lock();
            }
            if (changed && open && animate) onOpened.Invoke();
        }

        void OnValidate()
        {
            Validate(switches, typeof(ISwitch));
            Validate(lockables, typeof(ILockable));
        }

        void Validate(MonoBehaviour[] items, Type required)
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null || required.IsInstanceOfType(items[i])) continue;
                Debug.LogWarning($"{name}: {items[i].GetType().Name} não implementa {required.Name} e foi removido.", this);
                items[i] = null;
            }
        }
    }
}

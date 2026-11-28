using System;
using UnityEngine;
using UnityEngine.Events;

namespace Inanna.Levels
{
    /// <summary>
    /// Desliga armadilhas quando acionado (ex.: por uma alavanca <see cref="DamageSwitch"/>): desativa os componentes
    /// listados (ArrowLauncher, TrapZone...) e esconde o visual de perigo, para o jogador ver que ficou seguro.
    /// </summary>
    public class TrapShutdown : MonoBehaviour, IActivatable
    {
        [Tooltip("Componentes que param de funcionar (ex.: ArrowLauncher, TrapZone).")]
        [SerializeField] Behaviour[] trapsToDisable = Array.Empty<Behaviour>();
        [Tooltip("Visual de perigo que some (ex.: o chão vermelho).")]
        [SerializeField] GameObject[] dangerVisuals = Array.Empty<GameObject>();
        [SerializeField] UnityEvent onShutdown = new UnityEvent();

        public bool IsActivated { get; private set; }

        public void Activate()
        {
            if (IsActivated) return;
            SetActivatedImmediate(true);
            onShutdown.Invoke();
        }

        public void SetActivatedImmediate(bool activated)
        {
            IsActivated = activated;
            foreach (var trap in trapsToDisable) if (trap) trap.enabled = !activated;
            foreach (var visual in dangerVisuals) if (visual) visual.SetActive(!activated);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Inanna.Levels
{
    /// <summary>
    /// Placa de pressão: liga enquanto alguém está em cima (Inanna ou inimigo, conforme a máscara) e, ao ser pisada,
    /// dispara os alvos <see cref="ITriggerable"/> (ex.: lançador de dardos). Como <see cref="ISwitch"/>, também pode
    /// abrir portas pelo <see cref="SwitchGate"/>. Inimigos atraídos para cima dela também disparam a armadilha.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class PressurePlate : MonoBehaviour, ISwitch
    {
        [Tooltip("Componentes que implementam ITriggerable (ex.: ArrowLauncher em modo acionado).")]
        [SerializeField] MonoBehaviour[] targets = Array.Empty<MonoBehaviour>();
        [SerializeField] LayerMask stepMask;
        [Tooltip("Tempo mínimo entre dois disparos.")]
        [SerializeField, Min(0f)] float rearmSeconds = 1.5f;
        [Tooltip("A parte que afunda quando pisada (opcional).")]
        [SerializeField] Transform plateVisual;
        [SerializeField] float pressedDepth = 0.04f;

        readonly HashSet<Collider> _onTop = new HashSet<Collider>();
        Vector3 _visualRestPosition;
        float _readyAt;

        public event Action<bool> StateChanged;
        public bool IsActivated { get; private set; }

        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
            stepMask = LayerMask.GetMask("Player", "Enemy");
        }

        void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
            if (plateVisual) _visualRestPosition = plateVisual.localPosition;
        }

        void OnTriggerEnter(Collider other)
        {
            // O Unity manda avisos de trigger até para componente desligado: ignora.
            if (!isActiveAndEnabled || (stepMask.value & (1 << other.gameObject.layer)) == 0) return;
            _onTop.Add(other);
            if (_onTop.Count == 1) Press();
        }

        void OnTriggerExit(Collider other)
        {
            if (!_onTop.Remove(other) || _onTop.Count > 0) return;
            SetActivatedImmediate(false);
        }

        void Press()
        {
            SetActivatedImmediate(true);
            if (Time.time < _readyAt) return;
            _readyAt = Time.time + rearmSeconds;
            foreach (var target in targets)
                if (target && target is ITriggerable triggerable)
                    triggerable.Trigger();
        }

        public void SetActivatedImmediate(bool activated)
        {
            if (plateVisual) plateVisual.localPosition = _visualRestPosition + (activated ? Vector3.down * pressedDepth : Vector3.zero);
            if (IsActivated == activated) return;
            IsActivated = activated;
            StateChanged?.Invoke(activated);
        }

        void OnValidate()
        {
            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null || targets[i] is ITriggerable) continue;
                Debug.LogWarning($"{name}: {targets[i].GetType().Name} não implementa ITriggerable e foi removido dos alvos.", this);
                targets[i] = null;
            }
        }
    }
}

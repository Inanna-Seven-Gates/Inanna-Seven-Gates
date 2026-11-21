using System;
using UnityEngine;

namespace Inanna.Combat
{
    public enum GiharHandState
    {
        TwoInHand,   // pode bater e arremessar
        OneInHand,   // o outro está no ar: ainda pode bater e arremessar
        NoneInHand   // os dois no ar: não pode atacar, espera voltarem
    }

    /// <summary>
    /// Controla quantos gihars estão na mão.
    /// </summary>
    public class GiharLoadout : MonoBehaviour
    {
        [SerializeField] int maxGihars = 2;
        [Tooltip("Mostra um texto de debug no canto da tela.")]
        [SerializeField] bool showDebugHud = true;

        public int Max => maxGihars;
        public int InHand { get; private set; }
        public int InAir => maxGihars - InHand;

        public GiharHandState State => InHand >= 2 ? GiharHandState.TwoInHand
                                        : InHand == 1 ? GiharHandState.OneInHand
                                        : GiharHandState.NoneInHand;

        public bool CanMelee => InHand > 0;
        public bool CanThrow => InHand > 0;

        public event Action<GiharHandState> StateChanged;

        void Awake() => InHand = maxGihars;

        /// <summary>Tira count gihars da mão ao arremessar.</summary>
        public bool TryTake(int count)
        {
            if (count <= 0 || InHand < count) return false;
            InHand -= count;
            StateChanged?.Invoke(State);
            return true;
        }

        /// <summary>Um gihar voltou para a mão.</summary>
        public void ReturnOne()
        {
            InHand = Mathf.Min(maxGihars, InHand + 1);
            StateChanged?.Invoke(State);
        }

        void OnGUI()
        {
            if (!showDebugHud) return;
            GUI.Label(new Rect(12, 10, 400, 24), $"Gihars na mão: {InHand}/{maxGihars}   [{State}]");
        }
    }
}

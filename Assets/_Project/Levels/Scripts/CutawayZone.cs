using System;
using System.Collections.Generic;
using UnityEngine;

namespace Inanna.Levels
{
    /// <summary>
    /// Corte de câmera para fases com andares: enquanto a Inanna está dentro deste volume (no andar de baixo),
    /// os renderers do andar de cima perto dela ficam escondidos, para a câmera isométrica enxergar o chão.
    /// Só some o visual: colisores continuam. Volumes que se sobrepõem contam juntos (só reaparece quando
    /// ninguém mais pede para esconder).
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class CutawayZone : MonoBehaviour
    {
        static readonly Dictionary<Renderer, int> HideRequests = new Dictionary<Renderer, int>();

        [Tooltip("O que some enquanto a Inanna está aqui (andar de cima por perto).")]
        [SerializeField] Renderer[] hiddenWhileInside = Array.Empty<Renderer>();
        [SerializeField] LayerMask playerMask;

        // Último instante em que cada corpo foi visto dentro. Teletransporte (save, morte) não avisa a saída,
        // então quem some do OnTriggerStay por um tempinho conta como "saiu".
        const float ExitTimeout = 0.1f;
        readonly Dictionary<Collider, float> _inside = new Dictionary<Collider, float>();
        readonly List<Collider> _gone = new List<Collider>();
        bool _hiding;

        void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
            playerMask = LayerMask.GetMask("Player");
        }

        void Awake() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerEnter(Collider other) => Track(other);

        void OnTriggerStay(Collider other) => Track(other);

        void OnTriggerExit(Collider other)
        {
            if (!_inside.Remove(other) || _inside.Count > 0) return;
            SetHiding(false);
        }

        void Track(Collider other)
        {
            if ((playerMask.value & (1 << other.gameObject.layer)) == 0) return;
            _inside[other] = Time.fixedTime;
            SetHiding(true);
        }

        void FixedUpdate()
        {
            if (_inside.Count == 0) return;
            _gone.Clear();
            foreach (var pair in _inside)
                if (!pair.Key || !pair.Key.enabled || Time.fixedTime - pair.Value > ExitTimeout) _gone.Add(pair.Key);
            foreach (var c in _gone) _inside.Remove(c);
            if (_inside.Count == 0) SetHiding(false);
        }

        // Desligar o volume não pode deixar o andar de cima sumido para sempre.
        void OnDisable()
        {
            _inside.Clear();
            SetHiding(false);
        }

        void SetHiding(bool hide)
        {
            if (_hiding == hide) return;
            _hiding = hide;
            foreach (var r in hiddenWhileInside)
            {
                if (!r) continue;
                HideRequests.TryGetValue(r, out int count);
                count = Mathf.Max(0, count + (hide ? 1 : -1));
                HideRequests[r] = count;
                r.enabled = count == 0;
            }
        }
    }
}

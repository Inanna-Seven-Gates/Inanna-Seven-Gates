using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Inanna.CameraSystem
{
    /// <summary>
    /// Evita que o cenário tape a Inanna: tudo que estiver na layer "Occluder" entre a câmera e o jogador
    /// deixa de ser desenhado (mas continua projetando sombra, então o chão não "pisca").
    /// Versão simples e barata. Depois fazer shader com dither/fade (tarefa para a arte).
    /// O objeto precisa ter Collider + Renderer e estar na layer Occluder.
    /// </summary>
    public class CameraOcclusionFader : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] LayerMask occluderMask;
        [Tooltip("Espessura do 'raio'. Maior = esconde objetos mais cedo, perto das bordas.")]
        [SerializeField] float radius = 0.8f;
        [SerializeField] float targetHeight = 1f;
        [SerializeField] float checkInterval = 0.05f;

        readonly Dictionary<Renderer, ShadowCastingMode> _hidden = new();
        readonly HashSet<Renderer> _current = new();
        readonly List<Renderer> _toRestore = new();
        readonly RaycastHit[] _hits = new RaycastHit[32];
        float _nextCheck;

        void Update()
        {
            if (!target || Time.time < _nextCheck) return;
            _nextCheck = Time.time + checkInterval;

            _current.Clear();

            Vector3 from = transform.position;
            Vector3 to = target.position + Vector3.up * targetHeight;
            Vector3 dir = to - from;
            float dist = dir.magnitude;

            int n = Physics.SphereCastNonAlloc(from, radius, dir / dist, _hits, dist,
                                                occluderMask, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < n; i++)
            {
                Collider col = _hits[i].collider;
                if (col.transform.IsChildOf(target)) continue;

                if (!col.TryGetComponent(out Renderer r)) r = col.GetComponentInParent<Renderer>();
                if (!r) continue;

                _current.Add(r);
                if (!_hidden.ContainsKey(r))
                {
                    _hidden.Add(r, r.shadowCastingMode);
                    r.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
            }

            _toRestore.Clear();
            foreach (var kv in _hidden)
                if (!_current.Contains(kv.Key)) _toRestore.Add(kv.Key);

            foreach (var r in _toRestore)
            {
                if (r) r.shadowCastingMode = _hidden[r];
                _hidden.Remove(r);
            }
        }

        void OnDisable()
        {
            foreach (var kv in _hidden)
                if (kv.Key) kv.Key.shadowCastingMode = kv.Value;
            _hidden.Clear();
        }
    }
}

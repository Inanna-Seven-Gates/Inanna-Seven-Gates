using System.Collections.Generic;
using UnityEngine;

namespace Inanna.Combat
{
    /// <summary>
    /// Malhas procedurais planas (plano XZ, raio 1, apontando para +Z) usadas como overlays no chão:
    /// círculo de alcance, anel de marcação e setor/leque de ataque. Todas ficam em cache.
    /// Os triângulos seguem o sentido horário visto de cima (face voltada para cima).
    /// </summary>
    public static class OverlayMeshes
    {
        const int DiscSegments = 48;

        static Mesh _disc;
        static readonly Dictionary<int, Mesh> _rings = new();
        static readonly Dictionary<int, Mesh> _sectors = new();

        /// <summary>Disco cheio de raio 1.</summary>
        public static Mesh Disc() => _disc ? _disc : (_disc = Sector(360f, DiscSegments, "OverlayDisc"));

        /// <summary>Anel de raio externo 1 e raio interno <paramref name="inner"/>.</summary>
        public static Mesh Ring(float inner = 0.8f)
        {
            int key = Mathf.RoundToInt(inner * 1000f);
            if (_rings.TryGetValue(key, out var cached) && cached) return cached;

            var v = new Vector3[(DiscSegments + 1) * 2];
            var t = new int[DiscSegments * 6];
            for (int i = 0; i <= DiscSegments; i++)
            {
                Vector3 dir = Dir(i / (float)DiscSegments * Mathf.PI * 2f);
                v[i * 2] = dir;
                v[i * 2 + 1] = dir * inner;
            }
            for (int i = 0; i < DiscSegments; i++)
            {
                int o0 = i * 2, i0 = o0 + 1, o1 = o0 + 2, i1 = o0 + 3, k = i * 6;
                t[k] = o0; t[k + 1] = o1; t[k + 2] = i0;
                t[k + 3] = i0; t[k + 4] = o1; t[k + 5] = i1;
            }
            return _rings[key] = Build($"OverlayRing_{inner:0.##}", v, t);
        }

        /// <summary>Setor (leque) centrado em +Z com abertura total <paramref name="angleDeg"/>.</summary>
        public static Mesh Sector(float angleDeg, int segments = 24) =>
            Sector(angleDeg, segments, $"OverlaySector_{angleDeg:0.#}");

        static Mesh Sector(float angleDeg, int segments, string name)
        {
            angleDeg = Mathf.Clamp(angleDeg, 1f, 360f);
            segments = Mathf.Max(3, segments);

            int key = Mathf.RoundToInt(angleDeg * 10f) * 1000 + segments;
            if (_sectors.TryGetValue(key, out var cached) && cached) return cached;

            float half = angleDeg * 0.5f * Mathf.Deg2Rad;
            var v = new Vector3[segments + 2];
            var t = new int[segments * 3];
            v[0] = Vector3.zero;
            for (int i = 0; i <= segments; i++)
                v[i + 1] = Dir(Mathf.Lerp(-half, half, i / (float)segments));
            for (int i = 0; i < segments; i++)
            {
                t[i * 3] = 0;
                t[i * 3 + 1] = i + 1;
                t[i * 3 + 2] = i + 2;
            }
            return _sectors[key] = Build(name, v, t);
        }

        static Vector3 Dir(float radians) => new(Mathf.Sin(radians), 0f, Mathf.Cos(radians));

        static Mesh Build(string name, Vector3[] v, int[] t)
        {
            var m = new Mesh { name = name, vertices = v, triangles = t };
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }
    }
}
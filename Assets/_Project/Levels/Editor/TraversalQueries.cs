using System.Linq;
using UnityEngine;

namespace Inanna.Levels.EditorTools
{
    /// <summary>Consultas de geometria usadas pelas checagens de percurso.</summary>
    public static class TraversalQueries
    {
        public static float HorizontalDistance(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        /// <summary>Bounds de colisores e renderers do objeto e dos filhos.</summary>
        public static bool TryGetBounds(Transform target, out Bounds bounds)
        {
            var all = target.GetComponentsInChildren<Collider>().Select(c => c.bounds)
                .Concat(target.GetComponentsInChildren<Renderer>().Select(r => r.bounds))
                .ToList();
            bounds = all.FirstOrDefault();
            if (all.Count == 0) return false;
            foreach (var other in all) bounds.Encapsulate(other);
            return true;
        }

        /// <summary>Linha de visão livre até o alvo (ignora o próprio alvo e os filhos dele).</summary>
        public static bool HasLineOfSight(Vector3 from, Vector3 to, Transform target, TraversalSettings settings)
        {
            var direction = to - from;
            var hits = Physics.RaycastAll(from, direction.normalized, direction.magnitude, settings.LineOfSightMask, QueryTriggerInteraction.Ignore);
            return hits.All(hit => hit.transform.IsChildOf(target));
        }

        public static bool ContainsHorizontally(Bounds bounds, Vector3 point, float margin)
        {
            return point.x >= bounds.min.x - margin && point.x <= bounds.max.x + margin
                && point.z >= bounds.min.z - margin && point.z <= bounds.max.z + margin;
        }

        /// <summary>
        /// Menor distância de arremesso até o alvo, de uma célula alcançada e com linha de visão.
        /// Devolve false se nenhuma célula dentro do alcance enxerga o alvo.
        /// </summary>
        public static bool TryFindRangedSpot(TraversalGrid grid, Reachability reach, Transform target, Bounds targetBounds,
            TraversalSettings settings, out Vector3 eye, out float distance)
        {
            float searchRadius = settings.RangedReach + Mathf.Max(targetBounds.extents.x, targetBounds.extents.z);
            var candidates = grid.WalkableCellsAround(targetBounds.center, searchRadius)
                .Where(reach.IsReached)
                .Select(cell =>
                {
                    var cellEye = grid.FeetPosition(cell) + Vector3.up * settings.EyeHeight;
                    return (eye: cellEye, distance: HorizontalDistance(cellEye, targetBounds.ClosestPoint(cellEye)));
                })
                .Where(candidate => candidate.distance <= settings.RangedReach)
                .OrderBy(candidate => candidate.distance);

            foreach (var candidate in candidates)
            {
                if (!HasLineOfSight(candidate.eye, targetBounds.center, target, settings)) continue;
                eye = candidate.eye;
                distance = candidate.distance;
                return true;
            }
            eye = default;
            distance = float.MaxValue;
            return false;
        }
    }
}

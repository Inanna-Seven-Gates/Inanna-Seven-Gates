using System.Collections.Generic;
using System.Linq;
using Inanna.Combat;
using UnityEngine;

namespace Inanna.Levels.EditorTools
{
    /// <summary>
    /// Confere cada <see cref="CombatRoom"/> com as passagens trancadas: a sala tem que fechar de verdade
    /// (não dá para sair) e todo inimigo dela tem que poder ser derrotado lá de dentro, no corpo a corpo ou
    /// com o bumerangue. Se não der, o jogador fica preso. Inimigos = marcadores "I_" do greybox e objetos com
    /// <see cref="Health"/> na layer Enemy.
    /// </summary>
    public static class CombatRoomChecker
    {
        /// <summary>Folga para a grade e a espessura da parede na checagem de "a sala fecha".</summary>
        const float LeakMargin = 1f;

        public static void Check(TraversalGrid grid, IReadOnlyList<Vector3> routePath, TraversalSettings settings, TraversalReport report)
        {
            foreach (var room in Object.FindObjectsByType<CombatRoom>(FindObjectsSortMode.None).OrderBy(r => r.name))
                CheckRoom(grid, routePath, settings, report, room);
            grid.Rebuild();
        }

        static void CheckRoom(TraversalGrid grid, IReadOnlyList<Vector3> routePath, TraversalSettings settings, TraversalReport report, CombatRoom room)
        {
            var gates = room.Gates.ToList();
            var enemies = FindEnemies(room, settings);
            if (gates.Count == 0) report.AddWarning($"'{room.name}' não tem passagens (ILockable): a sala não tranca.");
            if (!room.StartsOnPlayerEnter && enemies.Count == 0)
            {
                report.AddStep($"'{room.name}' começa por evento (ex.: altar) e a onda nasce na hora: confira no playtest.");
                return;
            }
            if (enemies.Count == 0)
            {
                report.AddWarning($"'{room.name}' não tem inimigos marcados: a sala não vai trancar.");
                return;
            }
            if (!TryFindEntryCell(grid, routePath, room, out int entry))
            {
                report.AddWarning($"'{room.name}': não achei chão andável dentro do trigger.");
                return;
            }

            var initialStates = gates.Select(gate => gate.IsLocked).ToList();
            foreach (var gate in gates) gate.SetLockedImmediate(true);
            grid.Rebuild();
            try
            {
                Reachability reach;
                List<string> unreachable;
                while (true)
                {
                    reach = grid.Explore(entry);
                    unreachable = enemies.Where(enemy => !CanDefeat(grid, reach, enemy, settings, out _)).Select(enemy => enemy.name).ToList();
                    if (unreachable.Count == 0 || !SwitchSolver.TryProgress(grid, reach, settings, report)) break;
                    grid.Rebuild();
                }

                var roomBounds = room.RoomBounds;
                Vector3? leak = reach.ReachedCells.Select(grid.FeetPosition)
                    .Where(feet => !TraversalQueries.ContainsHorizontally(roomBounds, feet, LeakMargin))
                    .Select(feet => (Vector3?)feet)
                    .FirstOrDefault();
                bool leaks = leak.HasValue;
                if (leaks)
                {
                    report.AddFailure($"'{room.name}' não fecha: com as passagens trancadas dá para sair por ({leak.Value.x:0.0}, {leak.Value.z:0.0}).");
                    report.FailurePoint ??= leak;
                }

                if (unreachable.Count > 0)
                {
                    report.AddFailure($"'{room.name}' trancada com inimigo que não dá para derrotar lá de dentro ({string.Join(", ", unreachable)}): o jogador fica preso.");
                    report.FailurePoint ??= room.TriggerBounds.center;
                }
                else if (!leaks)
                {
                    int ranged = enemies.Count(enemy => CanDefeat(grid, reach, enemy, settings, out bool onlyRanged) && onlyRanged);
                    string detail = ranged > 0 ? $" ({ranged} só com o bumerangue)" : "";
                    string count = enemies.Count == 1 ? "o inimigo" : $"os {enemies.Count} inimigos";
                    report.AddStep($"Sala trancada '{room.name}': fecha, e dá para derrotar {count} lá de dentro{detail}.");
                }
            }
            finally
            {
                for (int i = 0; i < gates.Count; i++) gates[i].SetLockedImmediate(initialStates[i]);
            }
        }

        static bool CanDefeat(TraversalGrid grid, Reachability reach, Transform enemy, TraversalSettings settings, out bool onlyRanged)
        {
            onlyRanged = false;
            if (!TraversalQueries.TryGetBounds(enemy, out var bounds)) bounds = new Bounds(enemy.position, Vector3.zero);

            float meleeReach = settings.MeleeReach + settings.AgentRadius;
            // Corpo a corpo também precisa de linha de visão: golpe não atravessa parede.
            bool melee = grid.WalkableCellsAround(bounds.center, meleeReach + Mathf.Max(bounds.extents.x, bounds.extents.z))
                .Where(reach.IsReached)
                .Select(cell => grid.FeetPosition(cell) + Vector3.up * settings.EyeHeight)
                .Where(eye => TraversalQueries.HorizontalDistance(eye, bounds.ClosestPoint(eye)) <= meleeReach)
                .Any(eye => TraversalQueries.HasLineOfSight(eye, bounds.center, enemy, settings));
            if (melee) return true;

            onlyRanged = TraversalQueries.TryFindRangedSpot(grid, reach, enemy, bounds, settings, out _, out _);
            return onlyRanged;
        }

        static List<Transform> FindEnemies(CombatRoom room, TraversalSettings settings)
        {
            var roomBounds = room.RoomBounds;
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            var markers = Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
                .Select(renderer => renderer.transform)
                .Where(t => t.name.StartsWith(settings.EnemyMarkerPrefix));
            var living = Object.FindObjectsByType<Health>(FindObjectsSortMode.None)
                .Where(health => health.gameObject.layer == enemyLayer)
                .Select(health => health.transform);
            return markers.Concat(living)
                .Where(t => TraversalQueries.ContainsHorizontally(roomBounds, t.position, 0f))
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Onde a rota entra no trigger: é dali que o jogador fica trancado. Sala fora da rota (opcional):
        /// a célula andável mais perto do centro do trigger.
        /// </summary>
        static bool TryFindEntryCell(TraversalGrid grid, IReadOnlyList<Vector3> routePath, CombatRoom room, out int entry)
        {
            entry = -1;
            var trigger = room.TriggerBounds;
            foreach (var point in routePath)
            {
                if (!TraversalQueries.ContainsHorizontally(trigger, point, 0f)) continue;
                entry = grid.WalkableCellsAround(point, 0.5f).DefaultIfEmpty(-1).First();
                if (entry != -1) return true;
            }

            float radius = Mathf.Max(trigger.extents.x, trigger.extents.z);
            entry = grid.WalkableCellsAround(trigger.center, radius)
                .Where(cell => TraversalQueries.ContainsHorizontally(trigger, grid.FeetPosition(cell), 0f)
                    && Mathf.Abs(grid.FeetPosition(cell).y - (trigger.min.y)) < 1f)
                .OrderBy(cell => (grid.FeetPosition(cell) - trigger.center).sqrMagnitude)
                .DefaultIfEmpty(-1)
                .First();
            return entry != -1;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Inanna.Levels.EditorTools
{
    /// <summary>
    /// Confere se a rota do <see cref="LevelRoute"/> dá para ser percorrida pela Inanna.
    /// Quando o caminho trava, aciona os <see cref="DamageSwitch"/> que o bumerangue alcança (com linha de visão)
    /// e tenta de novo. Depois confere armadilhas (lugares de onde não dá mais para chegar ao fim) e as salas
    /// trancadas (<see cref="CombatRoomChecker"/>). No Play mode, também anda a rota com o CharacterController.
    /// A cena volta ao estado original no fim.
    /// </summary>
    public static class RouteValidator
    {
        public static TraversalReport ValidateActiveScene()
        {
            var route = Object.FindAnyObjectByType<LevelRoute>();
            if (route == null)
            {
                var missing = new TraversalReport(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
                missing.AddFailure("Nenhum LevelRoute na cena. Adicione um e preencha o começo e os pontos da rota.");
                return missing;
            }
            return Validate(route, TraversalSettings.FromScene(), Application.isPlaying);
        }

        public static TraversalReport Validate(LevelRoute route, TraversalSettings settings, bool walkWithController)
        {
            var report = new TraversalReport(route.gameObject.scene.name);
            var snapshot = SceneStateSnapshot.Capture();
            try
            {
                var grid = new TraversalGrid(LevelBounds(settings), settings);
                grid.Rebuild();
                SolveRoute(route, settings, grid, report);
                if (!report.Passed) return report;

                if (walkWithController) WalkWithController(report);

                // As salas trancadas são conferidas no estado em que o jogador chega (gatilhos ainda não acionados).
                snapshot.Restore();
                grid.Rebuild();
                CombatRoomChecker.Check(grid, report.Path, settings, report);
            }
            finally
            {
                snapshot.Restore();
            }
            return report;
        }

        static void SolveRoute(LevelRoute route, TraversalSettings settings, TraversalGrid grid, TraversalReport report)
        {
            if (route.StartPoint == null)
            {
                report.AddFailure("O LevelRoute não tem começo (StartPoint).");
                return;
            }

            if (!TryFindArrivalCell(grid, null, route.StartPoint, 1f, settings, out int current))
            {
                report.AddFailure($"O começo '{route.StartPoint.name}' não está em chão andável.");
                report.FailurePoint = route.StartPoint.position;
                return;
            }
            int startCell = current;
            report.Path.Add(grid.FeetPosition(current));
            report.AddStep($"Começo: {route.StartPoint.name}");

            foreach (var waypoint in route.Waypoints)
            {
                if (waypoint == null)
                {
                    report.AddWarning("O LevelRoute tem um ponto vazio na lista.");
                    continue;
                }

                while (true)
                {
                    var reach = grid.Explore(current);
                    if (TryFindArrivalCell(grid, reach, waypoint, route.ArrivalRadius, settings, out int arrival))
                    {
                        var leg = reach.PathTo(arrival);
                        leg.RemoveAt(0);
                        report.Path.AddRange(leg);
                        report.AddStep($"OK  {waypoint.name}");
                        current = arrival;
                        if (SwitchSolver.CollectKeysNear(grid.FeetPosition(arrival), route.ArrivalRadius + 0.5f, report)) grid.Rebuild();
                        break;
                    }

                    if (SwitchSolver.TryProgress(grid, reach, settings, report))
                    {
                        grid.Rebuild();
                        continue;
                    }

                    report.AddFailure($"Não dá para chegar em '{waypoint.name}' (nenhuma alavanca alcançável destrava o caminho).");
                    report.FailurePoint = ClosestReachedPoint(grid, reach, waypoint.position);
                    return;
                }
            }

            ReportOneWayDrops(report, settings);
            CheckForTraps(grid, startCell, current, report);
            float length = report.PathLength;
            report.AddStep($"Rota: {length:0} m, cerca de {length / settings.MoveSpeed:0} s só andando a {settings.MoveSpeed:0.#} m/s (sem lutas).");
        }

        static bool TryFindArrivalCell(TraversalGrid grid, Reachability reach, Transform target, float radius, TraversalSettings settings, out int arrival)
        {
            if (!TraversalQueries.TryGetBounds(target, out var bounds)) bounds = new Bounds(target.position, Vector3.zero);
            float minFeetY = bounds.min.y - settings.AgentHeight * 0.5f;
            float maxFeetY = bounds.max.y + 0.1f;
            float searchRadius = radius + Mathf.Max(bounds.extents.x, bounds.extents.z);

            arrival = -1;
            float best = float.MaxValue;
            foreach (int cell in grid.WalkableCellsAround(bounds.center, searchRadius))
            {
                if (reach != null && !reach.IsReached(cell)) continue;
                var feet = grid.FeetPosition(cell);
                if (feet.y < minFeetY || feet.y > maxFeetY) continue;

                float distance = TraversalQueries.HorizontalDistance(feet, bounds.ClosestPoint(feet));
                if (distance > radius || distance >= best) continue;
                best = distance;
                arrival = cell;
            }
            return arrival != -1;
        }

        static void ReportOneWayDrops(TraversalReport report, TraversalSettings settings)
        {
            for (int i = 1; i < report.Path.Count; i++)
            {
                float drop = report.Path[i - 1].y - report.Path[i].y;
                if (drop <= settings.StepHeight) continue;
                var at = report.Path[i];
                report.AddStep($"Queda sem volta de {drop:0.0} m em ({at.x:0.0}, {at.z:0.0}). Confira se é intencional.");
            }
        }

        /// <summary>
        /// De todo lugar que o jogador alcança, ainda tem que dar para chegar ao fim. Pega quedas sem volta
        /// para um beco, buracos e cantos onde o corpo entra e não sai. Roda no estado final (gatilhos acionados,
        /// que nunca voltam atrás), que é o maior conjunto de lugares alcançáveis.
        /// </summary>
        static void CheckForTraps(TraversalGrid grid, int startCell, int goalCell, TraversalReport report)
        {
            var reachable = grid.Explore(startCell);
            var canFinish = grid.CellsThatCanReach(new[] { goalCell });

            int trappedCells = 0;
            var startFeet = grid.FeetPosition(startCell);
            Vector3 nearestTrap = default;
            float nearestDistance = float.MaxValue;
            foreach (int cell in reachable.ReachedCells)
            {
                if (canFinish[cell]) continue;
                trappedCells++;
                var feet = grid.FeetPosition(cell);
                float distance = (feet - startFeet).sqrMagnitude;
                if (distance >= nearestDistance) continue;
                nearestDistance = distance;
                nearestTrap = feet;
            }

            if (trappedCells == 0)
            {
                report.AddStep("Sem armadilhas: de todo lugar alcançável ainda dá para chegar ao fim.");
                return;
            }
            report.AddFailure($"Jogador pode ficar preso: {trappedCells * grid.CellArea:0.0} m² de onde não dá mais para chegar ao fim (o mais perto do começo em ({nearestTrap.x:0.0}, {nearestTrap.y:0.0}, {nearestTrap.z:0.0})).");
            report.FailurePoint ??= nearestTrap;
        }

        static void WalkWithController(TraversalReport report)
        {
            var controller = TraversalSettings.FindPlayerController();
            if (controller == null)
            {
                report.AddWarning("Sem CharacterController na layer Player: pulei a caminhada real.");
                return;
            }
            CharacterControllerWalker.Walk(controller, report.Path, report);
        }

        static Vector3 ClosestReachedPoint(TraversalGrid grid, Reachability reach, Vector3 target)
        {
            var best = target;
            float bestDistance = float.MaxValue;
            foreach (int cell in reach.ReachedCells)
            {
                var feet = grid.FeetPosition(cell);
                float distance = (feet - target).sqrMagnitude;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = feet;
            }
            return best;
        }

        static Bounds LevelBounds(TraversalSettings settings)
        {
            Bounds? total = null;
            foreach (var collider in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                if (!collider.enabled || (settings.GroundMask.value & (1 << collider.gameObject.layer)) == 0) continue;
                if (total.HasValue)
                {
                    var merged = total.Value;
                    merged.Encapsulate(collider.bounds);
                    total = merged;
                }
                else total = collider.bounds;
            }
            var bounds = total ?? new Bounds(Vector3.zero, Vector3.one);
            bounds.Expand(2f);
            return bounds;
        }

        /// <summary>Guarda o estado de interruptores, alvos e passagens para devolver a cena como estava.</summary>
        sealed class SceneStateSnapshot
        {
            readonly List<(ISwitch item, bool activated)> _switches = new List<(ISwitch, bool)>();
            readonly List<(IActivatable item, bool activated)> _activatables = new List<(IActivatable, bool)>();
            readonly List<(ILockable item, bool locked)> _lockables = new List<(ILockable, bool)>();

            public static SceneStateSnapshot Capture()
            {
                var snapshot = new SceneStateSnapshot();
                foreach (var behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                {
                    if (behaviour is ISwitch item) snapshot._switches.Add((item, item.IsActivated));
                    if (behaviour is IActivatable activatable) snapshot._activatables.Add((activatable, activatable.IsActivated));
                    if (behaviour is ILockable lockable) snapshot._lockables.Add((lockable, lockable.IsLocked));
                }
                return snapshot;
            }

            public void Restore()
            {
                foreach (var (item, activated) in _switches) item.SetActivatedImmediate(activated);
                foreach (var (item, activated) in _activatables) item.SetActivatedImmediate(activated);
                foreach (var gate in Object.FindObjectsByType<SwitchGate>(FindObjectsSortMode.None)) gate.EvaluateImmediate();
                foreach (var (item, locked) in _lockables) item.SetLockedImmediate(locked);
                Physics.SyncTransforms();
            }
        }
    }
}

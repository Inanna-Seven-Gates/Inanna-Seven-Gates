using System.Collections.Generic;

namespace Inanna.Levels.EditorTools
{
    /// <summary>Aciona, como o jogador faria, um gatilho que o bumerangue alcança da área já alcançada.</summary>
    public static class SwitchSolver
    {
        public static bool TryActivateReachableSwitch(TraversalGrid grid, Reachability reach, IEnumerable<DamageSwitch> switches,
            TraversalSettings settings, TraversalReport report)
        {
            foreach (var damageSwitch in switches)
            {
                if (damageSwitch.IsActivated) continue;
                if (!TraversalQueries.TryGetBounds(damageSwitch.transform, out var bounds)) continue;
                if (!TraversalQueries.TryFindRangedSpot(grid, reach, damageSwitch.transform, bounds, settings, out var eye, out float distance)) continue;

                report.AddStep($"Alavanca '{damageSwitch.name}' acionada a {distance:0.0} m, de ({eye.x:0.0}, {eye.z:0.0}).");
                if (distance <= settings.MeleeReach + settings.AgentRadius)
                    report.AddWarning($"'{damageSwitch.name}' é alcançável no corpo a corpo ({distance:0.0} m): não obriga o uso do bumerangue.");
                float comfortable = settings.RangedReach * settings.ComfortableReachRatio;
                if (distance > comfortable)
                    report.AddWarning($"'{damageSwitch.name}' exige arremesso de {distance:0.0} m ({distance / settings.RangedReach:P0} do alcance). No caminho obrigatório, o ideal é até {comfortable:0.0} m.");

                damageSwitch.SetActivatedImmediate(true);
                return true;
            }
            return false;
        }
    }
}

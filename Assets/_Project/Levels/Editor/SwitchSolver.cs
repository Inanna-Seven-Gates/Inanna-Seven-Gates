using System.Linq;
using UnityEngine;

namespace Inanna.Levels.EditorTools
{
    /// <summary>
    /// Faz o que o jogador faria para destravar o caminho: pega chaves alcançadas, aciona alavancas que o
    /// bumerangue alcança e reavalia as portas (<see cref="SwitchGate"/>).
    /// </summary>
    public static class SwitchSolver
    {
        const float KeyPickupRadius = 1f;

        /// <summary>Tenta destravar algo a partir da área já alcançada. True se mudou alguma coisa na cena.</summary>
        public static bool TryProgress(TraversalGrid grid, Reachability reach, TraversalSettings settings, TraversalReport report)
        {
            bool changed = CollectReachableKeys(grid, reach, report) || TryActivateRangedSwitch(grid, reach, settings, report);
            if (changed) EvaluateSwitchGates(report);
            return changed;
        }

        /// <summary>Pega as chaves perto de um ponto (ex.: quando a rota chega na chave). True se pegou alguma.</summary>
        public static bool CollectKeysNear(Vector3 feet, float radius, TraversalReport report)
        {
            bool collected = false;
            foreach (var key in Object.FindObjectsByType<KeyPickup>(FindObjectsSortMode.None))
            {
                if (key.IsActivated || TraversalQueries.HorizontalDistance(key.transform.position, feet) > radius) continue;
                key.SetActivatedImmediate(true);
                report.AddStep($"Chave '{key.KeyName}' pega.");
                collected = true;
            }
            if (collected) EvaluateSwitchGates(report);
            return collected;
        }

        public static void EvaluateSwitchGates(TraversalReport report)
        {
            foreach (var gate in Object.FindObjectsByType<SwitchGate>(FindObjectsSortMode.None).OrderBy(g => g.name))
            {
                bool wasOpen = gate.IsOpen;
                gate.EvaluateImmediate();
                if (!wasOpen && gate.IsOpen) report.AddStep($"Porta '{gate.name}' abriu.");
            }
        }

        static bool CollectReachableKeys(TraversalGrid grid, Reachability reach, TraversalReport report)
        {
            bool collected = false;
            foreach (var key in Object.FindObjectsByType<KeyPickup>(FindObjectsSortMode.None))
            {
                if (key.IsActivated) continue;
                if (!grid.WalkableCellsAround(key.transform.position, KeyPickupRadius).Any(reach.IsReached)) continue;
                key.SetActivatedImmediate(true);
                report.AddStep($"Chave '{key.KeyName}' pega (alcançável).");
                collected = true;
            }
            return collected;
        }

        static bool TryActivateRangedSwitch(TraversalGrid grid, Reachability reach, TraversalSettings settings, TraversalReport report)
        {
            foreach (var damageSwitch in Object.FindObjectsByType<DamageSwitch>(FindObjectsSortMode.None).OrderBy(s => s.name))
            {
                if (damageSwitch.IsActivated) continue;
                if (!TraversalQueries.TryGetBounds(damageSwitch.transform, out var bounds)) continue;
                if (!TraversalQueries.TryFindRangedSpot(grid, reach, damageSwitch.transform, bounds, settings, out var eye, out float distance)) continue;

                report.AddStep($"Alavanca '{damageSwitch.name}' acionada a {distance:0.0} m, de ({eye.x:0.0}, {eye.z:0.0}).");
                if (damageSwitch.RangedOnly)
                {
                    if (distance <= settings.MeleeReach + settings.AgentRadius)
                        report.AddWarning($"'{damageSwitch.name}' é alcançável no corpo a corpo ({distance:0.0} m): não obriga o uso do bumerangue.");
                    float comfortable = settings.RangedReach * settings.ComfortableReachRatio;
                    if (distance > comfortable)
                        report.AddWarning($"'{damageSwitch.name}' exige arremesso de {distance:0.0} m ({distance / settings.RangedReach:P0} do alcance). No caminho obrigatório, o ideal é até {comfortable:0.0} m.");
                }
                damageSwitch.SetActivatedImmediate(true);
                return true;
            }
            return false;
        }
    }
}

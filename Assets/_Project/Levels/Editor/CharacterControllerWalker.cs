using System.Collections.Generic;
using UnityEngine;

namespace Inanna.Levels.EditorTools
{
    /// <summary>
    /// Anda a rota com o CharacterController da Inanna (mesma colisão, degrau e queda do jogo).
    /// Confirma de verdade o que a grade calculou. Só funciona no Play mode e devolve a Inanna para onde estava.
    /// </summary>
    public static class CharacterControllerWalker
    {
        const float StepLength = 0.2f;
        const float FallPerStep = 0.25f;
        const float ArrivalTolerance = 0.15f;
        const int PathSampling = 4;
        const int MaxStepsPerTarget = 400;
        const int MaxStepsWithoutProgress = 30;

        public static bool Walk(CharacterController controller, IReadOnlyList<Vector3> path, TraversalReport report)
        {
            if (path.Count == 0) return false;

            var transform = controller.transform;
            var originalPosition = transform.position;
            var originalRotation = transform.rotation;
            try
            {
                Physics.SyncTransforms();
                PlaceFeetAt(controller, path[0]);

                for (int i = PathSampling; ; i += PathSampling)
                {
                    var target = path[Mathf.Min(i, path.Count - 1)];
                    if (!WalkTo(controller, target))
                    {
                        var stuckAt = Feet(controller);
                        report.AddFailure($"O CharacterController travou em ({stuckAt.x:0.0}, {stuckAt.y:0.0}, {stuckAt.z:0.0}) indo para ({target.x:0.0}, {target.z:0.0}).");
                        report.FailurePoint = stuckAt;
                        return false;
                    }
                    if (i >= path.Count - 1) break;
                }

                report.AddStep("Caminhada real: a Inanna andou a rota inteira com o CharacterController.");
                return true;
            }
            finally
            {
                controller.enabled = false;
                transform.SetPositionAndRotation(originalPosition, originalRotation);
                controller.enabled = true;
                Physics.SyncTransforms();
            }
        }

        static bool WalkTo(CharacterController controller, Vector3 target)
        {
            float bestDistance = float.MaxValue;
            int stepsWithoutProgress = 0;

            for (int step = 0; step < MaxStepsPerTarget; step++)
            {
                var delta = target - Feet(controller);
                delta.y = 0f;
                float distance = delta.magnitude;
                if (distance <= ArrivalTolerance) return true;

                controller.Move(delta / distance * Mathf.Min(StepLength, distance) + Vector3.down * FallPerStep);

                if (distance < bestDistance - 0.05f)
                {
                    bestDistance = distance;
                    stepsWithoutProgress = 0;
                }
                else if (++stepsWithoutProgress > MaxStepsWithoutProgress)
                {
                    return false;
                }
            }
            return false;
        }

        static Vector3 Feet(CharacterController controller)
        {
            var center = controller.transform.TransformPoint(controller.center);
            return center + Vector3.down * (controller.height * controller.transform.lossyScale.y * 0.5f);
        }

        static void PlaceFeetAt(CharacterController controller, Vector3 feet)
        {
            var transform = controller.transform;
            var centerOffset = transform.TransformPoint(controller.center) - transform.position;
            float halfHeight = controller.height * transform.lossyScale.y * 0.5f;

            controller.enabled = false;
            transform.position = feet - centerOffset + Vector3.up * (halfHeight + controller.skinWidth);
            controller.enabled = true;
            Physics.SyncTransforms();
        }
    }
}

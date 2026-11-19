using UnityEditor;
using UnityEngine;

namespace Inanna.Levels.EditorTools
{
    /// <summary>
    /// Menu Inanna > Validar percurso da fase. Rode antes de entregar uma fase.
    /// No Play mode, também anda a rota com o CharacterController. O caminho aparece na Scene view por 30 s
    /// (verde = rota, vermelho = onde travou).
    /// </summary>
    static class LevelTraversalMenu
    {
        const float DrawSeconds = 30f;

        [MenuItem("Inanna/Validar percurso da fase")]
        static void ValidateActiveScene()
        {
            var report = RouteValidator.ValidateActiveScene();
            Draw(report);
            if (report.Passed) Debug.Log(report.ToString());
            else Debug.LogError(report.ToString());
        }

        static void Draw(TraversalReport report)
        {
            var lift = Vector3.up * 0.15f;
            for (int i = 1; i < report.Path.Count; i++)
                Debug.DrawLine(report.Path[i - 1] + lift, report.Path[i] + lift, Color.green, DrawSeconds);

            if (report.FailurePoint is Vector3 failure)
                Debug.DrawRay(failure, Vector3.up * 4f, Color.red, DrawSeconds);
        }
    }
}

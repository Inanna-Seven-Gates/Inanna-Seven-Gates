using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Inanna.Levels.EditorTools
{
    /// <summary>Resultado do validador de percurso: passos, avisos, falhas e o caminho encontrado.</summary>
    public sealed class TraversalReport
    {
        readonly List<string> _steps = new List<string>();
        readonly List<string> _warnings = new List<string>();
        readonly List<string> _failures = new List<string>();

        public TraversalReport(string levelName) => LevelName = levelName;

        public string LevelName { get; }
        public bool Passed => _failures.Count == 0;
        public List<Vector3> Path { get; } = new List<Vector3>();
        public Vector3? FailurePoint { get; set; }

        public float PathLength
        {
            get
            {
                float length = 0f;
                for (int i = 1; i < Path.Count; i++) length += Vector3.Distance(Path[i - 1], Path[i]);
                return length;
            }
        }

        public void AddStep(string message) => _steps.Add(message);
        public void AddWarning(string message) => _warnings.Add(message);
        public void AddFailure(string message) => _failures.Add(message);

        public override string ToString()
        {
            var text = new StringBuilder();
            text.AppendLine($"=== Percurso de {LevelName}: {(Passed ? "PERCORRÍVEL" : "NÃO PERCORRÍVEL")} ===");
            foreach (var step in _steps) text.AppendLine("  " + step);
            if (_warnings.Count > 0)
            {
                text.AppendLine("Avisos:");
                foreach (var warning in _warnings) text.AppendLine("  - " + warning);
            }
            if (_failures.Count > 0)
            {
                text.AppendLine("Falhas:");
                foreach (var failure in _failures) text.AppendLine("  - " + failure);
            }
            return text.ToString();
        }
    }
}

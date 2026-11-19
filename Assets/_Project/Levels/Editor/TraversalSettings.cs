using UnityEditor;
using UnityEngine;

namespace Inanna.Levels.EditorTools
{
    /// <summary>
    /// Parâmetros do validador de percurso. O corpo (raio, altura, degrau, velocidade) vem da Inanna na cena.
    /// Os alcances vêm do GDD (Itens/Weapons.md).
    /// </summary>
    public sealed class TraversalSettings
    {
        public float CellSize = 0.25f;
        public float AgentRadius = 0.5f;
        public float AgentHeight = 2f;
        public float StepHeight = 0.3f;
        /// <summary>Maior queda aceita. Quedas maiores que o degrau são sem volta.</summary>
        public float MaxDropHeight = 3.5f;
        public float MoveSpeed = 6f;
        /// <summary>Alcance do bumerangue (Gihar).</summary>
        public float RangedReach = 6f;
        /// <summary>Maior alcance corpo a corpo (Cetro, 1,7).</summary>
        public float MeleeReach = 1.7f;
        /// <summary>
        /// Folga para o que é obrigatório no caminho: exigir até 70% do alcance medido
        /// (skill level-design: "size required gaps at about 0.7 of measured reach").
        /// </summary>
        public float ComfortableReachRatio = 0.7f;
        /// <summary>Prefixo dos marcadores de inimigo do greybox (legenda: I = inimigo).</summary>
        public string EnemyMarkerPrefix = "I_";

        public LayerMask GroundMask;
        public LayerMask ObstacleMask;
        public LayerMask LineOfSightMask;

        public float EyeHeight => AgentHeight * 0.5f;

        public static TraversalSettings FromScene()
        {
            var settings = new TraversalSettings
            {
                GroundMask = LayerMask.GetMask("Ground"),
                // Inimigos não contam como obstáculo: o validador assume que eles foram derrotados.
                ObstacleMask = ~LayerMask.GetMask("Player", "Enemy"),
                LineOfSightMask = ~LayerMask.GetMask("Player", "Enemy", "Ignore Raycast"),
            };

            var controller = FindPlayerController();
            if (controller == null) return settings;

            var scale = controller.transform.lossyScale;
            settings.AgentRadius = controller.radius * Mathf.Max(scale.x, scale.z) + controller.skinWidth;
            settings.AgentHeight = controller.height * scale.y;
            settings.StepHeight = controller.stepOffset;

            var motor = controller.GetComponent<Inanna.Player.PlayerMotor>();
            if (motor != null)
            {
                var speed = new SerializedObject(motor).FindProperty("moveSpeed");
                if (speed != null) settings.MoveSpeed = speed.floatValue;
            }
            return settings;
        }

        public static CharacterController FindPlayerController()
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            foreach (var controller in Object.FindObjectsByType<CharacterController>(FindObjectsSortMode.None))
                if (controller.gameObject.layer == playerLayer)
                    return controller;
            return null;
        }
    }
}

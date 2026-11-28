using System;
using System.Collections.Generic;
using UnityEngine;

namespace Inanna.Levels
{
    /// <summary>
    /// Rota que o jogador precisa conseguir percorrer, em ordem (começo, salvamentos, baús, portão...).
    /// Não faz nada no jogo: é a entrada do validador de percurso (menu Inanna > Validar percurso da fase).
    /// </summary>
    public class LevelRoute : MonoBehaviour
    {
        [SerializeField] Transform startPoint;
        [Tooltip("Pontos que o jogador precisa alcançar, na ordem da fase.")]
        [SerializeField] Transform[] waypoints = Array.Empty<Transform>();
        [Tooltip("Distância horizontal que conta como 'chegou' (dá para interagir com baú, portão etc.).")]
        [SerializeField, Min(0.1f)] float arrivalRadius = 1.5f;

        public Transform StartPoint => startPoint;
        public IReadOnlyList<Transform> Waypoints => waypoints;
        public float ArrivalRadius => arrivalRadius;
    }
}

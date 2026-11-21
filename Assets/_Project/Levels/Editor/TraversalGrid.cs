using System.Collections.Generic;
using UnityEngine;

namespace Inanna.Levels.EditorTools
{
    /// <summary>
    /// Grade 2,5D do chão da fase. Cada célula guarda a altura do chão mais alto (layer Ground) e se o corpo
    /// da Inanna cabe ali. Serve para fases sem andares sobrepostos (túnel embaixo de passarela).
    /// </summary>
    public sealed class TraversalGrid
    {
        static readonly Vector2Int[] Neighbours =
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1),
        };

        readonly TraversalSettings _settings;
        readonly Vector3 _origin;
        readonly int _width;
        readonly int _depth;
        readonly float _probeTop;
        readonly float _probeLength;
        readonly float[] _floorHeight;
        readonly bool[] _walkable;

        public TraversalGrid(Bounds area, TraversalSettings settings)
        {
            _settings = settings;
            _origin = area.min;
            _width = Mathf.CeilToInt(area.size.x / settings.CellSize);
            _depth = Mathf.CeilToInt(area.size.z / settings.CellSize);
            _probeTop = area.max.y + 1f;
            _probeLength = area.size.y + 2f;
            _floorHeight = new float[_width * _depth];
            _walkable = new bool[_width * _depth];
        }

        public int CellCount => _walkable.Length;
        public float CellArea => _settings.CellSize * _settings.CellSize;

        /// <summary>Lê o chão e os obstáculos de novo. Chame depois de mudar a cena (ponte abaixada, porta aberta).</summary>
        public void Rebuild()
        {
            Physics.SyncTransforms();
            for (int z = 0; z < _depth; z++)
            for (int x = 0; x < _width; x++)
            {
                int cell = Index(x, z);
                var probe = new Vector3(_origin.x + (x + 0.5f) * _settings.CellSize, _probeTop, _origin.z + (z + 0.5f) * _settings.CellSize);
                if (Physics.Raycast(probe, Vector3.down, out var hit, _probeLength, _settings.GroundMask, QueryTriggerInteraction.Ignore))
                {
                    _floorHeight[cell] = hit.point.y;
                    _walkable[cell] = BodyFits(hit.point);
                }
                else
                {
                    _floorHeight[cell] = float.NaN;
                    _walkable[cell] = false;
                }
            }
        }

        public Vector3 FeetPosition(int cell)
        {
            int x = cell % _width;
            int z = cell / _width;
            return new Vector3(_origin.x + (x + 0.5f) * _settings.CellSize, _floorHeight[cell], _origin.z + (z + 0.5f) * _settings.CellSize);
        }

        /// <summary>Células andáveis num raio horizontal em volta de um ponto.</summary>
        public IEnumerable<int> WalkableCellsAround(Vector3 center, float radius)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt((center.x - radius - _origin.x) / _settings.CellSize));
            int maxX = Mathf.Min(_width - 1, Mathf.FloorToInt((center.x + radius - _origin.x) / _settings.CellSize));
            int minZ = Mathf.Max(0, Mathf.FloorToInt((center.z - radius - _origin.z) / _settings.CellSize));
            int maxZ = Mathf.Min(_depth - 1, Mathf.FloorToInt((center.z + radius - _origin.z) / _settings.CellSize));
            float radiusSqr = radius * radius;

            for (int z = minZ; z <= maxZ; z++)
            for (int x = minX; x <= maxX; x++)
            {
                int cell = Index(x, z);
                if (!_walkable[cell]) continue;
                var feet = FeetPosition(cell);
                float dx = feet.x - center.x, dz = feet.z - center.z;
                if (dx * dx + dz * dz <= radiusSqr) yield return cell;
            }
        }

        /// <summary>Busca em largura a partir de uma célula, respeitando degrau, queda máxima e beiradas.</summary>
        public Reachability Explore(int startCell)
        {
            var parent = new int[_walkable.Length];
            for (int i = 0; i < parent.Length; i++) parent[i] = -1;

            var queue = new Queue<int>();
            var successors = new List<int>(8);
            parent[startCell] = startCell;
            queue.Enqueue(startCell);

            while (queue.Count > 0)
            {
                int cell = queue.Dequeue();
                CollectSuccessors(cell, successors);
                foreach (int next in successors)
                {
                    if (parent[next] != -1) continue;
                    parent[next] = cell;
                    queue.Enqueue(next);
                }
            }
            return new Reachability(this, parent);
        }

        /// <summary>
        /// Busca de trás para frente: marca toda célula de onde dá para chegar em algum dos alvos.
        /// Uma célula alcançável a partir do começo e fora deste conjunto é um lugar onde o jogador fica preso.
        /// </summary>
        public bool[] CellsThatCanReach(IEnumerable<int> goals)
        {
            var predecessors = BuildPredecessors(out var firstPredecessor);
            var canReach = new bool[_walkable.Length];
            var queue = new Queue<int>();
            foreach (int goal in goals)
            {
                if (canReach[goal]) continue;
                canReach[goal] = true;
                queue.Enqueue(goal);
            }

            while (queue.Count > 0)
            {
                int cell = queue.Dequeue();
                for (int i = firstPredecessor[cell]; i < firstPredecessor[cell + 1]; i++)
                {
                    int previous = predecessors[i];
                    if (canReach[previous]) continue;
                    canReach[previous] = true;
                    queue.Enqueue(previous);
                }
            }
            return canReach;
        }

        /// <summary>Para onde dá para ir a partir de uma célula: andando, subindo degrau, descendo ou caindo de beirada.</summary>
        void CollectSuccessors(int cell, List<int> result)
        {
            result.Clear();
            if (!_walkable[cell]) return;
            int x = cell % _width;
            int z = cell / _width;

            foreach (var offset in Neighbours)
            {
                int nx = x + offset.x, nz = z + offset.y;
                if (!InBounds(nx, nz)) continue;
                int next = Index(nx, nz);
                bool diagonal = offset.x != 0 && offset.y != 0;

                if (CanMove(cell, next))
                {
                    // Diagonal só se os dois vizinhos retos também passarem (não corta quina de parede).
                    if (diagonal && (!CanMove(cell, Index(nx, z)) || !CanMove(cell, Index(x, nz)))) continue;
                    result.Add(next);
                }
                else if (!diagonal && TryDropOffLedge(cell, x, z, offset, out int landing))
                {
                    result.Add(landing);
                }
            }
        }

        /// <summary>Arestas invertidas em formato compacto: os antecessores de c ficam em [first[c], first[c+1]).</summary>
        int[] BuildPredecessors(out int[] first)
        {
            var from = new List<int>();
            var to = new List<int>();
            var successors = new List<int>(8);
            var count = new int[_walkable.Length + 1];

            for (int cell = 0; cell < _walkable.Length; cell++)
            {
                CollectSuccessors(cell, successors);
                foreach (int next in successors)
                {
                    from.Add(cell);
                    to.Add(next);
                    count[next]++;
                }
            }

            first = new int[_walkable.Length + 1];
            for (int cell = 0; cell < _walkable.Length; cell++) first[cell + 1] = first[cell] + count[cell];

            var fill = (int[])first.Clone();
            var predecessors = new int[from.Count];
            for (int i = 0; i < from.Count; i++) predecessors[fill[to[i]]++] = from[i];
            return predecessors;
        }

        bool CanMove(int from, int to)
        {
            if (!_walkable[to]) return false;
            float rise = _floorHeight[to] - _floorHeight[from];
            return rise <= _settings.StepHeight && -rise <= _settings.MaxDropHeight;
        }

        /// <summary>
        /// Andar para fora de uma beirada: o corpo passa no ar (na altura de onde saiu) e pousa na primeira
        /// célula andável lá embaixo, até um diâmetro de distância na mesma direção.
        /// </summary>
        bool TryDropOffLedge(int from, int x, int z, Vector2Int direction, out int landing)
        {
            landing = -1;
            int nx = x + direction.x, nz = z + direction.y;
            if (!InBounds(nx, nz)) return false;

            float fromHeight = _floorHeight[from];
            float lowerHeight = _floorHeight[Index(nx, nz)];
            float drop = fromHeight - lowerHeight;
            if (float.IsNaN(lowerHeight) || drop <= _settings.StepHeight || drop > _settings.MaxDropHeight) return false;

            var airborne = FeetPosition(Index(nx, nz));
            airborne.y = fromHeight;
            if (!BodyFits(airborne)) return false;

            int maxCells = Mathf.CeilToInt(_settings.AgentRadius * 2f / _settings.CellSize) + 1;
            for (int k = 1; k <= maxCells; k++)
            {
                int cx = x + direction.x * k, cz = z + direction.y * k;
                if (!InBounds(cx, cz)) return false;
                int candidate = Index(cx, cz);
                if (_walkable[candidate] && Mathf.Abs(_floorHeight[candidate] - lowerHeight) <= _settings.StepHeight)
                {
                    landing = candidate;
                    return true;
                }
            }
            return false;
        }

        static readonly Collider[] EnemyLayerHits = new Collider[16];

        bool BodyFits(Vector3 feet)
        {
            float radius = _settings.AgentRadius;
            // A esfera de baixo começa acima do degrau: obstáculos menores que ele dão para subir.
            var bottom = feet + Vector3.up * (_settings.StepHeight + radius);
            var top = feet + Vector3.up * Mathf.Max(_settings.AgentHeight - radius, _settings.StepHeight + radius);
            if (Physics.CheckCapsule(bottom, top, radius, _settings.ObstacleMask, QueryTriggerInteraction.Ignore)) return false;

            // Na layer Enemy, inimigo (com Health) não bloqueia: o validador assume que foi derrotado.
            // O resto dessa layer (alavanca, parede falsa) é sólido.
            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, EnemyLayerHits, _settings.EnemyLayerMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
                if (!EnemyLayerHits[i].GetComponentInParent<Inanna.Combat.Health>()) return false;
            return true;
        }

        bool InBounds(int x, int z) => x >= 0 && x < _width && z >= 0 && z < _depth;

        int Index(int x, int z) => z * _width + x;
    }

    /// <summary>Resultado de uma exploração: o que foi alcançado e o caminho até cada célula.</summary>
    public sealed class Reachability
    {
        readonly TraversalGrid _grid;
        readonly int[] _parent;

        public Reachability(TraversalGrid grid, int[] parent)
        {
            _grid = grid;
            _parent = parent;
        }

        public bool IsReached(int cell) => _parent[cell] != -1;

        public IEnumerable<int> ReachedCells
        {
            get
            {
                for (int cell = 0; cell < _parent.Length; cell++)
                    if (_parent[cell] != -1) yield return cell;
            }
        }

        /// <summary>Pontos dos pés do começo até a célula (inclusive).</summary>
        public List<Vector3> PathTo(int cell)
        {
            var path = new List<Vector3>();
            if (!IsReached(cell)) return path;
            while (true)
            {
                path.Add(_grid.FeetPosition(cell));
                if (_parent[cell] == cell) break;
                cell = _parent[cell];
            }
            path.Reverse();
            return path;
        }
    }
}

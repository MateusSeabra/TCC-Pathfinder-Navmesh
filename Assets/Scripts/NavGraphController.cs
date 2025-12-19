using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;

public class NavNode
{
    public int PolygonIndex;
    public Vector3 Center;
    public List<int> Neighbours;
    public Vector3[] Vertices;

    public float PheromoneBias { get; set; }
    public List<int> VisitasProximas { get; set; }
    public bool IsPath { get; set; }

    public NavNode(int index, Vector3 center, Vector3[] vertices)
    {
        PolygonIndex = index;
        Center = center;
        Vertices = vertices;
        Neighbours = new List<int>();

        PheromoneBias = 1.0f;
        VisitasProximas = new List<int>();
        IsPath = false;
    }
}

public class NavGraphController : MonoBehaviour
{
    public static NavGraphController Instance;
    public Dictionary<int, NavNode> Graph;

    private struct QuantizedVector3
    {
        public readonly long x, y, z;
        private const float Precision = 1000f; 

        public QuantizedVector3(Vector3 v)
        {
            x = Mathf.RoundToInt(v.x * Precision);
            y = Mathf.RoundToInt(v.y * Precision);
            z = Mathf.RoundToInt(v.z * Precision);
        }

        public override bool Equals(object obj) => obj is QuantizedVector3 other && x == other.x && y == other.y && z == other.z;
        public override int GetHashCode() => (x, y, z).GetHashCode();
    }

    private struct SpatialEdgeKey
    {
        public readonly QuantizedVector3 V1;
        public readonly QuantizedVector3 V2;

        public SpatialEdgeKey(Vector3 v1, Vector3 v2)
        {
            var q1 = new QuantizedVector3(v1);
            var q2 = new QuantizedVector3(v2);

            if (Compare(q1, q2) < 0) { V1 = q1; V2 = q2; }
            else { V1 = q2; V2 = q1; }
        }

        private static int Compare(QuantizedVector3 a, QuantizedVector3 b)
        {
            if (a.x != b.x) return a.x.CompareTo(b.x);
            if (a.y != b.y) return a.y.CompareTo(b.y);
            return a.z.CompareTo(b.z);
        }

        public override bool Equals(object obj) => obj is SpatialEdgeKey other && V1.Equals(other.V1) && V2.Equals(other.V2);
        public override int GetHashCode() => (V1, V2).GetHashCode();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Graph = new Dictionary<int, NavNode>();
    }

    public void BuildGraphFromNavMesh()
    {
        Debug.Log("Iniciando construção (WELDED) do Grafo NavMesh...");
        Graph.Clear();

        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();

        if (triangulation.vertices.Length == 0)
        {
            Debug.LogError("ERRO CRÍTICO: NavMesh vazia.");
            return;
        }

        Dictionary<SpatialEdgeKey, List<int>> edgeMap = new Dictionary<SpatialEdgeKey, List<int>>();

        for (int i = 0; i < triangulation.indices.Length / 3; i++)
        {
            int i1 = triangulation.indices[i * 3];
            int i2 = triangulation.indices[i * 3 + 1];
            int i3 = triangulation.indices[i * 3 + 2];

            Vector3 v1 = triangulation.vertices[i1];
            Vector3 v2 = triangulation.vertices[i2];
            Vector3 v3 = triangulation.vertices[i3];

            Vector3 center = (v1 + v2 + v3) / 3f;
            int polygonIndex = i;

            Graph.Add(polygonIndex, new NavNode(polygonIndex, center, new Vector3[] { v1, v2, v3 }));

            RegisterEdge(edgeMap, v1, v2, polygonIndex);
            RegisterEdge(edgeMap, v2, v3, polygonIndex);
            RegisterEdge(edgeMap, v3, v1, polygonIndex);
        }

        Debug.Log($"Passo 1: {Graph.Count} nós criados.");

        int totalConexoes = 0;
        foreach (var entry in edgeMap)
        {
            List<int> sharedPolys = entry.Value;

            if (sharedPolys.Count >= 2)
            {
                for (int a = 0; a < sharedPolys.Count; a++)
                {
                    for (int b = a + 1; b < sharedPolys.Count; b++)
                    {
                        int nodeA = sharedPolys[a];
                        int nodeB = sharedPolys[b];

                        NavNode nA = Graph[nodeA];
                        NavNode nB = Graph[nodeB];

                        if (!nA.Neighbours.Contains(nodeB))
                        {
                            nA.Neighbours.Add(nodeB);
                            nB.Neighbours.Add(nodeA);
                            totalConexoes += 2;
                        }
                    }
                }
            }
        }

        float mediaVizinhos = Graph.Count > 0 ? (float)totalConexoes / Graph.Count : 0;
        Debug.Log($"Passo 2: {totalConexoes} conexões geradas.");
        Debug.Log($"Média de Vizinhos: {mediaVizinhos:F2} (Agora deve ser ~3.0)");

#if UNITY_EDITOR
        DebugDrawGraph();
#endif
    }

    private void RegisterEdge(Dictionary<SpatialEdgeKey, List<int>> map, Vector3 v1, Vector3 v2, int polyIndex)
    {
        SpatialEdgeKey key = new SpatialEdgeKey(v1, v2);
        if (!map.ContainsKey(key))
        {
            map[key] = new List<int>();
        }
        map[key].Add(polyIndex);
    }

    public NavNode GetNode(int id)
    {
        if (Graph.TryGetValue(id, out NavNode node)) return node;
        return null;
    }

    public NavNode GetNodeFromWorldPos(Vector3 worldPos)
    {
        if (Graph.Count == 0) return null;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(worldPos, out hit, 5.0f, NavMesh.AllAreas))
        {
            NavNode closestNode = null;
            float minSqrDist = float.MaxValue;

            foreach (var node in Graph.Values)
            {
                if (Mathf.Abs(node.Center.x - hit.position.x) > 10f || Mathf.Abs(node.Center.z - hit.position.z) > 10f) continue;

                float sqrDist = (node.Center - hit.position).sqrMagnitude;
                if (sqrDist < minSqrDist)
                {
                    minSqrDist = sqrDist;
                    closestNode = node;
                }
            }
            return closestNode;
        }
        return null;
    }

    public float CalculateFraternity()
    {
        float total = 0;
        float qtdCell = 0;
        foreach (var node in Graph.Values)
        {
            if (node.IsPath)
            {
                total += node.VisitasProximas.Count;
                qtdCell++;
            }
        }
        return qtdCell > 0 ? total / qtdCell : 0;
    }

    private void DebugDrawGraph()
    {
        foreach (var node in Graph.Values)
        {
            foreach (var neighborId in node.Neighbours)
            {
                if (Graph.TryGetValue(neighborId, out NavNode neighbor))
                {
                    Debug.DrawLine(node.Center, neighbor.Center, Color.cyan, 10.0f);
                }
            }
        }
    }
}
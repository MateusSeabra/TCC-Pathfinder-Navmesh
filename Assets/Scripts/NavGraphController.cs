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

    private struct EdgeKey
    {
        public readonly int V1;
        public readonly int V2;

        public EdgeKey(int v1, int v2)
        {
            if (v1 < v2) { V1 = v1; V2 = v2; }
            else { V1 = v2; V2 = v1; }
        }

        public override bool Equals(object obj) => obj is EdgeKey other && V1 == other.V1 && V2 == other.V2;
        public override int GetHashCode() => (V1 * 397) ^ V2; 
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
        Debug.Log("Iniciando construção do Grafo NavMesh...");
        Graph.Clear();

        NavMeshTriangulation triangulation = NavMesh.CalculateTriangulation();

        if (triangulation.vertices.Length == 0)
        {
            Debug.LogError("ERRO CRÍTICO: NavMesh vazia. Faça o 'Bake' da NavMesh antes de rodar.");
            return;
        }

        Dictionary<EdgeKey, List<int>> edgeMap = new Dictionary<EdgeKey, List<int>>();

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

            RegisterEdge(edgeMap, i1, i2, polygonIndex);
            RegisterEdge(edgeMap, i2, i3, polygonIndex);
            RegisterEdge(edgeMap, i3, i1, polygonIndex);
        }

        Debug.Log($"Passo 1: {Graph.Count} nós criados.");

        int totalConexoes = 0;

        foreach (var entry in edgeMap)
        {
            List<int> sharedPolys = entry.Value;

            if (sharedPolys.Count == 2)
            {
                int nodeA = sharedPolys[0];
                int nodeB = sharedPolys[1];

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

        float mediaVizinhos = Graph.Count > 0 ? (float)totalConexoes / Graph.Count : 0;
        Debug.Log($"Passo 2: {totalConexoes} conexões geradas via Topologia Real.");
        Debug.Log($"Média de Vizinhos: {mediaVizinhos:F2} (Ideal ~3.0 para malhas triangulares)");

#if UNITY_EDITOR
        DebugDrawGraph();
#endif
    }

    private void RegisterEdge(Dictionary<EdgeKey, List<int>> map, int v1, int v2, int polyIndex)
    {
        EdgeKey key = new EdgeKey(v1, v2);
        if (!map.ContainsKey(key))
        {
            map[key] = new List<int>();
        }
        map[key].Add(polyIndex);
    }

    public Vector3[] ConvertNodesToPath(List<NavNode> nodes)
    {
        if (nodes == null || nodes.Count == 0) return null;

        return nodes.Select(n => n.Center).ToArray();
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
                if (Mathf.Abs(node.Center.x - hit.position.x) > 10f ||
                    Mathf.Abs(node.Center.z - hit.position.z) > 10f) continue;

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
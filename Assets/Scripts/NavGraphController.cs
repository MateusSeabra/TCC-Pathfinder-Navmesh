using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

public class NavNode
{
    public int PolygonIndex;
    public Vector3 Center;
    public List<int> Neighbours;

    public float PheromoneBias { get; set; }
    public List<int> VisitasProximas { get; set; }
    public bool IsPath { get; set; }

    public NavNode(int index, Vector3 center)
    {
        PolygonIndex = index;
        Center = center;
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
            Debug.LogError("ERRO CRÍTICO: Falha ao ler NavMesh. A NavMesh foi 'assada' (baked)? A superfície existe?");
            return;
        }

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

            Graph.Add(polygonIndex, new NavNode(polygonIndex, center));
        }

        Debug.Log($"Passo 1 Concluído: {Graph.Count} nós criados.");

        int totalConexoes = 0;
        foreach (var node in Graph.Values)
        {
            foreach (var otherNode in Graph.Values)
            {
                if (node == otherNode) continue;

                if (Vector3.Distance(node.Center, otherNode.Center) < 2.0f)
                {
                    if (!node.Neighbours.Contains(otherNode.PolygonIndex))
                    {
                        node.Neighbours.Add(otherNode.PolygonIndex);
                        totalConexoes++;
                    }
                    if (!otherNode.Neighbours.Contains(node.PolygonIndex))
                    {
                        otherNode.Neighbours.Add(node.PolygonIndex);
                        totalConexoes++;
                    }
                }
            }
        }

        float mediaVizinhos = Graph.Count > 0 ? (float)totalConexoes / Graph.Count : 0;

        Debug.Log($"Passo 2 Concluído: {totalConexoes} conexões estabelecidas.");
        Debug.Log($"ESTATÍSTICAS DO GRAFO: Média de Vizinhos por Nó: {mediaVizinhos:F2}");

        if (mediaVizinhos < 1.0f)
        {
            Debug.LogError("ALERTA DE FALHA: O grafo está altamente desconectado (Média < 1.0). " +
                           "Os agentes provavelmente não encontrarão caminho. " +
                           "Tente aumentar a distância de conexão ou verificar a escala da NavMesh.");
        }
        else if (mediaVizinhos < 2.0f)
        {
            Debug.LogWarning("AVISO: Conectividade baixa (Média < 2.0). Caminhos podem ser limitados.");
        }
        else
        {
            Debug.Log("<color=green>SUCESSO: Grafo construído com boa conectividade.</color>");
        }
    }

    public NavNode GetNode(int polygonIndex)
    {
        NavNode node;
        if (Graph.TryGetValue(polygonIndex, out node))
        {
            return node;
        }
        Debug.LogWarning($"Tentativa de acessar nó inexistente ID: {polygonIndex}");
        return null;
    }

    public NavNode GetNodeFromWorldPos(Vector3 worldPos)
    {
        if (Graph.Count == 0)
        {
            Debug.LogError("Tentando buscar nó em um Grafo Vazio! Certifique-se de chamar BuildGraphFromNavMesh primeiro.");
            return null;
        }

        NavMeshHit hit;

        if (NavMesh.SamplePosition(worldPos, out hit, 10.0f, NavMesh.AllAreas))
        {
            NavNode closestNode = null;
            float minDistance = float.MaxValue;

            foreach (var node in Graph.Values)
            {
                float dist = Vector3.Distance(node.Center, hit.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    closestNode = node;
                }
            }

            if (closestNode == null)
            {
                Debug.LogWarning($"Nenhum nó encontrado próximo a {worldPos} (SamplePosition funcionou, mas busca interna falhou).");
            }
            return closestNode;
        }

        Debug.LogError($"Falha ao projetar posição {worldPos} na NavMesh. O ponto está muito longe da malha ou fora dela?");
        return null;
    }

    public float CalculateFraternity()
    {
        float total = 0;
        float qtdCell = 0;

        foreach (KeyValuePair<int, NavNode> nodeEntry in Graph)
        {
            NavNode node = nodeEntry.Value;

            if (node.IsPath)
            {
                total += node.VisitasProximas.Count;
                qtdCell++;
            }
        }

        return qtdCell > 0 ? total / qtdCell : 0;
    }
}
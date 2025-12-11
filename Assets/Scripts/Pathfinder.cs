using System.Collections.Generic;
using System.Linq; 
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
    [Header("ACO Settings")]
    [Tooltip("Fator de multiplicação do feromônio (0.0 a 1.0). Quanto menor, mais atrativo o caminho.")]
    public float BiasFactor = 0.75f;

    [Tooltip("Valor mínimo que o viés do feromônio pode atingir.")]
    public float BiasCap = 0.5f;

    [Header("Algorithm Settings")]
    [SerializeField] private int maxIterations = 50000; 
    private const int PropagationDepth = 2; 

    private NavGraphController _navGraph;

    private HashSet<NavNode> _processedNodesInPass;

    private void Start()
    {
        _navGraph = NavGraphController.Instance;
        if (_navGraph == null)
        {
            Debug.LogError("[Pathfinder] CRÍTICO: NavGraphController não encontrado.");
            this.enabled = false;
        }
    }

    public PathResult CalculatePath(int agentId, Vector3 startPos, Vector3 endPos)
    {
        if (_navGraph == null || _navGraph.Graph.Count == 0)
            return new PathResult { Success = false };

        NavNode startNode = _navGraph.GetNodeFromWorldPos(startPos);
        NavNode endNode = _navGraph.GetNodeFromWorldPos(endPos);

        if (startNode == null || endNode == null)
        {
            Debug.LogWarning($"[Pathfinder] Agente {agentId}: Fora da NavMesh ou grafo não construído.");
            return new PathResult { Success = false };
        }

        AStarNode firstNode = new AStarNode(startNode, null, endNode.Center, startNode.PheromoneBias);

        List<AStarNode> openList = new List<AStarNode>();
        HashSet<int> closedSet = new HashSet<int>(); 

        openList.Add(firstNode);

        int safetyLoop = 0;

        while (openList.Count > 0)
        {
            safetyLoop++;
            if (safetyLoop > maxIterations)
            {
                Debug.LogError($"[Pathfinder] Falha: Limite de iterações ({maxIterations}) excedido para agente {agentId}.");
                return new PathResult { Success = false };
            }

            openList.Sort((a, b) => a.TotalCost.CompareTo(b.TotalCost));
            AStarNode currentNode = openList[0];
            openList.RemoveAt(0);

            if (currentNode.NodeData == endNode)
            {
                return ReconstructPath(currentNode, agentId);
            }

            closedSet.Add(currentNode.NodeData.PolygonIndex);

            foreach (int neighborId in currentNode.NodeData.Neighbours)
            {
                if (closedSet.Contains(neighborId)) continue;

                NavNode neighborNode = _navGraph.GetNode(neighborId);
                if (neighborNode == null) continue;

                AStarNode neighborAStar = new AStarNode(neighborNode, currentNode, endNode.Center, neighborNode.PheromoneBias);

                AStarNode existingNode = openList.Find(x => x.NodeData.PolygonIndex == neighborId);

                if (existingNode != null)
                {
                    if (existingNode.TotalCost <= neighborAStar.TotalCost) continue;
                    openList.Remove(existingNode);
                }

                openList.Add(neighborAStar);
            }
        }

        return new PathResult { Success = false };
    }

    private PathResult ReconstructPath(AStarNode finalNode, int agentId)
    {
        List<NavNode> pathNodes = new List<NavNode>();
        AStarNode current = finalNode;
        float totalLength = 0;

        while (current != null)
        {
            pathNodes.Add(current.NodeData);
            if (current.Parent != null)
            {
                totalLength += Vector3.Distance(current.NodeData.Center, current.Parent.NodeData.Center);
            }
            current = current.Parent;
        }

        pathNodes.Reverse();

        ApplyPheromones(pathNodes, agentId);

        Vector3[] waypoints = pathNodes.Select(n => n.Center).ToArray();

        return new PathResult
        {
            Success = true,
            TotalLength = totalLength,
            Waypoints = waypoints,
            PathNodes = pathNodes 
        };
    }

    private void ApplyPheromones(List<NavNode> path, int agentId)
    {
        _processedNodesInPass = new HashSet<NavNode>();

        foreach (NavNode node in path)
        {
            if (node.VisitasProximas != null && !node.VisitasProximas.Contains(agentId))
            {
                node.VisitasProximas.Add(agentId);
            }

            SpreadPheromoneRecursive(node, node, 1);
        }
    }

    private void SpreadPheromoneRecursive(NavNode targetNode, NavNode originalNode, int depth)
    {
        if (targetNode == null) return;

        if (!_processedNodesInPass.Contains(targetNode))
        {
            float distanceWeight = (targetNode == originalNode) ? 0 : 1f;
            float factor = 0.02f * distanceWeight;

            float multiplier = Mathf.Clamp(BiasFactor + factor, 0.9f, 1.0f);

            targetNode.PheromoneBias *= multiplier;

            if (targetNode.PheromoneBias < BiasCap)
                targetNode.PheromoneBias = BiasCap;

            _processedNodesInPass.Add(targetNode);
        }

        if (depth < PropagationDepth)
        {
            foreach (int neighborId in targetNode.Neighbours)
            {
                NavNode neighbor = _navGraph.GetNode(neighborId);
                SpreadPheromoneRecursive(neighbor, originalNode, depth + 1);
            }
        }
    }

    private class AStarNode
    {
        public NavNode NodeData;
        public AStarNode Parent;

        public float G_Cost; 
        public float H_Cost; 
        public float TotalCost; 

        public AStarNode(NavNode node, AStarNode parent, Vector3 targetPos, float pheromoneWeight)
        {
            NodeData = node;
            Parent = parent;

            H_Cost = Vector3.Distance(node.Center, targetPos);

            G_Cost = 0;
            if (parent != null)
            {
                G_Cost = parent.G_Cost + Vector3.Distance(parent.NodeData.Center, node.Center);
            }

            TotalCost = (G_Cost + H_Cost) * pheromoneWeight;
        }
    }
}

public struct PathResult
{
    public bool Success;
    public float TotalLength;
    public Vector3[] Waypoints; 
    public List<NavNode> PathNodes; 
}
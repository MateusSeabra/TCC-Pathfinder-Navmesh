using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using Unity.AI.Navigation;

public class TestController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Text biasName;
    [SerializeField] private Text capName;
    [SerializeField] private Text lengthName;
    [SerializeField] private Text mapName;

    [Header("Configuração 3D e NavMesh")]
    public NavMeshSurface GroundSurface;
    public GameObject ObstaclePrefab;
    public GameObject ObstacleContainer;
    public GameObject MarkerContainer;
    public GameObject StartPointPrefab;
    public GameObject EndPointPrefab;

    [Header("Debug Visual")]
    public GameObject DebugMark;
    public GameObject DebugPathMark;
    public GameObject MainCamera;

    private NavGraphController _navGraph;
    private List<GameObject> _spawnedObjects;
    private Vector3 _targetPos;
    private List<Vector3> _unitiesToTest;

    public float BiasFactor;
    private float BiasCap = 0.75f;
    private int biasType;
    private int pathType;
    private bool Control1;
    private bool Control2;
    private float fraternidadeTeste;
    private int mapType;
    private float mediaTeste;

    private List<NavNode> pathsDessaPassadaNodes;
    private readonly int propagacoes = 2;
    private int totalTestes;
    private string nomeCenario;
    private string nomePathfinder;

    private void Start()
    {
        if (NavGraphController.Instance == null)
        {
            Debug.LogError("ERRO CRÍTICO: 'NavGraphController' não encontrado na cena! O script TestController foi desativado.");
            this.enabled = false;
            return;
        }
        _navGraph = NavGraphController.Instance;

        if (GroundSurface == null) Debug.LogError("SETUP: 'GroundSurface' (NavMeshSurface) não está atribuído.");
        if (ObstaclePrefab == null) Debug.LogError("SETUP: 'ObstaclePrefab' não está atribuído.");
        if (ObstacleContainer == null) Debug.LogError("SETUP: 'ObstacleContainer' não está atribuído.");
        if (MarkerContainer == null) Debug.LogError("SETUP: 'MarkerContainer' não está atribuído.");

        _unitiesToTest = new List<Vector3>();
        _spawnedObjects = new List<GameObject>();
    }

    private void Update()
    {
        if (Control1)
        {
            Control1 = false;
            StartTest();
        }
        if (Control2 || Input.GetKeyDown(KeyCode.Space))
        {
            Control2 = false;
            Control1 = ConfigMixer();
        }
        if (Input.GetKeyDown(KeyCode.T))
            StartTest();
    }

    public bool ConfigMixer()
    {
        print($"Configuração: Mapa {mapType} / Bias {biasType} / Path {pathType}");

        switch (mapType)
        {
            case 0:
                LonelyTree();
                if (mapName) mapName.text = "Map: Lonely Tree";
                break;
            case 1:
                ChineseWall();
                if (mapName) mapName.text = "Map: Chinese Wall";
                break;
            case 2:
                BigRock();
                if (mapName) mapName.text = "Map: Boulder";
                break;
        }

        switch (pathType)
        {
            case 0: BiasCap = 0.75f; break;
            case 1: BiasCap = 0.5f; break;
            case 2: BiasCap = 0.01f; break;
        }

        if (capName) capName.text = "Cap: " + BiasCap;

        switch (biasType)
        {
            case 0: BiasFactor = 0.5f; break;
            case 1: BiasFactor = 0.6f; break;
            case 2: BiasFactor = 0.75f; break;
            case 3: BiasFactor = 0.9f; break;
            case 4: BiasFactor = 0.95f; break;
            case 5: BiasFactor = 0.99f; break;
        }

        if (biasName) biasName.text = "Bias: " + BiasFactor;

        OrderUnitsByDistanceTarget();
        return NextConfig();
    }

    public void ClearScenario()
    {
        _unitiesToTest.Clear();
        if (_spawnedObjects != null)
        {
            foreach (GameObject obj in _spawnedObjects)
            {
                if (obj != null) Destroy(obj);
            }
            _spawnedObjects.Clear();
        }
    }

    public void SpawnObstacle(Vector3 position)
    {
        if (ObstaclePrefab == null) return;

        Transform parent = ObstacleContainer != null ? ObstacleContainer.transform : null;
        GameObject obstacle = Instantiate(ObstaclePrefab, position, Quaternion.identity, parent);
        _spawnedObjects.Add(obstacle);
    }

    public void SetStartingPos(Vector3 pos)
    {
        if (StartPointPrefab != null)
        {
            Transform parent = MarkerContainer != null ? MarkerContainer.transform : null;
            _spawnedObjects.Add(Instantiate(StartPointPrefab, pos, Quaternion.identity, parent));
        }
        _unitiesToTest.Add(pos);
    }

    public void SetTargetPos(Vector3 pos)
    {
        if (EndPointPrefab != null)
        {
            Transform parent = MarkerContainer != null ? MarkerContainer.transform : null;
            _spawnedObjects.Add(Instantiate(EndPointPrefab, pos, Quaternion.identity, parent));
        }
        _targetPos = pos;
    }

    public void FinalizeScenarioBuild()
    {
        if (GroundSurface != null)
        {
            GroundSurface.BuildNavMesh();
        }
        else
        {
            Debug.LogError("ERRO: Tentando construir NavMesh sem GroundSurface atribuído!");
            return;
        }

        if (_navGraph != null)
        {
            _navGraph.BuildGraphFromNavMesh();
        }
    }

    public void LonelyTree()
    {
        nomeCenario = "Árvore solitária";
        ClearScenario();

        SetTargetPos(new Vector3(14, 1f, 24));
        int startX = 10;
        int startZ = 5;
        int qtdX = 9;
        int qtdY = 2;

        for (int i = 0; i < qtdY; i++)
        {
            for (int j = 0; j < qtdX; j++)
            {
                SetStartingPos(new Vector3(j + startX, 1f, startZ + i));
            }
        }

        SpawnObstacle(new Vector3(14, 0, 15));
        FinalizeScenarioBuild();
    }

    public void ChineseWall()
    {
        nomeCenario = "Muralha da China";
        ClearScenario();

        SetTargetPos(new Vector3(14, 1f, 27));
        int startX = 10;
        int startZ = 2;
        int qtdX = 9;
        int qtdY = 2;

        for (int i = 0; i < qtdY; i++)
        {
            for (int j = 0; j < qtdX; j++)
            {
                SetStartingPos(new Vector3(j + startX, 1f, startZ + i));
            }
        }

        int treeX = 1;
        int treeY = 17;

        for (int i = 0; i < treeY; i++)
        {
            for (int j = 0; j < treeX; j++)
            {
                SpawnObstacle(new Vector3(j + 14, 0, i + 6));
            }
        }
        FinalizeScenarioBuild();
    }

    public void BigRock()
    {
        nomeCenario = "Pedregulho";
        ClearScenario();

        SetTargetPos(new Vector3(14, 1f, 24));
        int startX = 10;
        int startZ = 5;
        int qtdX = 9;
        int qtdY = 2;

        for (int i = 0; i < qtdY; i++)
        {
            for (int j = 0; j < qtdX; j++)
            {
                SetStartingPos(new Vector3(j + startX, 1f, startZ + i));
            }
        }

        int treeX = 11;
        int treeY = 10;

        for (int i = 0; i < treeY; i++)
        {
            for (int j = 0; j < treeX; j++)
            {
                SpawnObstacle(new Vector3(j + 9, 0, i + 10));
            }
        }
        FinalizeScenarioBuild();
    }

    public void OrderUnitsByDistanceTarget()
    {
        nomePathfinder = "Alvo";
        _unitiesToTest.Sort((a, b) =>
        {
            float distanciaA = Vector3.Distance(a, _targetPos);
            float distanciaB = Vector3.Distance(b, _targetPos);
            return distanciaA.CompareTo(distanciaB);
        });
    }

    public void StartTest()
    {
        if (_targetPos == Vector3.zero && _unitiesToTest.Count == 0) 
        {
            Debug.LogError("ERRO: Nenhum teste selecionado ou cenário vazio.");
            return;
        }

        totalTestes++;
        float distanciasAcumuladas = 0;
        int caminhosSucesso = 0;

        for (int i = 0; i < _unitiesToTest.Count; i++)
        {
            float resp = Pathfinder(i, _unitiesToTest[i], _targetPos);

            if (resp >= 0)
            {
                distanciasAcumuladas += resp;
                caminhosSucesso++;
            }
        }

        if (caminhosSucesso > 0)
            mediaTeste = distanciasAcumuladas / caminhosSucesso;
        else
            mediaTeste = 0;

        if (_navGraph != null)
            fraternidadeTeste = _navGraph.CalculateFraternity();

        if (lengthName) lengthName.text = "Avg. Length: " + mediaTeste.ToString("F2");
        WriteLog();
    }

    public void SpreadMarker(NavNode node, NavNode originalNode, int propagacaoAtual)
    {
        if (node == null) return;

        if (pathsDessaPassadaNodes == null) pathsDessaPassadaNodes = new List<NavNode>();

        if (!pathsDessaPassadaNodes.Contains(node) && !node.IsPath)
        {
            float distance = (node == originalNode) ? 0 : 1f;
            float factor = 0.02f * distance;
            pathsDessaPassadaNodes.Add(node);

            node.PheromoneBias *= Mathf.Clamp(BiasFactor + factor, 0.9f, 1f);
            if (node.PheromoneBias < BiasCap) node.PheromoneBias = BiasCap;
        }
        else if (!pathsDessaPassadaNodes.Contains(node) && node.IsPath)
        {
            pathsDessaPassadaNodes.Add(node);
            node.PheromoneBias *= BiasFactor;
            if (node.PheromoneBias < BiasCap) node.PheromoneBias = BiasCap;
        }

        if (propagacaoAtual < propagacoes)
        {
            foreach (int neighbourIndex in node.Neighbours)
            {
                SpreadMarker(_navGraph.GetNode(neighbourIndex), originalNode, propagacaoAtual + 1);
            }
        }
    }

    public float Pathfinder(int id, Vector3 startingPos, Vector3 endingPos)
    {
        if (_navGraph == null) return -1;

        NavNode startNode = _navGraph.GetNodeFromWorldPos(startingPos);
        NavNode endNode = _navGraph.GetNodeFromWorldPos(endingPos);

        if (startNode == null || endNode == null)
        {
            Debug.LogError($"Agente {id}: Falha ao encontrar nó inicial/final na NavMesh.");
            return -1;
        }

        AStarNode firstNode = new AStarNode(startNode, null, endNode.Center, startNode.PheromoneBias);
        List<AStarNode> toCheckList = new List<AStarNode>();
        List<AStarNode> checkedList = new List<AStarNode>();

        toCheckList.Add(firstNode);

        int safetyLoopCount = 0;
        int maxLoops = 50000;

        while (toCheckList.Count != 0)
        {
            safetyLoopCount++;
            if (safetyLoopCount > maxLoops)
            {
                Debug.LogError($"ERRO: Pathfinder excedeu o limite de segurança ({maxLoops} iterações) para o agente {id}. Verifique se o alvo é alcançável.");
                return -1;
            }

            AStarNode nodeAtual = toCheckList[0];
            toCheckList.RemoveAt(0);

            foreach (int neighbourIndex in nodeAtual.NodeData.Neighbours)
            {
                NavNode posAux = _navGraph.GetNode(neighbourIndex);
                if (posAux == null) continue;

                if (posAux == endNode)
                {
                    List<NavNode> caminhoFinal = new List<NavNode>();
                    caminhoFinal.Add(posAux);
                    caminhoFinal.Add(nodeAtual.NodeData);

                    AStarNode auxNode = nodeAtual.parent;
                    while (auxNode != null)
                    {
                        caminhoFinal.Add(auxNode.NodeData);
                        auxNode = auxNode.parent;
                    }
                    caminhoFinal.Reverse();

                    Debug.Log($"Caminho encontrado para unidade {id}! Passos: {caminhoFinal.Count}");

                    foreach (NavNode elem in caminhoFinal) elem.IsPath = true;

                    pathsDessaPassadaNodes = new List<NavNode>();

                    foreach (NavNode elem in caminhoFinal)
                    {
                        SpreadMarker(elem, elem, 1);
                        if (elem.VisitasProximas != null) elem.VisitasProximas.Add(id);
                    }

                    foreach (NavNode elem in caminhoFinal) elem.IsPath = false;

                    GameObject daddy = MarkPath(caminhoFinal[0].Center, null, id);
                    float acumulado = 0;

                    for (int i = 1; i < caminhoFinal.Count; i++)
                    {
                        acumulado += Vector3.Distance(caminhoFinal[i - 1].Center, caminhoFinal[i].Center);
                        daddy = MarkPath(caminhoFinal[i].Center, daddy, id);
                    }
                    return acumulado;
                }

                AStarNode auxNodeAStar = new AStarNode(posAux, nodeAtual, endNode.Center, posAux.PheromoneBias);

                AStarNode auxAuxNode1 = toCheckList.Find(x => x.NodeData.PolygonIndex == auxNodeAStar.NodeData.PolygonIndex);
                if (auxAuxNode1 != null && auxAuxNode1.totalDistance <= auxNodeAStar.totalDistance) continue;

                AStarNode auxAuxNode2 = checkedList.Find(x => x.NodeData.PolygonIndex == auxNodeAStar.NodeData.PolygonIndex);
                if (auxAuxNode2 != null && auxAuxNode2.totalDistance <= auxNodeAStar.totalDistance) continue;

                int insertIndex = 0;
                for (insertIndex = 0; insertIndex < toCheckList.Count; insertIndex++)
                {
                    if (auxNodeAStar.totalDistance < toCheckList[insertIndex].totalDistance)
                        break;
                }
                toCheckList.Insert(insertIndex, auxNodeAStar);

                if (DebugMark != null)
                {
                    Instantiate(DebugMark,
                        new Vector3(auxNodeAStar.NodeData.Center.x, auxNodeAStar.NodeData.Center.y + 0.1f, auxNodeAStar.NodeData.Center.z),
                        Quaternion.identity,
                        ObstacleContainer != null ? ObstacleContainer.transform : null
                    );
                }
            }
            checkedList.Add(nodeAtual);
        }
        return -1;
    }

    public GameObject MarkPath(Vector3 pos, GameObject parent, int id)
    {
        if (DebugPathMark == null) return null;

        Vector3 posAux = new Vector3(pos.x, pos.y + 0.5f, pos.z); 

        Transform parentTransform = MarkerContainer != null ? MarkerContainer.transform : null;
        GameObject pPoint = Instantiate(DebugPathMark, posAux, Quaternion.identity, parentTransform);

        List<Color32> colors = new List<Color32>
        {
            new Color32(0x80, 0x00, 0x00, 0xFF), new Color32(0x9A, 0x63, 0x24, 0xFF),
            new Color32(0x80, 0x80, 0x00, 0xFF), new Color32(0x46, 0x99, 0x90, 0xFF),
            new Color32(0x00, 0x00, 0x00, 0xFF), new Color32(0xe6, 0x19, 0x4B, 0xFF),
            new Color32(0xf5, 0x82, 0x31, 0xFF), new Color32(0xff, 0xe1, 0x19, 0xFF),
            new Color32(0xbf, 0xef, 0x45, 0xFF), new Color32(0x3c, 0xb4, 0x4b, 0xFF),
            new Color32(0x42, 0xd4, 0xf4, 0xFF), new Color32(0x43, 0x63, 0xd8, 0xFF),
            new Color32(0x91, 0x1e, 0xb4, 0xFF), new Color32(0xf0, 0x32, 0xe6, 0xFF),
            new Color32(0xfa, 0xbe, 0xd4, 0xFF), new Color32(0xff, 0xd8, 0xb1, 0xFF),
            new Color32(0x00, 0x00, 0x75, 0xFF), new Color32(0xff, 0xff, 0xff, 0xFF)
        };

        if (parent != null && id >= 0 && id < colors.Count)
        {
            PathDrawer pDrawer = pPoint.GetComponent<PathDrawer>();
            if (pDrawer != null)
            {
                pDrawer.SetParent(id, parent, colors[id]);
            }
        }

        return pPoint;
    }

    public void WriteLog()
    {
        try
        {
            string date = DateTime.Now.ToString("yyyy-MM-dd");
            string folderPath = "Assets/Logs/";
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string path = folderPath + "Resultados-" + date + "-v3.txt";
            string separator = "; ";

            string toWrite = totalTestes + separator;
            toWrite += nomeCenario + separator;
            toWrite += nomePathfinder + separator;
            toWrite += BiasFactor + separator;
            toWrite += BiasCap + separator;
            toWrite += mediaTeste.ToString("F2") + separator;
            toWrite += fraternidadeTeste.ToString("F4");

            using (StreamWriter writer = new StreamWriter(path, true))
            {
                writer.WriteLine(toWrite);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Erro ao escrever log: " + e.Message);
        }
    }

    private bool NextConfig()
    {
        if (pathType == 3)
        {
            print("DONE!");
            return false;
        }

        mapType++;

        if (mapType == 3)
        {
            mapType = 0;
            biasType++;

            if (biasType == 6)
            {
                biasType = 0;
                pathType++;
            }
        }
        return true;
    }
}

public class AStarNode
{
    public NavNode NodeData;
    public float heuristicDistance;
    public float realDistance;
    public float totalDistance;
    public AStarNode parent;

    public AStarNode(NavNode node, AStarNode parent, Vector3 endingPos, float pheromoneWeight)
    {
        this.NodeData = node;
        this.parent = parent;

        heuristicDistance = Vector3.Distance(node.Center, endingPos);
        realDistance = 0;

        if (parent != null)
        {
            realDistance = parent.realDistance + Vector3.Distance(parent.NodeData.Center, node.Center);
        }

        totalDistance = (realDistance + heuristicDistance) * pheromoneWeight;
    }
}
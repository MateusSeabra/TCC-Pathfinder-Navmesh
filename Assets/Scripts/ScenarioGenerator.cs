using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

public class ScenarioGenerator : MonoBehaviour
{
    [Header("NavMesh References")]
    [Tooltip("Referência obrigatória para o componente que faz o Bake da NavMesh.")]
    [SerializeField] private NavMeshSurface groundSurface;

    [Header("Prefabs")]
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private GameObject startPointPrefab;
    [SerializeField] private GameObject endPointPrefab;

    [Header("Containers (Organization)")]
    [SerializeField] private Transform obstacleContainer;
    [SerializeField] private Transform markerContainer;

    public Vector3 TargetPosition { get; private set; }
    public List<Vector3> StartPositions { get; private set; }

    private List<GameObject> _spawnedObjects;

    public enum ScenarioType
    {
        LonelyTree = 0,
        ChineseWall = 1,
        BigRock = 2
    }

    private void Awake()
    {
        _spawnedObjects = new List<GameObject>();
        StartPositions = new List<Vector3>();

        if (groundSurface == null) Debug.LogError("[ScenarioGenerator] ERRO: 'GroundSurface' não atribuído!");
        if (obstaclePrefab == null) Debug.LogError("[ScenarioGenerator] ERRO: 'ObstaclePrefab' não atribuído!");
    }

    public void BuildScenario(int mapTypeIndex)
    {
        BuildScenario((ScenarioType)mapTypeIndex);
    }

    public void BuildScenario(ScenarioType type)
    {
        Debug.Log($"[ScenarioGenerator] Construindo cenário: {type}");

        ClearScenario();

        switch (type)
        {
            case ScenarioType.LonelyTree:
                SetupLonelyTree();
                break;
            case ScenarioType.ChineseWall:
                SetupChineseWall();
                break;
            case ScenarioType.BigRock:
                SetupBigRock();
                break;
            default:
                Debug.LogError($"[ScenarioGenerator] Tipo de cenário desconhecido: {type}");
                return;
        }

        FinalizeScenarioBuild();
    }

    private void ClearScenario()
    {
        StartPositions.Clear();
        TargetPosition = Vector3.zero;

        if (_spawnedObjects != null)
        {
            foreach (GameObject obj in _spawnedObjects)
            {
                if (obj != null) Destroy(obj);
            }
            _spawnedObjects.Clear();
        }
    }

    private void SetupLonelyTree()
    {
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
    }

    private void SetupChineseWall()
    {
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
    }

    private void SetupBigRock()
    {
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
    }

    private void SpawnObstacle(Vector3 position)
    {
        if (obstaclePrefab == null) return;

        GameObject obstacle = Instantiate(obstaclePrefab, position, Quaternion.identity, obstacleContainer);
        _spawnedObjects.Add(obstacle);
    }

    private void SetStartingPos(Vector3 pos)
    {
        StartPositions.Add(pos);

        if (startPointPrefab != null)
        {
            GameObject marker = Instantiate(startPointPrefab, pos, Quaternion.identity, markerContainer);
            _spawnedObjects.Add(marker);
        }
    }

    private void SetTargetPos(Vector3 pos)
    {
        TargetPosition = pos;

        if (endPointPrefab != null)
        {
            GameObject marker = Instantiate(endPointPrefab, pos, Quaternion.identity, markerContainer);
            _spawnedObjects.Add(marker);
        }
    }

    private void FinalizeScenarioBuild()
    {
        if (groundSurface != null)
        {
            groundSurface.BuildNavMesh();
        }

        if (NavGraphController.Instance != null)
        {
            NavGraphController.Instance.BuildGraphFromNavMesh();
        }
        else
        {
            Debug.LogError("[ScenarioGenerator] AVISO: NavGraphController não encontrado. O grafo lógico não foi atualizado.");
        }
    }
}
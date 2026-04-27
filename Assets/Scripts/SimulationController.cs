using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using Unity.AI.Navigation;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class SimulationController : MonoBehaviour
{
    [System.Serializable]
    public struct TestConfiguration
    {
        public int MapIndex;
        public float Bias;
        public float Cap;
        public float Voxel;
        public int Tile;
        public int SortIndex;
    }

    [Header("Modules Integration")]
    [SerializeField] private ScenarioGenerator _scenarioGenerator;
    [SerializeField] private Pathfinder _pathfinder;
    [SerializeField] private TestLogger _logger;

    [Header("Visuals")]
    [Tooltip("Prefab que contém o script PathDrawer.")]
    [SerializeField] private GameObject agentDrawerPrefab;
    [SerializeField] private Transform visualizerContainer;

    [Header("UI References")]
    [SerializeField] private Text mapNameText;
    [SerializeField] private Text biasNameText;
    [SerializeField] private Text capNameText;
    [SerializeField] private Text lengthNameText;

    [Header("Configurações do NavMesh")]
    [SerializeField] private NavMeshSurface navMeshSurface;

    [Header("UI Adicional")]
    [SerializeField] private Text textVoxelSize;
    [SerializeField] private Text textTileSize;

    [Header("Automação - Valores para Varredura (4 cada)")]
    [SerializeField] private float[] _biasFactors = new float[4] { 0.5f, 0.75f, 0.9f, 0.95f };
    [SerializeField] private float[] _biasCaps = new float[4] { 0.01f, 0.5f, 0.75f, 1.0f };
    [SerializeField] private float[] _voxelSizes = new float[4] { 0.02f, 0.05f, 0.1f, 0.16f };
    [SerializeField] private int[] _tileSizes = new int[4] { 16, 32, 64, 128 };

    [Header("Automação - Filtro de Cenário")]
    [Tooltip("Se marcado, a automação rodará apenas no cenário selecionado abaixo.")]
    [SerializeField] private bool testOnlySelectedMap = true;
    [SerializeField] private ScenarioGenerator.ScenarioType targetMapToTest;

    private List<TestConfiguration> _testQueue = new List<TestConfiguration>();
    private int _currentQueueIndex = 0;

    private List<Vector3> _activeUnits; 
    private List<PathDrawer> _spawnedDrawers;

    private float currentVoxelSize;
    private int currentTileSize;

    private int _mapTypeIndex;
    private int _biasTypeIndex;
    private int _sortTypeIndex;
    private int _totalTests;
    private string _currentSortModeName;
    private string _currentScenarioName;
    private bool _triggerTest;
    private bool _triggerNextConfig;

    private readonly List<Color32> _pathColors = new List<Color32>
    {
        new Color32(0x80, 0x00, 0x00, 0xFF), new Color32(0x9A, 0x63, 0x24, 0xFF),
        new Color32(0x80, 0x80, 0x00, 0xFF), new Color32(0x46, 0x99, 0x90, 0xFF),
        new Color32(0x00, 0x00, 0x00, 0xFF), new Color32(0xe6, 0x19, 0x4B, 0xFF),
        new Color32(0xf5, 0x82, 0x31, 0xFF), new Color32(0xff, 0xe1, 0x19, 0xFF),
        new Color32(0xbf, 0xef, 0x45, 0xFF), new Color32(0x3c, 0xb4, 0x4b, 0xFF),
        new Color32(0x42, 0xd4, 0xf4, 0xFF), new Color32(0x43, 0x63, 0xd8, 0xFF),
        new Color32(0x91, 0x1e, 0xb4, 0xFF), new Color32(0xf0, 0x32, 0xe6, 0xFF),
        new Color32(0xfa, 0xbe, 0xd4, 0xFF), new Color32(0xff, 0xd8, 0xb1, 0xFF),
        new Color32(0x00, 0x00, 0x75, 0xFF), new Color32(0xa9, 0xa9, 0xa9, 0xFF)
    };

    private void Awake()
    {
        _activeUnits = new List<Vector3>();
        _spawnedDrawers = new List<PathDrawer>();
        GenerateTestQueue();
        _currentQueueIndex = 0;

        if (!_scenarioGenerator || !_pathfinder)
        {
            Debug.LogError("[SimulationController] Dependências não atribuídas no Inspector!");
            this.enabled = false;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StopAllCoroutines();
            StartCoroutine(RunAutomationSuite());
        }
    }

    private void RunBatchTest()
    {
        if (_activeUnits.Count == 0 || _scenarioGenerator.TargetPosition == Vector3.zero)
        {
            UnityEngine.Debug.LogError("[SimulationController] Cenário vazio ou inválido.");
            return;
        }

        _totalTests++;
        float accumulatedDistance = 0;
        int successCount = 0;

        ClearVisuals();

        if (NavGraphController.Instance != null && NavGraphController.Instance.Graph != null)
        {
            foreach (var node in NavGraphController.Instance.Graph.Values)
            {
                node.PheromoneBias = 1.0f;
                node.IsPath = false;
                if (node.VisitasProximas != null) node.VisitasProximas.Clear();
            }
        }

        Stopwatch stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < _activeUnits.Count; i++)
        {
            PathResult result = _pathfinder.CalculatePath(i, _activeUnits[i], _scenarioGenerator.TargetPosition);

            if (result.Success)
            {
                accumulatedDistance += result.TotalLength;
                successCount++;

                DrawPathForAgent(i, result.Waypoints);
            }
        }

        stopwatch.Stop();
        long calcTimeMs = stopwatch.ElapsedMilliseconds;

        float avgLength = successCount > 0 ? accumulatedDistance / successCount : 0;

        float fraternity = 0;
        if (NavGraphController.Instance != null)
            fraternity = NavGraphController.Instance.CalculateFraternity();

        if (_logger != null)
        {
            _logger.WriteLog(
                _totalTests,
                _currentScenarioName,
                _currentSortModeName,
                _pathfinder.BiasFactor,
                _pathfinder.BiasCap,
                currentVoxelSize,
                currentTileSize,
                avgLength,
                fraternity,
                calcTimeMs
            );
        }

        UpdateUI_Result(avgLength, fraternity, calcTimeMs);
    }

    private void ExtractNavMeshSettings()
    {
        if (navMeshSurface == null)
        {
            Debug.LogError("NavMeshSurface não foi linkado no Inspector!");
            return;
        }

        NavMeshBuildSettings buildSettings = navMeshSurface.GetBuildSettings();

        currentVoxelSize = buildSettings.voxelSize;
        currentTileSize = buildSettings.tileSize;

        if (textVoxelSize != null)
            textVoxelSize.text = $"Voxel Size: {currentVoxelSize:F3}";

        if (textTileSize != null)
            textTileSize.text = $"Tile Size: {currentTileSize}";
    }

    private void DrawPathForAgent(int agentIndex, Vector3[] waypoints)
    {
        if (agentDrawerPrefab == null) return;

        if (agentIndex >= _spawnedDrawers.Count)
        {
            GameObject obj = Instantiate(agentDrawerPrefab, Vector3.zero, Quaternion.identity, visualizerContainer);
            PathDrawer drawer = obj.GetComponent<PathDrawer>();
            _spawnedDrawers.Add(drawer);
        }

        PathDrawer currentDrawer = _spawnedDrawers[agentIndex];

        Color32 color = _pathColors[agentIndex % _pathColors.Count];

        currentDrawer.SetPath(agentIndex, waypoints, UnityEngine.AI.NavMeshPathStatus.PathComplete, color);
    }

    private void ClearVisuals()
    {
        foreach (var drawer in _spawnedDrawers)
        {
            drawer.gameObject.SetActive(false);
        }

        for (int i = 0; i < _activeUnits.Count; i++)
        {
            if (i < _spawnedDrawers.Count) _spawnedDrawers[i].gameObject.SetActive(true);
        }
    }

    private void ApplyUnitSorting()
    {
        Vector3 target = _scenarioGenerator.TargetPosition;

        switch (_sortTypeIndex)
        {
            case 0:
                _currentSortModeName = "Alvo";
                _activeUnits.Sort((a, b) => Vector3.Distance(a, target).CompareTo(Vector3.Distance(b, target)));
                break;
            case 1:
                _currentSortModeName = "Centro";
                Vector3 center = GetCenterPoint(_activeUnits);
                _activeUnits.Sort((a, b) => Vector3.Distance(a, center).CompareTo(Vector3.Distance(b, center)));
                break;
            case 2:
                _currentSortModeName = "Misto";
                Vector3 c = GetCenterPoint(_activeUnits);
                _activeUnits.Sort((a, b) =>
                {
                    float distA = (Vector3.Distance(a, c) + Vector3.Distance(a, target)) / 2;
                    float distB = (Vector3.Distance(b, c) + Vector3.Distance(b, target)) / 2;
                    return distA.CompareTo(distB);
                });
                break;
        }
    }

    private Vector3 GetCenterPoint(List<Vector3> points)
    {
        if (points.Count == 0) return Vector3.zero;
        Vector3 sum = Vector3.zero;
        foreach (var p in points) sum += p;
        return sum / points.Count;
    }

    private void UpdateUI_MapName()
    {
        if (!mapNameText) return;
        string[] names = { "Lonely Tree", "Chinese Wall", "Boulder", "Forest", "Broken Rock", "Maze" };
        if (_mapTypeIndex < names.Length)
            mapNameText.text = $"Cenário: {names[_mapTypeIndex]}";
    }

    private void UpdateUI_BiasInfo()
    {
        if (biasNameText) biasNameText.text = $"Fator de Feromônio (Bias): {_pathfinder.BiasFactor:F2}";
        if (capNameText) capNameText.text = $"Limite de Redução (Cap): {_pathfinder.BiasCap:F2}";
    }

    private void UpdateUI_Result(float avg, float fraternity, long calcTimeMs)
    {
        if (lengthNameText)
        {
            lengthNameText.text = $"Comprimento Médio: {avg:F2}\n" +
                                  "\n" +
                                  $"Coesão (Fraternidade): {fraternity:F2}\n" +
                                  "\n" +
                                  $"Tempo de Processamento: {calcTimeMs} ms";
        }
    }

    private void GenerateTestQueue()
    {
        _testQueue.Clear();

        // NOVO: Define onde o loop de mapas começa e termina baseado na sua escolha do Inspector
        int startMap = testOnlySelectedMap ? (int)targetMapToTest : 0;
        int endMap = testOnlySelectedMap ? (int)targetMapToTest + 1 : 6;

        for (int s = 0; s < 3; s++) // Loop dos Modos de Ordenação
        {
            for (int m = startMap; m < endMap; m++) // Loop dos Mapas (agora dinâmico!)
            {
                foreach (float b in _biasFactors)
                {
                    foreach (float c in _biasCaps)
                    {
                        foreach (float v in _voxelSizes)
                        {
                            foreach (int t in _tileSizes)
                            {
                                _testQueue.Add(new TestConfiguration
                                {
                                    MapIndex = m,
                                    Bias = b,
                                    Cap = c,
                                    Voxel = v,
                                    Tile = t,
                                    SortIndex = s
                                });
                            }
                        }
                    }
                }
            }
        }
    }

    private IEnumerator RunAutomationSuite()
    {
        UnityEngine.Debug.Log($"[Automação] Iniciando bateria de {_testQueue.Count} testes...");
        _currentQueueIndex = 0;

        while (_currentQueueIndex < _testQueue.Count)
        {
            TestConfiguration config = _testQueue[_currentQueueIndex];

            navMeshSurface.overrideVoxelSize = true;
            navMeshSurface.overrideTileSize = true;

            navMeshSurface.voxelSize = config.Voxel;
            navMeshSurface.tileSize = config.Tile;

            _pathfinder.BiasFactor = config.Bias;
            _pathfinder.BiasCap = config.Cap;
            _sortTypeIndex = config.SortIndex;
            _mapTypeIndex = config.MapIndex;

            _scenarioGenerator.BuildScenario(config.MapIndex);

            ExtractNavMeshSettings();
            UpdateUI_MapName();
            UpdateUI_BiasInfo();

            string[] mapNames = { "Lonely Tree", "Chinese Wall", "Boulder", "Forest", "Broken Rock", "Maze" };
            _currentScenarioName = mapNames[config.MapIndex];

            _activeUnits = new List<Vector3>(_scenarioGenerator.StartPositions);
            ApplyUnitSorting();

            RunBatchTest();

            _currentQueueIndex++;
            yield return null;
        }

        UnityEngine.Debug.Log("[Automação] Bateria de testes concluída com sucesso!");
    }
}
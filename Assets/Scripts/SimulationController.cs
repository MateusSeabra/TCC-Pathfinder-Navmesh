using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public class SimulationController : MonoBehaviour
{
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

    private List<Vector3> _activeUnits; 
    private List<PathDrawer> _spawnedDrawers; 

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

        if (!_scenarioGenerator || !_pathfinder || !_logger)
        {
            Debug.LogError("[SimulationController] Dependências não atribuídas no Inspector!");
            this.enabled = false;
        }
    }

    private void Update()
    {
        if (_triggerTest)
        {
            _triggerTest = false;
            RunBatchTest();
        }

        if (_triggerNextConfig || Input.GetKeyDown(KeyCode.Space))
        {
            _triggerNextConfig = false;
            _triggerTest = ApplyNextConfig(); 
        }

        if (Input.GetKeyDown(KeyCode.T))
            RunBatchTest();
    }

    public bool ApplyNextConfig()
    {
        _scenarioGenerator.BuildScenario(_mapTypeIndex);
        UpdateUI_MapName();

        string[] mapNames = { "Lonely Tree", "Chinese Wall", "Big Rock" };
        _currentScenarioName = mapNames[_mapTypeIndex];

        _activeUnits = new List<Vector3>(_scenarioGenerator.StartPositions);

        //ConfigurePathfinderBias();

        UpdateUI_BiasInfo();

        ApplyUnitSorting();

        return AdvanceIndices();
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

        _logger.WriteLog(
            _totalTests,
            _currentScenarioName,
            _currentSortModeName,
            _pathfinder.BiasFactor,
            _pathfinder.BiasCap,
            avgLength,
            fraternity,
            calcTimeMs
        );

        UpdateUI_Result(avgLength, fraternity, calcTimeMs);

        UnityEngine.Debug.Log($"[RESULTADO] Teste #{_totalTests} | Cenário: {_currentScenarioName} | " +
                  $"Ordem: {_currentSortModeName} | Feromônio (Bias/Cap): {_pathfinder.BiasFactor:F2}/{_pathfinder.BiasCap:F2} | " +
                  $"Distância Média: {avgLength:F2} | Coesão (Fraternidade): {fraternity:F2} | Tempo de CPU: {calcTimeMs}ms");
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

    private void ConfigurePathfinderBias()
    {
        switch (_sortTypeIndex)
        {
            case 0: _pathfinder.BiasCap = 0.75f; break;
            case 1: _pathfinder.BiasCap = 0.5f; break;
            case 2: _pathfinder.BiasCap = 0.01f; break;
        }

        switch (_biasTypeIndex)
        {
            case 0: _pathfinder.BiasFactor = 0.5f; break;
            case 1: _pathfinder.BiasFactor = 0.6f; break;
            case 2: _pathfinder.BiasFactor = 0.75f; break;
            case 3: _pathfinder.BiasFactor = 0.9f; break;
            case 4: _pathfinder.BiasFactor = 0.95f; break;
            case 5: _pathfinder.BiasFactor = 0.99f; break;
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

    private bool AdvanceIndices()
    {
        if (_sortTypeIndex == 3) 
        {
            Debug.Log("BATCH COMPLETO!");
            return false;
        }

        _mapTypeIndex++;
        if (_mapTypeIndex == 3)
        {
            _mapTypeIndex = 0;
            _biasTypeIndex++;
            if (_biasTypeIndex == 6)
            {
                _biasTypeIndex = 0;
                _sortTypeIndex++;
            }
        }
        return true;
    }

    private void UpdateUI_MapName()
    {
        if (!mapNameText) return;
        string[] names = { "Lonely Tree", "Chinese Wall", "Boulder" };
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
}
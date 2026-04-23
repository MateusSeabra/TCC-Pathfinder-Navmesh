using System;
using System.IO;
using System.Globalization;
using UnityEngine;

public class TestLogger : MonoBehaviour
{
    [Header("File Settings")]
    [SerializeField] private string logFolderPath = "Assets/Logs/";
    [SerializeField] private string filePrefix = "Dataset_TCC_";

    private const string Separator = ",";

    public void WriteLog(int testIndex, string scenarioName, string sortMode,
                         float biasFactor, float biasCap, float voxelSize, int tileSize,
                         float avgPathLength, float cohesion, long calcTimeMs)
    {
        try
        {
            if (!Directory.Exists(logFolderPath))
            {
                Directory.CreateDirectory(logFolderPath);
            }

            string date = DateTime.Now.ToString("yyyy-MM-dd");
            string fullPath = Path.Combine(logFolderPath, $"{filePrefix}{date}.csv");

            bool isNewFile = !File.Exists(fullPath);

            string line = string.Join(Separator,
                testIndex,
                scenarioName,
                sortMode,
                biasFactor.ToString("F2", CultureInfo.InvariantCulture),
                biasCap.ToString("F2", CultureInfo.InvariantCulture),
                voxelSize.ToString("F3", CultureInfo.InvariantCulture),
                tileSize,
                avgPathLength.ToString("F2", CultureInfo.InvariantCulture),
                cohesion.ToString("F4", CultureInfo.InvariantCulture),
                calcTimeMs
            );

            using (StreamWriter writer = new StreamWriter(fullPath, true))
            {
                if (isNewFile)
                {
                    string header = "TestID,Scenario,SortMode,BiasFactor,BiasCap,VoxelSize,TileSize,AvgLength,Cohesion,TimeMs";
                    writer.WriteLine(header);
                }
                writer.WriteLine(line);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[TestLogger] Erro crítico ao salvar CSV: {e.Message}");
        }
    }
}
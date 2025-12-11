using System;
using System.IO;
using UnityEngine;

public class TestLogger : MonoBehaviour
{
    [Header("File Settings")]
    [SerializeField] private string logFolderPath = "Assets/Logs/";
    [SerializeField] private string filePrefix = "Resultados-";
    [SerializeField] private string versionSuffix = "-v3";

    private const string Separator = "; ";

    public void WriteLog(int testIndex, string scenarioName, string algorithmName,
                         float biasFactor, float biasCap, float avgPathLength, float fraternityMetric)
    {
        try
        {
            if (!Directory.Exists(logFolderPath))
            {
                Directory.CreateDirectory(logFolderPath);
                Debug.Log($"[TestLogger] Diretório criado: {logFolderPath}");
            }

            string date = DateTime.Now.ToString("yyyy-MM-dd");
            string fullPath = Path.Combine(logFolderPath, $"{filePrefix}{date}{versionSuffix}.txt");

            string line = $"{testIndex}{Separator}" +
                          $"{scenarioName}{Separator}" +
                          $"{algorithmName}{Separator}" +
                          $"{biasFactor:F2}{Separator}" +
                          $"{biasCap:F2}{Separator}" +
                          $"{avgPathLength:F2}{Separator}" +
                          $"{fraternityMetric:F4}";

            using (StreamWriter writer = new StreamWriter(fullPath, true))
            {
                writer.WriteLine(line);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[TestLogger] FALHA AO SALVAR LOG: {e.Message}");
        }
    }
}
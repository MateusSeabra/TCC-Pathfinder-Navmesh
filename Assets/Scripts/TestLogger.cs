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

            bool isNewFile = !File.Exists(fullPath);

            string line = $"{testIndex}{Separator}" +
                          $"{scenarioName}{Separator}" +
                          $"{algorithmName}{Separator}" +
                          $"{biasFactor:F2}{Separator}" +
                          $"{biasCap:F2}{Separator}" +
                          $"{avgPathLength:F2}{Separator}" +
                          $"{fraternityMetric:F4}";

            using (StreamWriter writer = new StreamWriter(fullPath, true))
            {
                if (isNewFile)
                {
                    string header = $"ID_Teste{Separator}Cenario{Separator}Ordem_Agentes{Separator}" +
                                    $"Fator_Feromonio(Bias){Separator}Limite_Feromonio(Cap){Separator}" +
                                    $"Comprimento_Medio{Separator}Fraternidade_Coesao";
                    writer.WriteLine(header);
                }

                writer.WriteLine(line);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[TestLogger] FALHA AO SALVAR LOG: {e.Message}");
        }
    }
}
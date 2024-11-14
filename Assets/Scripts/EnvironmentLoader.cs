using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class EnvironmentLoader : MonoBehaviour
{
    private static readonly Dictionary<string, string> EnvVars = new Dictionary<string, string>();

    void Awake()
    {
        LoadEnvFile();
        Debug.Log("env loader awake");
    }

    private void LoadEnvFile()
    {
        string filePath = Path.Combine(Application.dataPath, "../.env");
        
        if (File.Exists(filePath))
        {
            Debug.Log("env loader file exists");
            string[] lines = File.ReadAllLines(filePath);
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                string[] parts = line.Split('=');
                if (parts.Length == 2)
                {
                    EnvVars[parts[0].Trim()] = parts[1].Trim();
                }
            }
        }
        else
        {
            Debug.LogWarning(".env file not found");
        }
    }

    public static string GetEnvVariable(string key)
    {
        return EnvVars.TryGetValue(key, out string value) ? value : null;
    }
}
using System;
using System.IO;
using UnityEngine;

namespace ColorBlockJam.Utils
{
    // Small text files under persistentDataPath.
    public static class FileUtils
    {
        public static void SaveToFile(string filename, string text)
        {
            var path = Path.Combine(Application.persistentDataPath, filename);
            try
            {
                File.WriteAllText(path, text);
                DebugExtension.Log($"SaveToFile '{filename}' at {path}");
            }
            catch (Exception e)
            {
                Debug.LogError($"{nameof(FileUtils)}: can't save '{path}': {e.Message}");
            }
        }

        // Null when the file is missing or can't be read.
        public static string ReadFromFile(string filename)
        {
            var path = Path.Combine(Application.persistentDataPath, filename);
            try
            {
                if (File.Exists(path)) return File.ReadAllText(path);

                DebugExtension.Log("File not found: " + filename);
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError($"{nameof(FileUtils)}: can't read '{path}': {e.Message}");
                return null;
            }
        }
    }
}

using MyToolz.Utilities.Debug;
using System;

namespace MyToolz.Extensions
{
    public static class Extensions
    {
        public static float ToFloat(this double arg)
        {
            return float.Parse(arg.ToString());
        }
    }

    public static class UIUtilities
    {
        public static void PopulateList<T>(int count, Func<T> createFunction, Action<T> createdCallback)
        {
            for (int i = 0; i < count; i++)
            {
                T result;
                try
                {
                    result = createFunction();
                }
                catch (Exception e)
                {
                    continue;
                }
                if (result == null) continue;
                createdCallback(result);
            }
        }
        public static string ExtractSceneName(string scenePath)
        {
            if (string.IsNullOrEmpty(scenePath))
            {
                DebugUtility.LogError("Scene path is null or empty.");
                return string.Empty;
            }

            string fileName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            if (string.IsNullOrEmpty(fileName))
            {
                DebugUtility.LogError("Failed to extract scene name from path: " + scenePath);
                return string.Empty;
            }

            DebugUtility.Log("Extracted scene name: " + fileName);
            return fileName;
        }
    }
}
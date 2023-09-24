using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace Exanite.SceneManagement
{
    public static class SceneLoaderRegistry
    {
        private static Dictionary<Scene, SceneLoader> SceneLoadersDictionary { get; } = new Dictionary<Scene, SceneLoader>();

        public static IReadOnlyDictionary<Scene, SceneLoader> SceneLoaders => SceneLoadersDictionary;

        public static void Register(Scene scene, SceneLoader sceneLoader)
        {
            if (SceneLoadersDictionary.TryGetValue(scene, out var existingSceneLoader))
            {
                if (sceneLoader == existingSceneLoader)
                {
                    return;
                }
            }

            SceneLoadersDictionary.Add(scene, sceneLoader);
        }

        public static void Unregister(Scene scene)
        {
            SceneLoadersDictionary.Remove(scene);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Exanite.SceneManagement
{
    public static class SceneLoaderRegistry
    {
        private static Dictionary<Scene, SceneLoader> SceneLoadersDictionary { get; } = new Dictionary<Scene, SceneLoader>();

        public static IReadOnlyDictionary<Scene, SceneLoader> SceneLoaders => SceneLoadersDictionary;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            SceneLoadersDictionary.Clear();
        }

        public static void Register(Scene scene, SceneLoader sceneLoader)
        {
            SceneLoadersDictionary.Add(scene, sceneLoader);
        }

        public static void Unregister(Scene scene)
        {
            SceneLoadersDictionary.Remove(scene);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Exanite.SceneManagement
{
    public static class SceneInitializerRegistry
    {
        private static Dictionary<Scene, SceneInitializer> SceneInitializersDictionary { get; } = new Dictionary<Scene, SceneInitializer>();

        public static IReadOnlyDictionary<Scene, SceneInitializer> SceneInitializers => SceneInitializersDictionary;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            SceneInitializersDictionary.Clear();
        }

        public static void Register(Scene scene, SceneInitializer sceneInitializer)
        {
            SceneInitializersDictionary.Add(scene, sceneInitializer);
        }

        public static void Unregister(Scene scene)
        {
            SceneInitializersDictionary.Remove(scene);
        }
    }
}

using UniDi;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Exanite.SceneManagement.Editor
{
    internal static class UnityEntrypoints
    {
        [InitializeOnLoadMethod]
        public static void Initialize()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredEditMode)
            {
                for (var i = 0; i < EditorSceneManager.sceneCount; i++)
                {
                    CleanupProjectContext(EditorSceneManager.GetSceneAt(i));
                }
            }
        }

        private static void CleanupProjectContext(Scene scene)
        {
            if (Application.isPlaying)
            {
                return;
            }

            // Sometimes a ProjectContext object is left in the scene when an exception occurs during scene initialization
            // This removes it
            foreach (var rootObject in scene.GetRootGameObjects())
            {
                if (rootObject.TryGetComponent(out ProjectContext _))
                {
                    Object.DestroyImmediate(rootObject);
                    Debug.Log("Removed leftover ProjectContext. This usually happens when an exception occurs during scene initialization.");
                }
            }
        }
    }
}

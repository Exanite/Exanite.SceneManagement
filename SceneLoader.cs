using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UniDi;
using UnityEngine;

namespace Exanite.SceneManagement
{
    [DefaultExecutionOrder(-100000)]
    public class SceneLoader : MonoBehaviour
    {
        [Header("Configuration")]
        [Required] private SceneContext sceneContext;
        [SerializeField] private List<SceneLoadStage> stages = new();
        [Space]
        [SerializeField] private bool disableSceneObjectsDuringLoad = true;

        /// <summary>
        /// Stores all of the original objects in the scene while resources are being loaded.
        /// <para/>
        /// This allows the objects to be enabled/disabled in bulk, which maintains Unity's script execution order overrides.
        /// Enabling/disabling each object one by one causes the script execution order to be partially ignored.
        /// <para/>
        /// This is because enabling/disabling an object immediately causes the OnEnable/OnDisable callbacks to run.
        /// </summary>
        private Transform sceneObjectsParent;

        private void Awake()
        {
            if (disableSceneObjectsDuringLoad)
            {
                DisableSceneObjects();
            }

            LoadScene()
                .ContinueWith(() =>
                {
                    RestoreDisabledSceneObjects();
                });
        }

        private async UniTask LoadScene()
        {
            ProjectContext.Instance.EnsureIsInitialized();
            var container = ProjectContext.Instance.Container;

            foreach (var stage in stages)
            {
                await stage.Load(container);
            }
        }

        private void DisableSceneObjects()
        {
            var scene = gameObject.scene;

            using (UnityEngine.Pool.ListPool<GameObject>.Get(out var rootGameObjects))
            {
                // Get root GameObjects before creating the temporary GameObject below
                scene.GetRootGameObjects(rootGameObjects);

                sceneObjectsParent = new GameObject($"Scene Objects (Created by {GetType().Name})").transform;
                sceneObjectsParent.gameObject.SetActive(false);

                foreach (var rootGameObject in rootGameObjects)
                {
                    if (rootGameObject != gameObject)
                    {
                        rootGameObject.transform.SetParent(sceneObjectsParent, true);
                    }
                }
            }
        }

        private void RestoreDisabledSceneObjects()
        {
            if (sceneObjectsParent == null)
            {
                return;
            }

            sceneObjectsParent.gameObject.SetActive(true);
            while (sceneObjectsParent.childCount != 0)
            {
                var child = sceneObjectsParent.GetChild(sceneObjectsParent.childCount - 1);
                child.transform.SetParent(null, true);
            }

            Destroy(sceneObjectsParent.gameObject);
        }
    }
}

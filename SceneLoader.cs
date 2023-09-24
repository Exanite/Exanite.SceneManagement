using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UniDi;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Exanite.SceneManagement
{
    [DefaultExecutionOrder(-13000)]
    public class SceneLoader : MonoBehaviour
    {
        [Header("Configuration")]
        [Required]
        [SerializeField] private SceneIdentifier identifier;
        [Required]
        [SerializeField] private SceneContext sceneContext;
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

        private List<UniTask> pendingTasks = new();

        public SceneIdentifier Identifier
        {
            get => identifier;
            set => identifier = value;
        }

        public SceneContext SceneContext
        {
            get => sceneContext;
            set => sceneContext = value;
        }

        public List<SceneLoadStage> Stages => stages;

        public bool DisableSceneObjectsDuringLoad
        {
            get => disableSceneObjectsDuringLoad;
            set => disableSceneObjectsDuringLoad = value;
        }

        public bool IsLoading { get; private set; }

        private void Awake()
        {
            LoadScene().Forget();
        }

        private void OnEnable()
        {
            SceneLoaderRegistry.Register(gameObject.scene, this);
        }

        private void OnDisable()
        {
            SceneLoaderRegistry.Unregister(gameObject.scene);
        }

        public void AddPendingTask(UniTask task)
        {
            if (!IsLoading)
            {
                throw new InvalidOperationException($"Can only add pending tasks while this {GetType().Name} is loading.");
            }

            pendingTasks.Add(task);
        }

        private async UniTask LoadScene()
        {
            SceneLoaderRegistry.Register(gameObject.scene, this);

            IsLoading = true;

            if (DisableSceneObjectsDuringLoad)
            {
                DisableSceneObjects();
            }

            ProjectContext.Instance.EnsureIsInitialized();
            var container = ProjectContext.Instance.Container;

            foreach (var stage in stages)
            {
                await stage.Load(this, container);
            }

            RestoreDisabledSceneObjects();

            // Wait 3 frames (arbitrary number) for any additional load tasks added by activated scene objects
            for (var i = 0; i < 3; i++)
            {
                await UniTask.Yield();
            }

            while (pendingTasks.Count > 0)
            {
                await pendingTasks[0];

                pendingTasks.RemoveAll(task => task.Status != UniTaskStatus.Pending);
            }

            IsLoading = false;
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
                SceneManager.MoveGameObjectToScene(child.gameObject, gameObject.scene);
            }

            Destroy(sceneObjectsParent.gameObject);
        }
    }
}

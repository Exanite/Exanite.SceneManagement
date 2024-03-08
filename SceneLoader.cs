using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UniDi;
using UnityEngine;
using UnityEngine.SceneManagement;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif

namespace Exanite.SceneManagement
{
    [DefaultExecutionOrder(-13000)]
    public class SceneLoader : MonoBehaviour
    {
        [Header("Configuration")]
#if ODIN_INSPECTOR
        [Required]
#endif
        [SerializeField] private SceneIdentifier identifier;
#if ODIN_INSPECTOR
        [Required]
#endif
        [SerializeField] private SceneContext sceneContext;
        [SerializeField] private List<SceneLoadStage> stages = new();

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

        private List<SceneLoader> parentSceneLoaders = new();
        private List<SceneLoader> pendingParentSceneLoaders = new();

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

        /// <summary>
        /// Is the SceneLoader loading the scene?
        /// </summary>
        public bool IsLoading { get; private set; }

        /// <summary>
        /// Has the objects in this SceneLoader's scene been activated?
        /// <para/>
        /// This is part of the loading phase.
        /// </summary>
        public bool HasActivatedScene { get; private set; }

        private void Awake()
        {
            SceneLoaderRegistry.Register(gameObject.scene, this);

            LoadScene().Forget();
        }

        private void OnDestroy()
        {
            SceneLoaderRegistry.Register(gameObject.scene, this);
        }

        public void AddParentSceneLoader(SceneLoader parentSceneLoader)
        {
            if (HasActivatedScene)
            {
                throw new InvalidOperationException($"Can only add parent scenes before the scene has been activated.");
            }

            parentSceneLoaders.Add(parentSceneLoader);
            pendingParentSceneLoaders.Add(parentSceneLoader);
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
            IsLoading = true;
            HasActivatedScene = false;
            DisableSceneObjects();

            ProjectContext.Instance.EnsureIsInitialized();
            var container = ProjectContext.Instance.Container;

            foreach (var stage in stages)
            {
                await stage.Load(this, container);
            }

            while (pendingParentSceneLoaders.Count > 0)
            {
                pendingParentSceneLoaders.RemoveAll(parent => !parent.IsLoading);

                await UniTask.Yield();
            }

            try
            {
                await SceneLoadMonitors.Activation.AcquireLock();

                HasActivatedScene = true;

                var parentContainers = parentSceneLoaders.Select(loader => loader.SceneContext.Container).ToList();
                SceneLoadManager.SetSceneContextParameters(parentContainers.Count == 0 ? null : parentContainers);
                EnableSceneObjects();
                SceneLoadManager.CleanupSceneContextParameters();
            }
            finally
            {
                SceneLoadMonitors.Activation.ReleaseLock();
            }

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
                SceneManager.MoveGameObjectToScene(sceneObjectsParent.gameObject, gameObject.scene);

                foreach (var rootGameObject in rootGameObjects)
                {
                    if (rootGameObject != gameObject)
                    {
                        rootGameObject.transform.SetParent(sceneObjectsParent, true);
                    }
                }
            }
        }

        private void EnableSceneObjects()
        {
            if (sceneObjectsParent == null)
            {
                return;
            }

            sceneObjectsParent.gameObject.SetActive(true);
            while (sceneObjectsParent.childCount != 0)
            {
                var child = sceneObjectsParent.GetChild(0);
                child.transform.SetParent(null, true);
                SceneManager.MoveGameObjectToScene(child.gameObject, gameObject.scene);
            }

            Destroy(sceneObjectsParent.gameObject);
        }
    }
}

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
    public class SceneInitializer : MonoBehaviour
    {
        [Header("Configuration")]
#if ODIN_INSPECTOR
        [RequiredIn(PrefabKind.InstanceInScene | PrefabKind.NonPrefabInstance)]
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

        private List<SceneInitializer> parentSceneInitializers = new();
        private List<SceneInitializer> pendingParentSceneInitializers = new();

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
        /// Is the SceneInitializer loading the scene?
        /// </summary>
        public bool IsLoading { get; private set; }

        /// <summary>
        /// Has the objects in this SceneInitializer's scene been activated?
        /// <para/>
        /// This is part of the loading phase.
        /// </summary>
        public bool HasActivatedScene { get; private set; }

        private void Awake()
        {
            SceneInitializerRegistry.Register(gameObject.scene, this);

            LoadScene().Forget();
        }

        private void OnDestroy()
        {
            SceneInitializerRegistry.Unregister(gameObject.scene);
        }

        public void AddParentSceneInitializer(SceneInitializer parentSceneInitializer)
        {
            if (HasActivatedScene)
            {
                throw new InvalidOperationException($"Can only add parent scenes before the scene has been activated.");
            }

            parentSceneInitializers.Add(parentSceneInitializer);
            pendingParentSceneInitializers.Add(parentSceneInitializer);
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

            // Ensure scene loader does not have a parent
            transform.parent = null;
            transform.SetAsFirstSibling();

            // Prevent scene from activating
            DisableSceneObjects();

            // Since we disable scene objects, the scene's SceneContext will not be able to consume these parameters
            // These parameters must be saved and restored before the SceneContext activates
            var initialParentContainers = SceneContext.ParentContainers;
            var initialBindings = SceneContext.ExtraBindingsInstallMethod;

            // Initialize DI
            ProjectContext.Instance.EnsureIsInitialized();

            // Run load stages that don't require scene initialization
            foreach (var stage in stages)
            {
                if (!stage.WaitForSceneInitialization)
                {
                    await stage.Load(gameObject.scene);
                }
            }

            // Wait for parent scenes to initialize
            while (pendingParentSceneInitializers.Count > 0)
            {
                pendingParentSceneInitializers.RemoveAll(parent => !parent.IsLoading);

                await UniTask.Yield();
            }

            // Activate scene
            try
            {
                await SceneLoadMonitors.Activation.AcquireLock();

                // Wait 1 frame to prevent multiple activations in a frame
                await UniTask.Yield();

                HasActivatedScene = true;

                var parentContainers = parentSceneInitializers.Select(loader => loader.SceneContext.Container).ToList();
                SceneLoader.AddSceneParentContainers(initialParentContainers);
                SceneLoader.AddSceneBindings(initialBindings);
                SceneLoader.AddSceneParentContainers(parentContainers.Count == 0 ? null : parentContainers);
                EnableSceneObjects();
                SceneLoader.ClearSceneParameters();
            }
            finally
            {
                SceneLoadMonitors.Activation.ReleaseLock();
            }

            // Run load stages that require scene initialization
            foreach (var stage in stages)
            {
                if (stage.WaitForSceneInitialization)
                {
                    await stage.Load(gameObject.scene);
                }
            }

            // Wait 3 frames (arbitrary number)
            // This allows objects in the scene to add more load tasks if necessary
            for (var i = 0; i < 3; i++)
            {
                await UniTask.Yield();
            }

            // Wait for all pending tasks to finish
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

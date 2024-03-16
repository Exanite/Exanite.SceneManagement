using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UniDi;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Exanite.SceneManagement
{
    /// <summary>
    /// Used to load scenes and create DI container parent-child relations.
    /// <para/>
    /// Note: Only use this class to load scenes, directly loading with
    /// <see cref="SceneManager"/> will bypass UniDi bindings and
    /// container parenting.
    /// </summary>
    public class SceneLoader : MonoBehaviour
    {
        public const string ParentSceneId = "ParentScene";

        [Inject] private SceneContextRegistry sceneContextRegistry;

        public static bool IsLoading => SceneLoadMonitors.Load.HasUsers;

        public UniTask<Scene> LoadScene(SceneLoadSettings settings)
        {
            if (settings.LoadMode == LoadSceneMode.Additive)
            {
                return LoadAdditiveScene(settings.SceneName, settings.Parent, settings.LocalPhysicsMode, settings.Bindings);
            }
            else
            {
                return LoadSingleScene(settings.SceneName, settings.LocalPhysicsMode, settings.Bindings);
            }
        }

        private async UniTask<Scene> LoadAdditiveScene(
            string sceneName,
            Scene parent = default,
            LocalPhysicsMode localPhysicsMode = LocalPhysicsMode.None,
            Action<DiContainer> bindings = null)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                throw new ArgumentException($"Failed to load scene. Specified scene '{sceneName}' does not exist.", nameof(sceneName));
            }

            var parentContext = sceneContextRegistry.TryGetSceneContextForScene(parent);

            try
            {
                await SceneLoadMonitors.Load.AcquireLock();

                bindings += container =>
                {
                    if (parentContext != null)
                    {
                        container.Bind<Scene>().WithId(ParentSceneId).To<Scene>().FromInstance(parentContext.gameObject.scene);
                    }
                };

                AddSceneParentContainers(parentContext == null ? null : new[] { parentContext.Container });
                AddSceneBindings(bindings);

                var loadSceneParameters = new LoadSceneParameters(LoadSceneMode.Additive, localPhysicsMode);

                return await LoadScene(sceneName, loadSceneParameters);
            }
            finally
            {
                ClearSceneParameters();

                SceneLoadMonitors.Load.ReleaseLock();
            }
        }

        private async UniTask<Scene> LoadSingleScene(
            string sceneName,
            LocalPhysicsMode localPhysicsMode = LocalPhysicsMode.None,
            Action<DiContainer> bindings = null)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                throw new ArgumentException($"Failed to load scene. Specified scene '{sceneName}' does not exist.", nameof(sceneName));
            }

            try
            {
                await SceneLoadMonitors.Load.AcquireLock();

                AddSceneBindings(bindings);

                var loadSceneParameters = new LoadSceneParameters(LoadSceneMode.Single, localPhysicsMode);

                return await LoadScene(sceneName, loadSceneParameters);
            }
            finally
            {
                SceneLoadMonitors.Load.ReleaseLock();
                ClearSceneParameters();
            }
        }

        /// <summary>
        /// Unloads the provided <see cref="Scene"/>
        /// </summary>
        /// <param name="scene">
        /// The <see cref="Scene"/> to unload
        /// </param>
        public async UniTask UnloadScene(Scene scene)
        {
            if (SceneManager.sceneCount == 1)
            {
                throw new InvalidOperationException($"Cannot unload the last active scene '{scene.name}'.");
            }

            await SceneManager.UnloadSceneAsync(scene);
        }

        /// <summary>
        /// Adds the parent containers to the next <see cref="SceneContext"/> activated.
        /// </summary>
        public static void AddSceneParentContainers(IEnumerable<DiContainer> parentContainers)
        {
            if (parentContainers != null)
            {
                // Need to prevent duplicates and maintain ordering
                var set = new HashSet<DiContainer>();
                var list = new List<DiContainer>();

                if (SceneContext.ParentContainers != null)
                {
                    foreach (var parentContainer in SceneContext.ParentContainers)
                    {
                        if (set.Add(parentContainer))
                        {
                            list.Add(parentContainer);
                        }
                    }
                }

                foreach (var parentContainer in parentContainers)
                {
                    if (set.Add(parentContainer))
                    {
                        list.Add(parentContainer);
                    }
                }

                SceneContext.ParentContainers = list;
            }
        }

        /// <summary>
        /// Adds the bindings to the next <see cref="SceneContext"/> activated.
        /// </summary>
        public static void AddSceneBindings(Action<DiContainer> bindings)
        {
            SceneContext.ExtraBindingsInstallMethod += bindings;
        }

        /// <summary>
        /// Clears the parameters set by <see cref="AddSceneParentContainers"/> and <see cref="AddSceneBindings"/>.
        /// </summary>
        public static void ClearSceneParameters()
        {
            SceneContext.ParentContainers = null;
            SceneContext.ExtraBindingsInstallMethod = null;
        }

        private async UniTask<Scene> LoadScene(string sceneName, LoadSceneParameters loadSceneParameters)
        {
            try
            {
                await SceneLoadMonitors.Activation.AcquireLock();

                await SceneManager.LoadSceneAsync(sceneName, loadSceneParameters);

                // Wait for scene to initialize
                await UniTask.Yield();
            }
            finally
            {
                SceneLoadMonitors.Activation.ReleaseLock();
            }

            // LoadSceneAsync does not return the newly loaded scene, this is the only way to get the new scene
            return SceneManager.GetSceneAt(SceneManager.sceneCount - 1);
        }
    }
}

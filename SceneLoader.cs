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

        public static bool IsLoading => SceneLoadMonitors.Load.HasPending;

        /// <summary>
        /// Loads the <see cref="Scene"/> using the provided <see cref="Scene"/> as its parent.
        /// </summary>
        /// <param name="sceneName">
        /// The name of the <see cref="Scene"/> to load.
        /// </param>
        /// <param name="loadMode">
        /// Should the scene use additive or single loading?
        /// </param>
        /// <param name="parent">
        /// The parent of the new <see cref="Scene"/>. Ignored if <see cref="loadMode"/> is <see cref="LoadSceneMode.Single">LoadSceneMode.Single</see>.
        /// </param>
        /// <param name="localPhysicsMode">
        /// Should this scene have its own physics simulation?
        /// </param>
        /// <param name="bindings">
        /// Bindings to install to the <see cref="DiContainer"/>.
        /// </param>
        /// <returns>
        /// The newly loaded <see cref="Scene"/>
        /// </returns>
        public UniTask<Scene> LoadScene(
            string sceneName,
            LoadSceneMode loadMode,
            Scene parent = default,
            LocalPhysicsMode localPhysicsMode = LocalPhysicsMode.None,
            Action<DiContainer> bindings = null)
        {
            if (loadMode == LoadSceneMode.Additive)
            {
                return LoadAdditiveScene(sceneName, parent, localPhysicsMode, bindings);
            }
            else
            {
                return LoadSingleScene(sceneName, localPhysicsMode, bindings);
            }
        }

        /// <summary>
        /// Loads the <see cref="Scene"/> using the provided <see cref="Scene"/> as its parent.
        /// </summary>
        /// <param name="sceneName">
        /// The name of the <see cref="Scene"/> to load.
        /// </param>
        /// <param name="parent">
        /// The parent of the new <see cref="Scene"/>.
        /// </param>
        /// <param name="localPhysicsMode">
        /// Should this scene have its own physics simulation?
        /// </param>
        /// <param name="bindings">
        /// Bindings to install to the <see cref="DiContainer"/>.
        /// </param>
        /// <returns>
        /// The newly loaded <see cref="Scene"/>
        /// </returns>
        public UniTask<Scene> LoadAdditiveScene(
            string sceneName,
            Scene parent = default,
            LocalPhysicsMode localPhysicsMode = LocalPhysicsMode.None,
            Action<DiContainer> bindings = null)
        {
            var context = sceneContextRegistry.TryGetSceneContextForScene(parent);

            return LoadAdditiveScene(sceneName, context, localPhysicsMode, bindings);
        }

        /// <summary>
        /// Loads the <see cref="Scene"/> using the provided
        /// <see cref="SceneContext"/> as its parent.
        /// </summary>
        /// <param name="sceneName">
        /// The name of the <see cref="Scene"/> to load.
        /// </param>
        /// <param name="parent">
        /// The parent of the new <see cref="Scene"/>.
        /// </param>
        /// <param name="localPhysicsMode">
        /// Should this scene have its own physics simulation?
        /// </param>
        /// <param name="bindings">
        /// Bindings to install to the <see cref="DiContainer"/>.
        /// </param>
        /// <returns>
        /// The newly loaded <see cref="Scene"/>.
        /// </returns>
        public async UniTask<Scene> LoadAdditiveScene(
            string sceneName,
            SceneContext parent = null,
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

                bindings += container =>
                {
                    if (parent != null)
                    {
                        container.Bind<Scene>().WithId(ParentSceneId).To<Scene>().FromInstance(parent.gameObject.scene);
                    }
                };

                AddSceneContextParentContainers(parent == null ? null : new[] { parent.Container });
                AddSceneContextBindings(bindings);

                var loadSceneParameters = new LoadSceneParameters(LoadSceneMode.Additive, localPhysicsMode);

                return await LoadScene(sceneName, loadSceneParameters);
            }
            finally
            {
                ClearSceneContextParameters();

                SceneLoadMonitors.Load.ReleaseLock();
            }
        }

        /// <summary>
        /// Loads the <see cref="Scene"/> while unloading all other scenes.
        /// </summary>
        /// <param name="sceneName">
        /// The name of the <see cref="Scene"/> to load.
        /// </param>
        /// <param name="localPhysicsMode">
        /// Should this scene have its own physics simulation?
        /// </param>
        /// <param name="bindings">
        /// Bindings to install to the <see cref="DiContainer"/>.
        /// </param>
        /// <returns>
        /// The newly loaded <see cref="Scene"/>.
        /// </returns>
        public async UniTask<Scene> LoadSingleScene(
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

                AddSceneContextBindings(bindings);

                var loadSceneParameters = new LoadSceneParameters(LoadSceneMode.Single, localPhysicsMode);

                return await LoadScene(sceneName, loadSceneParameters);
            }
            finally
            {
                SceneLoadMonitors.Load.ReleaseLock();
                ClearSceneContextParameters();
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
        public static void AddSceneContextParentContainers(IEnumerable<DiContainer> parentContainers)
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
        public static void AddSceneContextBindings(Action<DiContainer> bindings)
        {
            SceneContext.ExtraBindingsInstallMethod += bindings;
        }

        /// <summary>
        /// Clears the parameters set by <see cref="AddSceneContextParameters"/>.
        /// </summary>
        public static void ClearSceneContextParameters()
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

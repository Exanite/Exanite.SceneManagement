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
    public class SceneLoadManager : MonoBehaviour
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
        /// <param name="parent">
        /// The parent of the new <see cref="Scene"/>. Ignored if <see cref="additive"/> is <see langword="false"/>.
        /// </param>
        /// <param name="localPhysicsMode">
        /// Should this scene have its own physics simulation?
        /// </param>
        /// <param name="bindings">
        /// Bindings to install to the <see cref="DiContainer"/>.
        /// </param>
        /// <param name="bindingsLate">
        /// Late bindings to install to the <see cref="DiContainer"/>, these
        /// are installed after all other bindings are installed.
        /// </param>
        /// <param name="additive">
        /// Should the scene use additive or single loading?
        /// </param>
        /// <returns>
        /// The newly loaded <see cref="Scene"/>
        /// </returns>
        public UniTask<Scene> LoadScene(
            string sceneName,
            Scene parent = default,
            LocalPhysicsMode localPhysicsMode = LocalPhysicsMode.None,
            Action<DiContainer> bindings = null,
            Action<DiContainer> bindingsLate = null,
            bool additive = true)
        {
            if (additive)
            {
                return LoadAdditiveScene(sceneName, parent, localPhysicsMode, bindings, bindingsLate);
            }
            else
            {
                return LoadSingleScene(sceneName, localPhysicsMode, bindings, bindingsLate);
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
        /// <param name="bindingsLate">
        /// Late bindings to install to the <see cref="DiContainer"/>, these
        /// are installed after all other bindings are installed.
        /// </param>
        /// <returns>
        /// The newly loaded <see cref="Scene"/>
        /// </returns>
        public UniTask<Scene> LoadAdditiveScene(
            string sceneName,
            Scene parent = default,
            LocalPhysicsMode localPhysicsMode = LocalPhysicsMode.None,
            Action<DiContainer> bindings = null,
            Action<DiContainer> bindingsLate = null)
        {
            var context = sceneContextRegistry.TryGetSceneContextForScene(parent);

            return LoadAdditiveScene(sceneName, context, localPhysicsMode, bindings, bindingsLate);
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
        /// <param name="bindingsLate">
        /// Late bindings to install to the <see cref="DiContainer"/>, these
        /// are installed after all other bindings are installed.
        /// </param>
        /// <returns>
        /// The newly loaded <see cref="Scene"/>.
        /// </returns>
        public async UniTask<Scene> LoadAdditiveScene(
            string sceneName,
            SceneContext parent = null,
            LocalPhysicsMode localPhysicsMode = LocalPhysicsMode.None,
            Action<DiContainer> bindings = null,
            Action<DiContainer> bindingsLate = null)
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

                SetSceneContextParameters(parent == null ? null : new[] { parent.Container }, bindings, bindingsLate);

                var loadSceneParameters = new LoadSceneParameters(LoadSceneMode.Additive, localPhysicsMode);

                return await LoadScene(sceneName, loadSceneParameters);
            }
            finally
            {
                SceneLoadMonitors.Load.ReleaseLock();
                CleanupSceneContextParameters();
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
        /// <param name="bindingsLate">
        /// Late bindings to install to the <see cref="DiContainer"/>, these
        /// are installed after all other bindings are installed.
        /// </param>
        /// <returns>
        /// The newly loaded <see cref="Scene"/>.
        /// </returns>
        public async UniTask<Scene> LoadSingleScene(
            string sceneName,
            LocalPhysicsMode localPhysicsMode = LocalPhysicsMode.None,
            Action<DiContainer> bindings = null,
            Action<DiContainer> bindingsLate = null)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                throw new ArgumentException($"Failed to load scene. Specified scene '{sceneName}' does not exist.", nameof(sceneName));
            }

            try
            {
                await SceneLoadMonitors.Load.AcquireLock();

                SetSceneContextParameters(null, bindings, bindingsLate);

                var loadSceneParameters = new LoadSceneParameters(LoadSceneMode.Single, localPhysicsMode);

                return await LoadScene(sceneName, loadSceneParameters);
            }
            finally
            {
                SceneLoadMonitors.Load.ReleaseLock();
                CleanupSceneContextParameters();
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
        /// Configures the next <see cref="SceneContext"/> activated to use the provided parent containers and bindings.
        /// </summary>
        public static void SetSceneContextParameters(IEnumerable<DiContainer> parentContainers = null, Action<DiContainer> bindings = null, Action<DiContainer> bindingsLate = null)
        {
            SceneContext.ParentContainers = parentContainers;

            SceneContext.ExtraBindingsInstallMethod = bindings;
            SceneContext.ExtraBindingsLateInstallMethod = bindingsLate;
        }

        /// <summary>
        /// Cleans up parameters set in <see cref="SetSceneContextParameters"/>.
        /// </summary>
        public static void CleanupSceneContextParameters()
        {
            SceneContext.ParentContainers = null;

            SceneContext.ExtraBindingsInstallMethod = null;
            SceneContext.ExtraBindingsLateInstallMethod = null;
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

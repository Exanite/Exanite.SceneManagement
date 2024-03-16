using System;
using UniDi;
using UnityEngine.SceneManagement;

namespace Exanite.SceneManagement
{
    public class SceneLoadSettings
    {
        /// <summary>
        /// The name of the <see cref="Scene"/> to load.
        /// </summary>
        public string SceneName { get; set; }

        /// <summary>
        /// Should the scene use additive or single loading?
        /// </summary>
        public LoadSceneMode LoadMode { get; set; }

        /// <summary>
        /// Should this scene have its own physics simulation?
        /// </summary>
        public LocalPhysicsMode LocalPhysicsMode { get; set; } = LocalPhysicsMode.None;

        /// <summary>
        /// The parent of the new <see cref="Scene"/>. Ignored if <see cref="LoadMode"/> is <see cref="LoadSceneMode.Single">LoadSceneMode.Single</see>.
        /// </summary>
        public Scene Parent { get; set; } = default;

        /// <summary>
        /// Bindings to install to the scene's <see cref="DiContainer"/>.
        /// </summary>
        public Action<DiContainer> Bindings { get; set; } = null;

        public SceneLoadSettings() {}

        public SceneLoadSettings(string sceneName, LoadSceneMode loadMode)
        {
            SceneName = sceneName;
            LoadMode = loadMode;
        }
    }
}

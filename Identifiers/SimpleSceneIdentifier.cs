using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UniDi;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

namespace Exanite.SceneManagement.Identifiers
{
    public class SimpleSceneIdentifier : SceneIdentifier
    {
        [Required] [SerializeField] private string sceneName;
        [SerializeField] private InspectorLocalPhysicsMode localPhysicsMode;

        public LocalPhysicsMode LocalPhysicsMode
        {
            get => (LocalPhysicsMode)localPhysicsMode;
            set => localPhysicsMode = (InspectorLocalPhysicsMode)value;
        }

        public override async UniTask<Scene> Load(SceneLoader sceneLoader, DiContainer container)
        {
            var sceneLoadManager = container.Resolve<SceneLoadManager>();

            var newScene = await sceneLoadManager.LoadAdditiveScene(sceneName, null, LocalPhysicsMode);
            var newSceneLoader = SceneLoaderRegistry.SceneLoaders[newScene];
            Assert.AreEqual(this, newSceneLoader.Identifier);

            return newScene;
        }

        [Serializable] [Flags]
        public enum InspectorLocalPhysicsMode
        {
            None = 0,
            Physics2D = 1,
            Physics3D = 2,
        }
    }
}

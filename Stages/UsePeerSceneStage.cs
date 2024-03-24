using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Exanite.SceneManagement.Stages
{
    /// <summary>
    /// Gets an existing parent scene or loads it if it is not loaded.
    /// <para/>
    /// Peer scenes are scenes that have to be loaded before this scene initializes.
    /// </summary>
    public class UsePeerSceneStage : SceneLoadStage
    {
        [SerializeField] private SceneIdentifier peerSceneIdentifier;

        public override async UniTask Load(Scene currentScene)
        {
            await GetOrLoadScene(currentScene);
        }

        private async UniTask<SceneInitializer> GetOrLoadScene(Scene currentScene)
        {
            // Wait for all scene load operations to complete
            await UniTask.WaitWhile(() => SceneLoader.IsLoading);

            // Check to see if there are existing scenes compatible with being a parent of this scene
            var existingScene = SceneInitializerRegistry.SceneInitializers.FirstOrDefault(pair =>
                {
                    return IsCompatibleScene(pair.Value);
                })
                .Value;

            if (existingScene != null)
            {
                return existingScene;
            }

            // Otherwise, create a new parent
            var parentScene = await peerSceneIdentifier.Load(LoadSceneMode.Additive);
            var parentSceneInitializer = SceneInitializerRegistry.SceneInitializers[parentScene];

            return parentSceneInitializer;
        }

        protected virtual bool IsCompatibleScene(SceneInitializer scene)
        {
            return scene.Identifier == peerSceneIdentifier;
        }
    }
}

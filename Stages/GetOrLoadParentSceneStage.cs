using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Exanite.SceneManagement.Stages
{
    public class GetOrLoadParentSceneStage : SceneLoadStage
    {
        [SerializeField] private SceneIdentifier parentSceneIdentifier;

        public override async UniTask Load(Scene currentScene)
        {
            var parentScene = await GetOrLoadParentScene(currentScene);
            var sceneLoader = SceneLoaderRegistry.SceneLoaders[currentScene];

            sceneLoader.AddParentSceneLoader(parentScene);
        }

        private async UniTask<SceneLoader> GetOrLoadParentScene(Scene currentScene)
        {
            // Wait for all scene load operations to complete
            await UniTask.WaitWhile(() => SceneLoadManager.IsLoading);

            // Check to see if there are existing scenes compatible with being a parent of this scene
            var existingScenes = SceneLoaderRegistry.SceneLoaders.Where(pair =>
            {
                var (_, sceneLoader) = pair;

                return sceneLoader.Identifier == parentSceneIdentifier;
            });

            foreach (var (_, candidateSceneLoader) in existingScenes)
            {
                // Todo Replace with actual condition
                if (true)
                {
                    return candidateSceneLoader;
                }
            }

            // Otherwise, create a new parent
            var parentScene = await parentSceneIdentifier.Load(currentScene);
            var parentSceneLoader = SceneLoaderRegistry.SceneLoaders[parentScene];

            return parentSceneLoader;
        }
    }
}

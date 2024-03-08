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
            var existingScene = SceneLoaderRegistry.SceneLoaders.FirstOrDefault(pair =>
                {
                    return IsCompatibleScene(pair.Value);
                })
                .Value;

            if (existingScene != null)
            {
                return existingScene;
            }

            // Otherwise, create a new parent
            var parentScene = await parentSceneIdentifier.Load();
            var parentSceneLoader = SceneLoaderRegistry.SceneLoaders[parentScene];

            return parentSceneLoader;
        }

        protected virtual bool IsCompatibleScene(SceneLoader scene)
        {
            return scene.Identifier == parentSceneIdentifier;
        }
    }
}

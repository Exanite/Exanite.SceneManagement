using System.Linq;
using Cysharp.Threading.Tasks;
using UniDi;
using UnityEngine;

namespace Exanite.SceneManagement.Stages
{
    public class GetOrLoadParentSceneStage : SceneLoadStage
    {
        [SerializeField] private SceneIdentifier parentSceneIdentifier;

        public override async UniTask Load(SceneLoader sceneLoader, DiContainer container)
        {
            var parentScene = GetOrLoadParentScene(sceneLoader, container);

            // Todo Set parent container right before activation
            // Likely needs lock to prevent race conditions
        }

        private async UniTask<SceneLoader> GetOrLoadParentScene(SceneLoader sceneLoader, DiContainer container)
        {
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
            var parentScene = await parentSceneIdentifier.Load(sceneLoader, container);
            var parentSceneLoader = SceneLoaderRegistry.SceneLoaders[parentScene];

            await UniTask.WaitWhile(() => parentSceneLoader.IsLoading);

            return parentSceneLoader;
        }
    }
}
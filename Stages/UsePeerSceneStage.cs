using System.Linq;
using Cysharp.Threading.Tasks;
using Exanite.Core.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Exanite.SceneManagement.Stages
{
    /// <summary>
    /// Defines and automatically creates a scene relation.
    /// When a scene loads, this scene load stage will attempt to look for an existing peer scene and load it if it isn't found.
    /// Different <see cref="SceneRelationType">SceneRelationTypes</see> and other options can be used to modify how the relation works.
    /// </summary>
    public class UsePeerSceneStage : SceneLoadStage
    {
        [SerializeField] protected SceneIdentifier peerSceneIdentifier;
        [SerializeField] protected SceneRelationType relationType = SceneRelationType.Peer;

        public override async UniTask Load(Scene currentScene)
        {
            switch (relationType)
            {
                case SceneRelationType.Peer:
                {
                    await GetOrLoadPeerScene(currentScene);

                    break;
                }
                case SceneRelationType.Parent:
                {
                    var parentScene = await GetOrLoadPeerScene(currentScene);
                    var sceneInitializer = SceneInitializerRegistry.SceneInitializers[currentScene];

                    sceneInitializer.AddParentSceneInitializer(parentScene);

                    break;
                }
                default: throw ExceptionUtility.NotSupportedEnumValue(relationType);
            }
        }

        private async UniTask<SceneInitializer> GetOrLoadPeerScene(Scene currentScene)
        {
            // Wait for all scene load operations to complete
            await UniTask.WaitWhile(() => SceneLoader.IsLoading);

            // Check to see if there are existing scenes compatible with being a peer of this scene
            var existingScene = SceneInitializerRegistry.SceneInitializers.FirstOrDefault(pair =>
                {
                    return IsCompatibleScene(pair.Value);
                })
                .Value;

            if (existingScene != null)
            {
                return existingScene;
            }

            // Otherwise, create a new peer
            var peerScene = await peerSceneIdentifier.Load(LoadSceneMode.Additive);
            var peerSceneInitializer = SceneInitializerRegistry.SceneInitializers[peerScene];

            return peerSceneInitializer;
        }

        protected virtual bool IsCompatibleScene(SceneInitializer scene)
        {
            return scene.Identifiers.Find(identifier => identifier == peerSceneIdentifier);
        }
    }
}

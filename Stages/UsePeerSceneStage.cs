using System;
using System.Linq;
using Cysharp.Threading.Tasks;
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

        [Space]
        [SerializeField] protected bool requireScene = true;
        [SerializeField] protected bool createSceneIfDoesntExist = true;

        public SceneIdentifier PeerSceneIdentifier
        {
            get => peerSceneIdentifier;
            set => peerSceneIdentifier = value;
        }

        public SceneRelationType RelationType
        {
            get => relationType;
            set => relationType = value;
        }

        public bool RequireScene
        {
            get => requireScene;
            set => requireScene = value;
        }

        public bool CreateSceneIfDoesntExist
        {
            get => createSceneIfDoesntExist;
            set => createSceneIfDoesntExist = value;
        }

        public override async UniTask Load(Scene currentScene)
        {
            var currentSceneInitializer = SceneInitializerRegistry.SceneInitializers[currentScene];

            SceneInitializer peerSceneInitializer;
            var existingPeerScene = await TryGetExistingPeerScene(currentSceneInitializer);
            if (existingPeerScene.Exists)
            {
                peerSceneInitializer = existingPeerScene.Initializer;
            }
            else
            {
                if (createSceneIfDoesntExist)
                {
                    peerSceneInitializer = await LoadPeerScene(currentSceneInitializer);
                }
                else
                {
                    if (requireScene)
                    {
                        throw new Exception("Failed to find existing scene that is suitable to be this scene's peer scene");
                    }
                    else
                    {
                        return;
                    }
                }
            }

            switch (relationType)
            {
                case SceneRelationType.Parent:
                {
                    currentSceneInitializer.AddParentSceneInitializer(peerSceneInitializer);

                    break;
                }
            }
        }

        protected virtual async UniTask<(bool Exists, SceneInitializer Initializer)> TryGetExistingPeerScene(SceneInitializer currentScene)
        {
            // Wait for all scene load operations to complete
            await UniTask.WaitWhile(() => SceneLoader.IsLoading);

            // Check if there are existing scenes compatible with being a peer of this scene
            var existingScene = SceneInitializerRegistry.SceneInitializers.FirstOrDefault(pair =>
                {
                    return IsCompatiblePeerScene(pair.Value, currentScene);
                })
                .Value;

            return (existingScene != null, existingScene);
        }

        protected virtual async UniTask<SceneInitializer> LoadPeerScene(SceneInitializer currentScene)
        {
            var peerScene = await peerSceneIdentifier.Load(LoadSceneMode.Additive);
            var peerSceneInitializer = SceneInitializerRegistry.SceneInitializers[peerScene];

            return peerSceneInitializer;
        }

        protected virtual bool IsCompatiblePeerScene(SceneInitializer peerScene, SceneInitializer currentScene)
        {
            return peerScene != currentScene && peerScene.Identifiers.Find(identifier => identifier == peerSceneIdentifier);
        }
    }
}

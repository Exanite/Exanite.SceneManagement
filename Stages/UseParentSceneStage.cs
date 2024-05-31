using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace Exanite.SceneManagement.Stages
{
    /// <summary>
    /// Gets an existing parent scene or loads it if it is not loaded.
    /// <para/>
    /// Parent scenes are scenes that have to be loaded before this scene initializes.
    /// This scene can then access DI bindings registered in the parent scene.
    /// </summary>
    public class UseParentSceneStage : UsePeerSceneStage
    {
        protected override async UniTask<SceneInitializer> LoadInternal(Scene currentScene)
        {
            var parentScene = await base.LoadInternal(currentScene);
            var sceneInitializer = SceneInitializerRegistry.SceneInitializers[currentScene];

            sceneInitializer.AddParentSceneInitializer(parentScene);

            return parentScene;
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();

            relationType = SceneRelationType.Parent;
        }
    }
}

using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Exanite.SceneManagement
{
    public abstract class SceneLoadStage : MonoBehaviour
    {
        /// <summary>
        /// Should the <see cref="SceneLoadStage"/> wait for Scene Initialization before being ran?
        /// <para/>
        /// This allows access to the scene's objects and dependencies that comes from dependency injection.
        /// </summary>
        public virtual bool WaitForSceneInitialization => false;

        public abstract UniTask Load(Scene currentScene);
    }
}

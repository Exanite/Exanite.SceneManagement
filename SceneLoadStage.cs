using Cysharp.Threading.Tasks;
using UniDi;
using UnityEngine;

namespace Exanite.SceneManagement
{
    public abstract class SceneLoadStage : MonoBehaviour
    {
        public abstract UniTask Load(SceneLoader sceneLoader, DiContainer container);
    }
}

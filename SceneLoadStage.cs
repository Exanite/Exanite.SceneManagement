using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Exanite.SceneManagement
{
    public abstract class SceneLoadStage : MonoBehaviour
    {
        public abstract UniTask Load(Scene currentScene);
    }
}

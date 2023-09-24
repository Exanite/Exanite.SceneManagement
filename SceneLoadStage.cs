using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Exanite.SceneManagement
{
    public abstract class SceneLoadStage : MonoBehaviour
    {
        public abstract UniTask Load();
    }
}

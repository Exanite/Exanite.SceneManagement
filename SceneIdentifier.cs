using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Exanite.SceneManagement
{
    public abstract class SceneIdentifier : ScriptableObject
    {
        public abstract UniTask<Scene> Load(Scene currentScene, bool isAdditive = true);
    }
}

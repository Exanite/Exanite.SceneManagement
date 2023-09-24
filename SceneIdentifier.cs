using Cysharp.Threading.Tasks;
using UniDi;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Exanite.SceneManagement
{
    public abstract class SceneIdentifier : ScriptableObject
    {
        public abstract UniTask<Scene> Load(SceneLoader sceneLoader, DiContainer container);
    }
}

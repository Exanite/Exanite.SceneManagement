using System;
using Cysharp.Threading.Tasks;
using UniDi;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Exanite.SceneManagement
{
    public abstract class SceneIdentifier : ScriptableObject
    {
        public UniTask<Scene> Load(LoadSceneMode loadMode)
        {
            return Load(loadMode, null);
        }

        public abstract UniTask<Scene> Load(LoadSceneMode loadMode, Action<SceneLoadSettings> configureSettings);

        protected UniTask<Scene> LoadScene(SceneLoadSettings settings)
        {
            return ProjectContext.Instance.Container.Resolve<SceneLoader>().LoadScene(settings);
        }
    }
}

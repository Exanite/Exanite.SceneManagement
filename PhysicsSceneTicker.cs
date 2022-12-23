using UniDi;
using UnityEngine;

namespace Exanite.SceneManagement
{
    [DefaultExecutionOrder(-50)]
    public class PhysicsSceneTicker : MonoBehaviour
    {
        [Inject]
        private PhysicsScene physicsScene;

        private void FixedUpdate()
        {
            physicsScene.Simulate(Time.fixedDeltaTime);
        }
    }
}

using System;
using Cysharp.Threading.Tasks;

namespace Exanite.SceneManagement
{
    public class UniTaskMonitor
    {
        private int userCount;
        private bool isLocked;

        public int PendingCount
        {
            get => userCount;
            set => userCount = value;
        }

        public bool HasUsers => userCount != 0;

        public async UniTask AcquireLock()
        {
            userCount++;
            await UniTask.WaitWhile(() => isLocked);
            isLocked = true;
        }

        public void ReleaseLock()
        {
            if (!isLocked)
            {
                throw new InvalidOperationException("Cannot release lock because it is not locked.");
            }

            isLocked = false;
            userCount--;
        }
    }
}

using System;
using Cysharp.Threading.Tasks;

namespace Exanite.SceneManagement
{
    public class UniTaskMonitor
    {
        private int pendingCount;
        private bool isLocked;

        public int PendingCount
        {
            get => pendingCount;
            set => pendingCount = value;
        }

        public bool HasPending => pendingCount != 0;

        public async UniTask AcquireLock()
        {
            pendingCount++;
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
            pendingCount--;
        }
    }
}

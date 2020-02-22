using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace XavierLab
{
    public class MonoBehaviourTimer : BaseTimer
    {
        public event OnTick OnTickEvent;

        public override void SetTimeout(float duration, Action<BaseTimer> cb = null)
        {
            DoTimeout(duration, cb);
        }


        protected async void DoTimeout(float duration, Action<BaseTimer> cb = null)
        {
            runtime = Time.time;
            currentDelay = duration;
            isRunning = true;
            cts = new CancellationTokenSource();

            while (!cts.IsCancellationRequested)
            {
                if (!isPaused)
                {
                    if (GetTimeSinceStart() >= currentDelay) StopTimer();

                    MainDispatcher.Instance.Invoke(() => 
                    { 
                        OnTickEvent?.Invoke(GetTimeLeft());
                    });

                    await Task.Delay(5);
                }
                else if (!cts.IsCancellationRequested && isPaused)
                {
                    await Task.Delay(5);
                }
                else if (!cts.IsCancellationRequested && isPaused) StopTimer();
            }

            isRunning = false;
            if (!isUnloaded)
            {
                MainDispatcher.Instance.Invoke(() =>
                {
                    cb?.Invoke(this);
                });

                if (loop)
                {
                    loopCounter++;
                    if (LoopCounter < loopCount || loopCount.Equals(0)) SetTimeout(duration, cb);
                }
                else Unload();
            }
        }
    }
}

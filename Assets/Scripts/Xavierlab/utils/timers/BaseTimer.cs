using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Callback = System.Action;

public delegate void OnTick(float timeLeft);

namespace XavierLab
{
    public abstract class BaseTimer
    {
        public bool isRunning = false;
        public bool loop = false;
        public int loopCount = 0;
        public bool IsPaused { get => isPaused; set => isPaused = value; }
        

        protected float runtime;
        protected bool isUnloaded = false;
        protected float currentDelay;
        protected int loopCounter = 0;
        public int LoopCounter { get => loopCounter; }

        protected CancellationTokenSource cts = new CancellationTokenSource();
        protected bool isPaused = false;

        public virtual void StopTimer()
        {
            cts.Cancel();
        }

        public virtual void Unload()
        {
            isUnloaded = true;
            StopTimer();
        }

        protected float pauseTime;
        public virtual void PauseTimer()
        {
            isPaused = true;
            pauseTime = Time.time;
        }

        public virtual void ResumeTimer()
        {
            runtime += Time.time - pauseTime;
            isPaused = false;
        }

        /// <summary>
        /// creates a timer that lasts [duration] represented as a float value
        /// </summary>
        /// <param name="duration">float.  ie: 0.5f = 500ms</param>
        /// <param name="cb">generic call back method</param>
        public abstract void SetTimeout(float duration, Action<BaseTimer> cb = null);


        public float GetTimeSinceStart()
        {
            return Time.time - runtime;
        }

        public float GetTimeLeft()
        {
            return currentDelay - (Time.time - runtime);
        }               
    }
}

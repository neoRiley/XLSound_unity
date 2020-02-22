using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace XavierLab
{
    public class Timers
    {
        static Dictionary<string, TimerFactory> _factories;

        [RuntimeInitializeOnLoadMethod]
        public static void Initialize()
        {
            _factories = new Dictionary<string, TimerFactory>();

            foreach (TimerTypes timerType in Enum.GetValues(typeof(TimerTypes)))
            {
                var classRef = $"XavierLab.{Enum.GetName(typeof(TimerTypes), timerType)}Factory";
                var key = $"XavierLab.{Enum.GetName(typeof(TimerTypes), timerType)}Timer";
                
                var factory = (TimerFactory)Activator.CreateInstance(Type.GetType(classRef));
                if (factory != null)
                {
                    _factories.Add(key, factory);
                }
            }
        }


        public static T SetTimeout<T>(float duration, Action<BaseTimer> cb, bool loop=false, int loopCount=0)
        {
            var timer = Create<T>();
            (timer as BaseTimer).loop = loop;
            (timer as BaseTimer).loopCount = loopCount;
            (timer as BaseTimer).SetTimeout(duration, cb);
            return (T)(object)timer;
        }


        public static AsyncTimer AsyncSetTimeout(float duration, Action<BaseTimer> cb, bool loop = false, int loopCount = 0)
        {
            var timer = Create<AsyncTimer>();
            timer.loop = loop;
            timer.loopCount = loopCount;
            timer.SetTimeout(duration, cb);
            return timer;
        }


        public static MonoBehaviourTimer SetTimeout(float duration, Action<BaseTimer> cb, bool loop = false, int loopCount = 0)
        {
            var timer = Create<MonoBehaviourTimer>();
            timer.loop = loop;
            timer.loopCount = loopCount;
            timer.SetTimeout(duration, cb);
            return timer;
        }


        public static T Create<T>()
        {
            string key = typeof(T).ToString();
            var factory = _factories[key].Create();
            return (T)(object)factory;
        }
    }
}

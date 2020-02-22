using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Callback = System.Action;
using System.Runtime.CompilerServices;
using XavierLab;

public class TimerUtils : MonoBehaviour
{	
	public static TimerObject SetTimeout (float delay, Callback cb, bool loop = false, [CallerMemberName] string callerName = "", [CallerFilePath] string memberName = "")
	{
        GameObject t = new GameObject($"[{L.GetClassName(memberName)}.{callerName}] TimerObject: " + delay.ToString() + " seconds (" + Random.Range(1,200).ToString() + ")");
		TimerObject timer = t.AddComponent<TimerObject>();
        timer.loop = loop;
		timer.SetTimeout(delay, cb);
		return timer;
	}
	
	public static TimerObject SetGUITimer(Callback cb)
	{
		GameObject t = new GameObject("TimerObject GUI Timer");
		TimerObject timer = t.AddComponent<TimerObject>();
		timer.SetGUITimer(cb);
		return timer;
	}
}
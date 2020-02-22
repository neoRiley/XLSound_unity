using System;
using UnityEngine;
using System.Collections;
using Callback = System.Action;

public class TimerObject : MonoBehaviour
{
    public bool isRunning = false;
    public bool loop = false;

    public event OnTick OnTickEvent;

    private bool useGUI = false;
	private bool canRun = true;
	private bool isPaused = false;
    public bool IsPaused { get => isPaused; set => isPaused = value; }

    public void StopTimer()
	{
		canRun = false;
	}
	
	public void Unload()
	{
		canRun = false;
		useGUI = false;

        try
        {
            if (gameObject != null) Destroy(gameObject);
        }
        catch(Exception err)
        {
            // harmless - the gameobject has already been deleted, but it fails to see that with the null check for some reason.  So we catch.
            //Debug.LogError("Error caught in timer: " + err.Message);
        }
	}
	
	private float pauseTime;
	public void PauseTimer()
	{
		isPaused = true;
		pauseTime = Time.time;
	}
	
	public void ResumeTimer()
	{
		runtime += Time.time - pauseTime;
		isPaused = false;
	}

	public void SetGUITimer(Callback _guiCB)
	{
		guiCB = _guiCB;
		useGUI = true;
	}
	
	public void SetTimeout(float delay, Callback cb)
	{
		StartCoroutine( DoTimeout(delay, cb) );
	}
	
	private float runtime;
    private float currentDelay;
	private IEnumerator DoTimeout(float delay, Callback cb)
	{
		runtime = Time.time;
        currentDelay = delay;
        isRunning = true;
		while( canRun )
		{
			if( !isPaused )
			{					
				if(GetTimeSinceStart() >= currentDelay ) break;
                OnTickEvent?.Invoke(GetTimeLeft());
                yield return new WaitForSeconds(.005f);
			}
			else if( canRun && isPaused ) 
			{
				yield return new WaitForSeconds(.005f);
			}
			else if( !canRun && isPaused ) break;
		}

        isRunning = false;
		if( canRun ) cb();

        try
        {
            if (gameObject != null && loop && cb != null) SetTimeout(delay, cb);
            else Unload();
        }
        catch(Exception err)
        {
            // harmless
        }
        
	}

    public float GetTimeSinceStart()
    {
        return Time.time - runtime;
    }

    public float GetTimeLeft()
    {
        return currentDelay - (Time.time - runtime);
    }

    private Callback guiCB;

    public void OnGUI()
	{
		if( !useGUI || !canRun ) return;
		
		if( guiCB != null ) guiCB();
	}
}


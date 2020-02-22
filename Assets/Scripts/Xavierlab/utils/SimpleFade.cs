using UnityEngine;
using System.Collections;
using XavierLab;
using System;
using Pixelplacement;
using Pixelplacement.TweenSystem;

public delegate void OnUpdate(float value);

[RequireComponent (typeof(CanvasGroup))]
public class SimpleFade : MonoBehaviour
{
    public float fadeTimeIn = 1.0f;
    public float fadeTimeOut = 0.25f;
    public bool hideOnStart = false;
    public bool overrideAlphaCheck = false;

    public Action OnTransitionInCompleteEvent;
    public Action OnTransitionOutCompleteEvent;

    public event OnUpdate OnUpdateEvent;

    public CanvasGroup canvas;

    TweenBase currentTween;

    public bool IsShowing
    {
        get
        {
            return canvas.alpha > 0.0f;
        }
    }

    void Awake()
    {
        canvas = GetComponent<CanvasGroup>();
        if (hideOnStart) ZeroOut();
    }


    protected virtual void Start()
    {
        
    }


    public virtual void ZeroOut()
    {
        canvas.interactable = false;
        canvas.blocksRaycasts = false;
        canvas.alpha = 0.0f;
        if (currentTween != null) currentTween.Stop();
    }


    public virtual void AllOn()
    {
        canvas.interactable = true;
        canvas.blocksRaycasts = true;
        canvas.alpha = 1.0f;
        if (currentTween != null) currentTween.Stop();
    }


    public virtual void Show(bool show)
    {
        if (canvas != null)
        {
            canvas.interactable = show;
            canvas.blocksRaycasts = show;

            if (canvas.alpha.Equals(0.0f) && !show && !overrideAlphaCheck) return;
            if (!canvas.alpha.Equals(0.0f) && show && !overrideAlphaCheck) return;
        }
        

        float to = show ? 1 : 0;        
        float from = !show ? 1 : 0;
        if (overrideAlphaCheck) from = canvas.alpha;
        
        string func = show ? "HandleTransitionInComplete" : "HandleTransitionOutComplete";
        float fadeTime = show ? fadeTimeIn : fadeTimeOut;

        if( currentTween != null ) currentTween.Stop();
        currentTween = Tween.Value
        (
            from, 
            to, 
            (Action<float>)(x =>
            {
                OnUpdate((float)x);
            }), 
            fadeTime, 0, null, Tween.LoopType.None, null,
            ()=>
            {
                if (show) HandleTransitionInComplete();
                else HandleTransitionOutComplete();
            }
        );
    }


    protected virtual void OnUpdate(float value)
    {
        if (canvas != null)
        {
            canvas.alpha = value;
            OnUpdateEvent?.Invoke(value);
        }
    }


    protected virtual void HandleTransitionInComplete()
    {
        OnTransitionInCompleteEvent?.Invoke();
    }


    protected virtual void HandleTransitionOutComplete()
    {
        OnTransitionOutCompleteEvent?.Invoke();
    }
}

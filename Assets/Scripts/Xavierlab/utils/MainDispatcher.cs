using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;



public class MainDispatcher : MonoBehaviour
{
    static GameObject container;
    static MainDispatcher instance;
    public static MainDispatcher Instance
    {
        get => instance;
    }


    public static void Initialize()
    {
        if (container == null)
        {
            container = new GameObject("MainDispatcher");
            instance = container.AddComponent<MainDispatcher>();
        }
    }


    private List<Action> pending = new List<Action>();


    public void Invoke(Action fn)
    {
        lock (this.pending)
        {
            this.pending.Add(fn);
        }
    }

    private void InvokePending()
    {
        lock (this.pending)
        {
            foreach (Action action in this.pending)
            {
                action();
            }

            this.pending.Clear();
        }
    }

    private void Update()
    {
        this.InvokePending();
    }
}

#if UNITY_EDITOR

[InitializeOnLoad]
public class MainEditorDispatcher : ScriptableObject
{
    static MainEditorDispatcher()
    {
        EditorApplication.update += () =>
        {
            InvokePending();
        };
    }

    static List<Action> pending = new List<Action>();

    public static void Invoke(Action fn)
    {
        lock (pending)
        {
            pending.Add(fn);
        }
    }

    private static void InvokePending()
    {
        lock (pending)
        {
            foreach (Action action in pending)
            {
                action();
            }

            pending.Clear();
        }
    }
}

#endif
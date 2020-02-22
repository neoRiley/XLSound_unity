using UnityEngine;
using System.Collections;

public class MonoBehaviourSingleton<Instance> : MonoBehaviour where Instance : MonoBehaviourSingleton<Instance>
{
    public static Instance instance;
    public bool isPersistant = false;

    public virtual void Awake()
    {
        if (instance == null)
        {
            instance = this as Instance;
            if (isPersistant)
            {
                // force top-level GameObject otherwise DontDestroyOnLoad won't work
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);
            }
        }
    }
}
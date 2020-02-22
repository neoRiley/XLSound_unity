using System;
using UnityEngine;

public class BaseClickHandler : MonoBehaviour
{
    public Action<GameObject> OnTap;
    public virtual void OnMouseUpAsButton()
    {
        Debug.Log("0");
        OnTap?.Invoke(gameObject);
    }
}


using System;
using UnityEngine;

public class BaseClickHandler : MonoBehaviour
{
    public Action<GameObject> OnTap;
    public virtual void OnMouseUpAsButton()
    {
        OnTap?.Invoke(gameObject);
    }
}


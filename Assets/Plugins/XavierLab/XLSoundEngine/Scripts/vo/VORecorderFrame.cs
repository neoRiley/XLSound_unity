using UnityEngine;
using System.Collections;
using System;

namespace XavierLab
{
    [Serializable]
    public class VORecorderFrame
    {
        public VOPositions position;
        public int frameTime; // time after start where the event happens
        [HideInInspector]
        public int span; // time between last vo and this one
        [HideInInspector]
        public Texture texture;
    }
}
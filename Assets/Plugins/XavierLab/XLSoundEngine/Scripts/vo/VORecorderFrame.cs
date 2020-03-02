using UnityEngine;
using System.Collections;
using System;

namespace XavierLab
{
    [Serializable]
    public class VORecorderFrame
    {
        public VOPositions position;
        public float frameTime; // time after start where the event happens
        public float duration; // duration of how long to hold image.  likely used for the last image
        [HideInInspector]
        public float span; // time between last vo and this one
        [HideInInspector]
        public Texture texture;
    }
}
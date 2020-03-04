using UnityEngine;
using System.Collections;
using System;
using UnityEditor;

namespace XavierLab
{
    [Serializable]
    public class VORecorderFrame
    {
        [SerializeField]
        int id = -1;
        public int ID
        {
            get
            {
                if (id.Equals(-1)) id = VORecorderInspector.GetNextVOFrameID();
                return id;
            }
        }

        [HideInInspector]
        [SerializeField]
        public VOPositions position;

        [HideInInspector]
        [SerializeField]
        public int frameTime; // time after start where the event happens

        [HideInInspector]
        public int span; // time between last vo and this one

        [HideInInspector]
        public Texture texture;
    }
}
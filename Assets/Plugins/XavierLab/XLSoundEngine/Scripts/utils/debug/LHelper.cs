using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace XavierLab
{
    /// <summary>
    /// LHelper is meant to give someone an interface to change color values for the various log event types.
    /// Just create a GameObject and add to it in the scene.
    /// Otherwise, L.Log will work with default color values without any editing or objects being added to the scene
    /// </summary>
    [ExecuteInEditMode]
    public class LHelper : MonoBehaviour
    {
        [SerializeField]
        public LogTypes[] logTypes;

        public void Awake()
        {
            if (logTypes == null || logTypes.Length < 1)
            {
                // This is the first load, we need to create default values for working with.
                logTypes = L.FillLogTypesWithDefaults();
            }

            L.LogTypesList = logTypes;
        }
    }
}
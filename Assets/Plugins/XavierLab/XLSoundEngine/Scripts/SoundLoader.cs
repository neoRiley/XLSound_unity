﻿using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace XavierLab
{
    public class SoundLoader : MonoBehaviour
    {
        public bool removeOnUnload = true;
        public List<Sounds> soundList;

        public virtual void Awake()
        {
            if (isActiveAndEnabled && soundList.Count > 0) XLSound.LoadSoundsForScene(soundList);
        }

        protected virtual void Start()
        {

        }

        public virtual void OnDestroy()
        {
            if (removeOnUnload && soundList.Count > 0) XLSound.RemoveSoundsForScene(soundList);
        }
    }
}
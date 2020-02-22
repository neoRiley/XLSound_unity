using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace XavierLab
{
    public class SceneSounds : MonoBehaviour
    {
        public List<Sounds> soundList;

        public void Awake()
        {
            if(isActiveAndEnabled && soundList.Count > 0) XLSound.LoadSoundsForScene(soundList, LoadSceneMode.Additive);
        }

        public void OnDisable()
        {
            if (soundList.Count > 0) XLSound.RemoveSoundsForScene(soundList);
        }
    }
}

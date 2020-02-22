using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using XavierLab;

public class RobotLabMain : MonoBehaviour
{
    public void Start()
    {
        CreateWallLights();
        CreateWindowTaps();
        CreateTurbineTaps();

        TestMusic();
        //XLSound.PlaySoundWithSnapshot(Sounds.My_Song_6_12, 10.0f);
    }


    protected void CreateWallLights()
    {
        GameObject[] lights = GameObject.FindGameObjectsWithTag("WallLight");

        foreach (GameObject g in lights)
        {
            g.AddComponent<BoxCollider>();
            var handler = g.AddComponent<WallLightClickHandler>();
            handler.OnTap += (GameObject obj) => {
                XLSound.PlaySoundAtPosition(Sounds.plastic_tap_0_23, obj.transform.position);
            };
        }
    }

    protected void CreateWindowTaps()
    {
        GameObject[] windows = GameObject.FindGameObjectsWithTag("Window");

        foreach (GameObject g in windows)
        {
            g.AddComponent<BoxCollider>();
            var handler = g.AddComponent<WindowClickHandler>();
            handler.OnTap += (GameObject obj) => {
                XLSound.PlaySoundAtPosition(Sounds.window_tap_0_25, obj.transform.position);
            };
        }
    }

    protected void CreateTurbineTaps()
    {
        GameObject t = GameObject.FindGameObjectWithTag("Turbine_Left");
        var collider = t.AddComponent<SphereCollider>();
        collider.center = Vector3.zero;
        var lHandler = t.AddComponent<TurbineClickHandler>();
        lHandler.OnTap += (GameObject obj) => {
            XLSound.PlaySoundAtPosition(Sounds.metal_bang_3_24, obj.transform.position);
        };

        t = GameObject.FindGameObjectWithTag("Turbine_Right");
        collider = t.AddComponent<SphereCollider>();
        collider.center = Vector3.zero;
        var rHandler = t.AddComponent<TurbineClickHandler>();
        rHandler.OnTap += (GameObject obj) => {
            XLSound.PlaySoundAtPosition(Sounds.metal_bang_3_24, obj.transform.position);
        };
    }

    public void Update()
    {
        if (Input.GetKeyUp(KeyCode.R))
        {
            //SoundCanvasEngine.Instance.PlaySound(Sounds.GAME_START_SFX_CLICKSOUND_2D);
            RotationCamera.instance.SetTarget(transform.position);
        }
    }


    void TestMuting()
    {
        Timers.AsyncSetTimeout(5.0f, (x) =>
        {
            XLSound.Mute();

            Timers.AsyncSetTimeout(10.0f, (xx) =>
            {
                XLSound.UnMute();
            });
        });
    }

    void TestMusic()
    {
        int musicIndex = 1;
        
        XLSound.PlaySoundWithSnapshot(Sounds.My_Song_6_12, 5.0f);

        Timers.AsyncSetTimeout(30.0f, (x) =>
        {
            L.Log(LogEventType.EVENT, $"Should switch Music: {musicIndex}");
            if( musicIndex.Equals(0))
                XLSound.PlaySoundWithSnapshot(Sounds.My_Song_6_12, 7.0f);
            else if (musicIndex.Equals(1))
                XLSound.PlaySoundWithSnapshot(Sounds.XWingsAttack_complete_master_15, 7.0f);
            else if (musicIndex.Equals(2))
                XLSound.PlaySoundWithSnapshot(Sounds.ExhaustPort_complete_master_22, 7.0f);

            musicIndex++;
            musicIndex = musicIndex > 2 ? 0 : musicIndex;
        }, true);
    }


    void LoadTestScene()
    {
        Timers.AsyncSetTimeout(5.0f, (x) =>
        {
            SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Additive);

            Timers.AsyncSetTimeout(5.0f, (xx) =>
            {
                SceneManager.UnloadSceneAsync("SampleScene");
            });
        });
    }
}


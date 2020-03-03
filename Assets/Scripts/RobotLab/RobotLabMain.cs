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
        //CreateVOTap();
        CreateWallLights();
        CreateWindowTaps();
        CreateTurbineTaps();

        TestMusic();
        //XLSound.PlaySoundWithTag("WallLight");

        //Timers.AsyncSetTimeout(5.0f, (x) =>
        //{
        //    XLSound.StopSoundWithTag("WallLight");
        //});
    }


    protected void CreateVOTap()
    {
        GameObject t = GameObject.FindGameObjectWithTag("BlastDoor");
        var collider = t.AddComponent<SphereCollider>();
        collider.center = Vector3.zero;
        var lHandler = t.AddComponent<TurbineClickHandler>();
        lHandler.OnTap += (GameObject obj) =>
        {
            XLSound.PlayVOSound(Sounds.AttractVO_heyDoIKnowYouFromSomewhere_v1, (VOPositions pos) =>
            {
                L.Log(LogEventType.EVENT, $"VO event: {pos}");
            }, () =>
            {
                L.Log(LogEventType.STRING, $"VO Completed");
            });
        };

        
    }


    protected void CreateWallLights()
    {
        GameObject[] lights = GameObject.FindGameObjectsWithTag("WallLight");

        foreach (GameObject g in lights)
        {
            g.AddComponent<BoxCollider>();
            var handler = g.AddComponent<WallLightClickHandler>();
            handler.OnTap += (GameObject obj) => 
            {
                XLSound.PlaySound(obj);
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
            handler.OnTap += (GameObject obj) => 
            {
                var box = obj.GetComponent<BoxCollider>();
                XLSound.PlaySound(Sounds.window_tap_0, box.center);
            };
        }
    }

    protected void CreateTurbineTaps()
    {
        GameObject t = GameObject.FindGameObjectWithTag("Turbine_Left");
        var collider = t.AddComponent<SphereCollider>();
        collider.center = Vector3.zero;
        var lHandler = t.AddComponent<TurbineClickHandler>();
        lHandler.OnTap += (GameObject obj) => 
        {
            XLSound.PlaySound(Sounds.metal_bang_3, obj.transform.position);
        };

        t = GameObject.FindGameObjectWithTag("Turbine_Right");
        collider = t.AddComponent<SphereCollider>();
        collider.center = Vector3.zero;
        var rHandler = t.AddComponent<TurbineClickHandler>();
        rHandler.OnTap += (GameObject obj) => 
        {
            XLSound.PlaySound(Sounds.metal_bang_3, obj.transform.position);
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
        
        XLSound.PlaySound(Sounds.My_Song_6, 5.0f);

        Timers.AsyncSetTimeout(40.0f, (Action<BaseTimer>)((x) =>
        {
            L.Log(LogEventType.EVENT, $"Should switch Music: {musicIndex}");
            if( musicIndex.Equals(0))
                XLSound.PlaySound((Sounds)Sounds.My_Song_6, 7.0f);
            else if (musicIndex.Equals(1))
                XLSound.PlaySound((Sounds)Sounds.XWingsAttack_complete_master, 7.0f);
            else if (musicIndex.Equals(2))
                XLSound.PlaySound((Sounds)Sounds.ExhaustPort_complete_master, 7.0f);

            musicIndex++;
            musicIndex = musicIndex > 2 ? 0 : musicIndex;
        }), true);
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


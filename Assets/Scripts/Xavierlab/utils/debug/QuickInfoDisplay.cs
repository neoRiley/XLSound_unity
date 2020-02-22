using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class QuickInfoDisplay : MonoBehaviourSingleton<QuickInfoDisplay>
{
    public GUIStyle guiStyle;
    public int top = 50;
    public int left = 10;
    public int boxWidth = 300;
    public int lineHeight = 20;

    public bool canShow = false;

    float deltaTime = 0.0f;

    string[] messages = new string[100];

    string fps = "";
    public string FPS
    {
        get
        {
            return fps;
        }
        set
        {
            fps = value;
        }
    }


    public void AddMessage(string msg, int lineNumber)
    {
        messages[lineNumber] = msg;
    }


    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        UpdateFPS();
    }


    void UpdateFPS()
    {
        float msec = deltaTime * 1000.0f;
        float fps = 1.0f / deltaTime;
        FPS = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
        AddMessage(FPS, 0);
    }
    

    void OnGUI()
    {
        if (!canShow) return;
        int fontSize = Screen.height * 2 / 100;
        int w = Mathf.FloorToInt(Screen.width * ( (float)boxWidth / 1920));
        int h = Mathf.FloorToInt(Screen.height * ((float)(messages.Length * lineHeight) / 1080));

        h = Mathf.Max(h, lineHeight);
        
        Rect rect = new Rect(left, top, w, h);
        guiStyle.fontSize = fontSize;

        string str = "";
        foreach(string s in messages) str += s + "\n";
        
        GUI.Label(rect, str, guiStyle);  
    }
}

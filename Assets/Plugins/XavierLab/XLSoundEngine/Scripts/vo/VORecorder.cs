﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using System.Threading;
using System.Reflection;

namespace XavierLab
{
    public class VORecorder : MonoBehaviour
    {
        [SerializeField]
        [HideInInspector]
        public int clipLength = 0;

        [SerializeField]
        public List<VORecorderFrame> frames;

        [SerializeField]
        [HideInInspector]
        public int finalFrameDelay = 100;
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(VORecorder))]
    [CanEditMultipleObjects]
    public class VORecorderInspector : Editor
    {
        public static int GetNextVOFrameID()
        {
            int id = 0;
            if (PlayerPrefs.HasKey("VORecorderFrameIDSource"))
            {
                id = PlayerPrefs.GetInt("VORecorderFrameIDSource") + 1;
            }

            PlayerPrefs.SetInt("VORecorderFrameIDSource", id);

            return id;
        }


        bool IsRecordingSentence
        {
            get
            {
                if (sentenceToken != null) return !sentenceToken.IsCancellationRequested;
                return false;
            }
        }

        bool IsEditingFrame
        {
            get
            {
                if (editFrameToken != null) return !editFrameToken.IsCancellationRequested;
                return false;
            }
        }

        VORecorder recorder;
        SerializedProperty finalFrameDelayObj;
        AudioSource source;
        List<VORecorderFrame> frames;

        CancellationTokenSource sentenceToken;
        CancellationTokenSource editFrameToken;
        DateTime startTime;
        Dictionary<VOPositions, Texture> voImages;


        public void OnEnable()
        {
            RefreshProperties();

            voImages = new Dictionary<VOPositions, Texture>();
            foreach (VOPositions p in Enum.GetValues(typeof(VOPositions)))
            {
                Texture texture = Resources.Load<Texture>($"vo/images/{p.ToString()}");
                voImages.Add(p, texture);
            }

            // set initial frame
            nextTexture = voImages[VOPositions.SilentMB];

            XLSoundUtils.StopAllClips();
        }


        public void OnDisable()
        {

        }


        void RefreshProperties()
        {
            recorder = (VORecorder)target;

            source = recorder.GetComponent<AudioSource>();
            recorder.clipLength = Mathf.FloorToInt(source.clip.length * 1000);
            finalFrameDelayObj = serializedObject.FindProperty("finalFrameDelay");
        }


        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            RefreshProperties();

            areaWidth = EditorGUIUtility.currentViewWidth;
            waveRect.x = 20.0f;
            waveRect.xMax = areaWidth - waveRect.x;

            GUILayout.Label("VO Recorder", EditorStyles.boldLabel);
            GUILayout.Space(10f);

            GUIStyle lenStyle = new GUIStyle(EditorStyles.label);
            lenStyle.normal.textColor = Color.yellow;
            EditorGUILayout.LabelField("Clip Length", $"{recorder.clipLength}", lenStyle);
            GUILayout.Space(10f);

            if (IsRecordingSentence)
            {
                DrawSentenceRecorder();
            }
            else
            {
                DrawNormalEditor();
                frames = new List<VORecorderFrame>();
                if (recorder.frames != null) frames.AddRange(recorder.frames);
            }

            ValidateAndSaveFrames();

            serializedObject.ApplyModifiedProperties();

            // update prefab AFTER the changed values are applied to the SoundClip object
            if (GUI.changed)
            {
                var inScenePrefab = PrefabUtility.GetOutermostPrefabInstanceRoot(recorder.gameObject);
                if (inScenePrefab != null)
                {
                    L.Log(LogEventType.METHOD, $"should update prefab FROM scene");
                    // gameobject is IN the scene
                    PrefabUtility.ApplyPrefabInstance(recorder.gameObject, InteractionMode.UserAction);
                }
            }
        }


        void ValidateAndSaveFrames()
        {
            if (frames != null && frames.Count > 0)
            {
                frames.Sort((x, y) => x.frameTime - y.frameTime);
            }

            recorder.frames = frames;
        }


        string sentence;
        void DrawSentenceRecorder()
        {
            sentence = EditorGUILayout.TextField("Sentence to parse", sentence);
            if (GUILayout.Button("Enter"))
            {
                frames = new List<VORecorderFrame>();
                string[] ary = sentence.Split(' ');
                int count = ary.Length;
                L.Log(LogEventType.INT, $"sentence has {ary.Length} spaces");

                // ie: E Ooh E Ooh UR FV SilentMB STCh SilentMB UR
                int delay = Mathf.FloorToInt(recorder.clipLength / count);
                for (int i = 0; i < count; i++)
                {
                    frames.Add(new VORecorderFrame
                    {
                        position = XLSound.GetEnumForString<VOPositions>(ary[i]),
                        frameTime = i * delay
                    });
                }

                L.Log(LogEventType.INT, $"final count: {count}, delay: {delay}");

                sentenceToken.Cancel();
            }
        }


        bool isConfirmingRecording = false;
        void DrawNormalEditor()
        {
            DrawFrameList();

            GUILayout.Space(10f);
            finalFrameDelayObj.intValue = EditorGUILayout.IntField("Final Frame Duration:", finalFrameDelayObj.intValue);
            GUILayout.Space(10f);

            DrawWaveForm();

            if (GUILayout.Button("Enter Sentence"))
            {
                bool confirm = EditorUtility.DisplayDialog(
                    "Replace current recording?",
                    "A recording already exists - do you want to replace it with a new recording?",
                    "Yes", "No");

                if (confirm)
                {
                    frames = new List<VORecorderFrame>(); // clear incase old exists
                    sentenceToken = new CancellationTokenSource();
                    L.Log(LogEventType.ERROR, $"Enter sentence recording started", true);
                }
            }

            if (frames != null && frames.Count > 0)
            {
                Preview(frames);
            }

        }


        void DrawFrameList()
        {
            if (frames != null)
            {
                foreach (VORecorderFrame frame in frames)
                {
                    if (GUILayout.Button($"{frame.position}: {frame.frameTime}"))
                    {
                        editedFrame = frame;
                        var p = (frame.frameTime * 0.001f) / source.clip.length;
                        UpdateTimeStampsAndLine(p);
                        PlayAudioFromPercentage(p);
                        editFrameToken = new CancellationTokenSource();
                    }
                    GUILayout.Space(5f);
                }
            }
        }


        Rect waveRect = new Rect();
        float areaWidth = 0.0f;
        int currentTimeStamp = 0;
        float time = 0.0f;
        float p = 0.0f;
        Vector4 lastLine;
        float UpdateFrameTimePoint(float point)
        {
            p = point / waveRect.width;
            UpdateTimeStampsAndLine(p);

            return p;
        }


        void UpdateTimeStampsAndLine(float p)
        {
            time = source.clip.length * p;
            currentTimeStamp = Mathf.FloorToInt(time * 1000);
            UpdateLastLine(new Vector2(areaWidth * p, 0), new Vector2(areaWidth * p, 100));
        }


        VORecorderFrame editedFrame;
        void DrawWaveForm()
        {
            GUILayout.Space(10f);

            var tex = XLSoundUtils.PaintWaveformSpectrum(source.clip, (int)areaWidth, 100, Color.green);
            if (lastLine != null)
            {
                XLSoundUtils.DrawTimeMarkerLine(tex, new Vector2(lastLine.x, lastLine.y), new Vector2(lastLine.z, lastLine.w), Color.red);
            }
            var centeredStyle = GUI.skin.GetStyle("Label");
            centeredStyle.alignment = TextAnchor.MiddleCenter;
            GUILayout.Box(tex, centeredStyle);

            bool isOnBox = false;

            if (GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition)) isOnBox = true;
            else isOnBox = false;


            if (isOnBox)
            {
                Event e = Event.current;

                if (e.type == EventType.MouseDrag || e.type == EventType.MouseUp)
                {
                    var point = e.mousePosition.x - waveRect.x;
                    var per = UpdateFrameTimePoint(point);
                    if (e.type == EventType.MouseUp && isOnBox)
                    {
                        PlayAudioFromPercentage(p);
                    }

                    Repaint();
                }
            }

            if (IsEditingFrame)
            {
                editedFrame.frameTime = EditorGUILayout.IntField("Time Stamp:", currentTimeStamp);
                editedFrame.position = (VOPositions)EditorGUILayout.EnumPopup("Primitive to create:", editedFrame.position);
                if (GUILayout.Button("Done"))
                {
                    L.Log(LogEventType.BOOL, $"finished editing frame");
                    editFrameToken.Cancel();
                }
                if (GUILayout.Button("Delete"))
                {
                    bool confirm = EditorUtility.DisplayDialog(
                        "Delete?",
                        "Are you sure you want to remove this animation position?",
                        "Yes", "No");

                    if (confirm)
                    {
                        editFrameToken.Cancel();
                        frames.Remove(editedFrame);
                    }
                }
            }
            else
            {
                EditorGUILayout.FloatField("Time Stamp:", currentTimeStamp);
                if (GUILayout.Button("Add New"))
                {
                    int lastFrame = currentTimeStamp;
                    if (currentTimeStamp <= 0)
                    {
                        lastFrame = frames[frames.Count - 1].frameTime + 10;
                        var p = (lastFrame * 0.001f) / source.clip.length;
                        UpdateTimeStampsAndLine(p);
                    }
                    L.Log(LogEventType.BOOL, $"add new frame. lastFrame: {lastFrame}");
                    editedFrame = new VORecorderFrame
                    {
                        frameTime = lastFrame
                    };

                    frames.Add(editedFrame);
                    editFrameToken = new CancellationTokenSource();
                }
            }
            GUILayout.Space(10f);
        }


        void PlayAudioFromPercentage(float p)
        {
            var samples = XLSoundUtils.GetSampleCount(source.clip);
            var playFrom = Mathf.FloorToInt(samples * p);
            XLSoundUtils.PlayClip(source.clip, playFrom, false);
        }


        void UpdateLastLine(Vector2 p1, Vector2 p2)
        {
            lastLine = new Vector4
            {
                x = p1.x,
                y = p1.y,
                z = p2.x,
                w = p2.y
            };
        }


        bool isPreviewing = false;
        Texture nextTexture;
        void Preview(List<VORecorderFrame> list)
        {
            GUILayout.Space(10f);
            GUILayout.Label("VO Preview", EditorStyles.boldLabel);
            GUILayout.Space(10f);

            if (GUILayout.Button("Preview"))
            {
                ShowPreview(list);
                Repaint();
            }

            if (nextTexture != null)
            {
                var centeredStyle = GUI.skin.GetStyle("Label");
                centeredStyle.alignment = TextAnchor.MiddleCenter;
                GUILayout.Box(nextTexture, centeredStyle);
                Repaint();
            }
        }


        async void ShowPreview(List<VORecorderFrame> list)
        {
            isPreviewing = true;
            Queue<VORecorderFrame> frames = GetVOQue(list);
            VORecorderFrame frame;

            nextTexture = voImages[VOPositions.SilentMB];

            await Task.Delay(250);

            var playTime = DateTime.Now;
            bool didStartAudio = false;

            while (frames.Count > 0)
            {
                frame = frames.Dequeue();
                nextTexture = frame.texture;
                if (!didStartAudio)
                {
                    XLSoundUtils.PlayClip(source.clip);
                    didStartAudio = true;
                }

                Repaint();
                await Task.Delay(frame.span);
            }

            await Task.Delay(recorder.finalFrameDelay);
            isPreviewing = false;
            nextTexture = voImages[VOPositions.SilentMB];
        }


        Queue<VORecorderFrame> GetVOQue(List<VORecorderFrame> list)
        {
            Queue<VORecorderFrame> que = new Queue<VORecorderFrame>();

            int lastTime = 0;
            foreach (VORecorderFrame v in list)
            {
                v.texture = voImages[v.position];
                v.span = v.frameTime - lastTime;
                lastTime = v.frameTime;
                que.Enqueue(v);
            }

            return que;
        }
    }

#endif
}

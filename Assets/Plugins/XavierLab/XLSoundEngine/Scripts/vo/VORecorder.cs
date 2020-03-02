using System;
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
        //[HideInInspector]
        public SoundClip soundClip;

        [SerializeField]
        public List<VORecorderFrame> frames;
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(VORecorder))]
    [CanEditMultipleObjects]
    public class VORecorderInspector : Editor
    {
        bool IsRecording
        {
            get
            {
                if (recorderToken != null) return !recorderToken.IsCancellationRequested;
                return false;
            }
        }

        VORecorder recorder;
        SerializedProperty soundClipObj;
        //SerializedProperty framesObj;

        CancellationTokenSource recorderToken;

        Dictionary<VOPositions, Texture> voImages;


        public void OnEnable()
        {
            RefreshProperties();

            voImages = new Dictionary<VOPositions, Texture>();
            foreach(VOPositions p in Enum.GetValues(typeof(VOPositions)))
            {
                Texture texture = Resources.Load<Texture>($"vo/images/{p.ToString()}");
                voImages.Add(p, texture);
            }

            // set initial frame
            nextTexture = voImages[VOPositions.SilentMB];
        }


        public void OnDisable()
        {
            if (IsRecording)
            {
                recorderToken.Cancel();
                L.Log(LogEventType.WARN, $"LEAVING VORECORDER - turning off recording", true);
            }            
        }


        void RefreshProperties()
        {
            recorder = (VORecorder)target;

            soundClipObj = serializedObject.FindProperty("soundClip");
            //framesObj = serializedObject.FindProperty("frames");
            //mixerObj = serializedObject.FindProperty("mixer");
            //mixerGroupObj = serializedObject.FindProperty("mixerGroup");
            //soundTypeObj = serializedObject.FindProperty("soundType");
            //autoPlayObj = serializedObject.FindProperty("autoPlay");
            //loopObj = serializedObject.FindProperty("loop");
            //snapshotObj = serializedObject.FindProperty("snapshot");
            //pitchRangeObj = serializedObject.FindProperty("pitchRange");
        }

        DateTime startTime;
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            RefreshProperties();

            GUILayout.Label("VO Recorder", EditorStyles.boldLabel);
            GUILayout.Space(10f);

            bool changed = DrawDefaultInspector();

            if (soundClipObj.objectReferenceValue == null)
            {
                soundClipObj.objectReferenceValue = recorder.GetComponent<SoundClip>();
            }

            var frames = new List<VORecorderFrame>();
            if (recorder.frames != null) frames.AddRange(recorder.frames);

            if (!IsRecording)
            {
                if (GUILayout.Button("Record"))
                {
                    bool confirm = EditorUtility.DisplayDialog(
                        "Replace current recording?",
                        "A recording already exists - do you want to replace it with a new recording?",
                        "Yes", "No");

                    if (confirm)
                    {
                        frames = new List<VORecorderFrame>(); // clear incase old exists
                        recorderToken = new CancellationTokenSource();
                        var source = recorder.GetComponent<AudioSource>();
                        L.Log(LogEventType.ERROR, $"Recording started: clip length: {source.clip.length}", true);
                        XLSoundUtils.PlayClip(source.clip);
                        startTime = DateTime.Now;
                        MonitorAudioClip(source.clip);
                    }
                }
            }
            else
            {
                GUIStyle s = new GUIStyle(EditorStyles.miniButton);
                s.normal.textColor = Color.red;
                s.onHover.textColor = Color.red;
                if (GUILayout.Button("Stop Recording",s))
                {
                    L.Log(LogEventType.ERROR, $"Recording stopped", true);
                    XLSoundUtils.StopAllClips();
                    recorderToken.Cancel();
                }
                else
                {
                    Event e = Event.current;
                    if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Space)
                    {
                        var frame = RecordFrame();
                        frames.Add(frame);
                    }
                }

            }

            if (frames != null && frames.Count > 0)
            {
                Preview(frames);
            }

            recorder.frames = frames;

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

            if( nextTexture != null )
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

            await Task.Delay(1000);

            var playTime = DateTime.Now;
            bool didStartAudio = false;

            while (frames.Count > 0)
            {
                L.Log(LogEventType.METHOD, $"Setting texture for preview: {(DateTime.Now - playTime).TotalMilliseconds}");
                frame = frames.Dequeue();
                nextTexture = frame.texture;
                if(/*(DateTime.Now - playTime).TotalMilliseconds > 21f &&*/ !didStartAudio)
                {
                    var source = recorder.GetComponent<AudioSource>();
                    XLSoundUtils.PlayClip(source.clip);
                    didStartAudio = true;
                }

                Repaint();
                await Task.Delay((int)frame.span);
                //if (frames.Count == 0) isPreviewing = false;
            }

            await Task.Delay(250);
            isPreviewing = false;
            nextTexture = voImages[VOPositions.SilentMB];
        }


        Queue<VORecorderFrame> GetVOQue(List<VORecorderFrame> list)
        {
            Queue<VORecorderFrame> que = new Queue<VORecorderFrame>();

            float lastTime = 0;
            foreach(VORecorderFrame v in list)
            {
                v.texture = voImages[v.position];
                v.span = v.frameTime - lastTime;
                lastTime = v.frameTime;
                que.Enqueue(v);
            }

            return que;
        }


        async void MonitorAudioClip(AudioClip clip)
        {
            CancellationTokenSource token = new CancellationTokenSource();
            var value = 0;
            MainEditorDispatcher.Invoke(() =>
            {
                value = Mathf.FloorToInt(clip.length * 1000);
                L.Log(LogEventType.BOOL, $"value: {value}");
                token.Cancel();
            });

            while (!token.IsCancellationRequested) await Task.Delay(5);

            await Task.Run(async () =>
            {
                L.Log(LogEventType.ERROR, $"Recording should last: {value}", true);
                await Task.Delay(value);
            });


            MainEditorDispatcher.Invoke(() =>
            {
                L.Log(LogEventType.ERROR, $"Recording stopped", true);
                recorderToken.Cancel();
                XLSoundUtils.StopAllClips();
                Repaint();
            });
        }


        VORecorderFrame RecordFrame()
        {
            float time = Mathf.Floor((float)(DateTime.Now - startTime).TotalMilliseconds);
            L.Log(LogEventType.BOOL, $"CREATE VO FRAME: {time}", true);
            VORecorderFrame frame = new VORecorderFrame
            {
                frameTime = time
            };

            return frame;
        }
    }

#endif
}

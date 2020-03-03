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
        bool IsRecording
        {
            get
            {
                if (recorderToken != null) return !recorderToken.IsCancellationRequested;
                return false;
            }
        }

        bool IsRecordingSentence
        {
            get
            {
                if (sentenceToken != null) return !sentenceToken.IsCancellationRequested;
                return false;
            }
        }

        VORecorder recorder;
        SerializedProperty finalFrameDelayObj;
        AudioSource source;
        List<VORecorderFrame> frames;

        CancellationTokenSource recorderToken;
        CancellationTokenSource sentenceToken;
        DateTime startTime;
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

            source = recorder.GetComponent<AudioSource>();
            recorder.clipLength = Mathf.FloorToInt(source.clip.length * 1000);
            finalFrameDelayObj = serializedObject.FindProperty("finalFrameDelay");
        }

        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            RefreshProperties();

            GUILayout.Label("VO Recorder", EditorStyles.boldLabel);
            GUILayout.Space(10f);

            GUIStyle lenStyle = new GUIStyle(EditorStyles.label);
            lenStyle.normal.textColor = Color.yellow;
            EditorGUILayout.LabelField("Clip Length",$"{recorder.clipLength}", lenStyle);
            GUILayout.Space(10f);            

            if(IsRecordingSentence)
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
            //for(int i=0; i < frames.Count; i++)
            //{
            //    var prev = i > 0 ? frames[i - 1] : null;                 //i=0? null, i=1? 0
            //    var current = frames[i];                                 //i=0? 0   , i=1? 1
            //    var next = i <= frames.Count - 2 ? frames[i + 1] : null; //i=0? 1   , i=1? 2

            //    if (prev == null) current.frameTime = Mathf.Max(0, current.frameTime);
            //    else if (prev != null && next != null) current.frameTime = Mathf.Clamp(current.frameTime, prev.frameTime, next.frameTime);
            //    if (next == null) current.frameTime = Mathf.Min(recorder.clipLength, current.frameTime);
            //}

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
                for(int i = 0; i < count; i++)
                {
                    frames.Add(new VORecorderFrame
                    {
                        position = XLSound.GetEnumForString<VOPositions>(ary[i]),
                        frameTime = Mathf.Max(1,i) * delay
                    });
                }

                L.Log(LogEventType.INT, $"final count: {count}, delay: {delay}");
                
                sentenceToken.Cancel();
            }
        }

        bool isConfirmingRecording = false;
        void DrawNormalEditor()
        {
            bool changed = DrawDefaultInspector();

            GUILayout.Space(10f);
            finalFrameDelayObj.intValue = EditorGUILayout.IntField("Final Frame Duration:", finalFrameDelayObj.intValue);
            GUILayout.Space(10f);

            

            if (!IsRecording)
            {
                if (!isConfirmingRecording)
                {
                    if (GUILayout.Button("Record"))
                    {
                        isConfirmingRecording = true;
                    }
                }
                else
                {
                    GUIStyle s = new GUIStyle(EditorStyles.miniButton);
                    s.normal.textColor = Color.yellow;
                    s.onHover.textColor = Color.yellow;
                    if (GUILayout.Button("Do you want to replace current recording?",s))
                    {
                        //bool confirm = EditorUtility.DisplayDialog(
                        //    "Replace current recording?",
                        //    "A recording already exists - do you want to replace it with a new recording?",
                        //    "Yes", "No");
                                                
                        frames = new List<VORecorderFrame>(); // clear incase old exists
                        recorderToken = new CancellationTokenSource();
                        L.Log(LogEventType.ERROR, $"Recording started: clip length: {source.clip.length}", true);
                        XLSoundUtils.PlayClip(source.clip);
                        startTime = DateTime.Now;
                        MonitorAudioClip(source.clip);
                        isConfirmingRecording = false;
                    }
                    if (GUILayout.Button("Cancel"))
                    {
                        isConfirmingRecording = false;
                    }
                }

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
            else
            {
                GUIStyle s = new GUIStyle(EditorStyles.miniButton);
                s.normal.textColor = Color.red;
                s.onHover.textColor = Color.red;
                if (GUILayout.Button("Stop Recording", s))
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

            await Task.Delay(1000);

            var playTime = DateTime.Now;
            bool didStartAudio = false;

            while (frames.Count > 0)
            {
                frame = frames.Dequeue();
                nextTexture = frame.texture;
                if(!didStartAudio)
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
            int time = Mathf.FloorToInt((float)(DateTime.Now - startTime).TotalMilliseconds);
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

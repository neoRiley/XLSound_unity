using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using System.Threading;
using System.Reflection;
using System.IO;

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
        SerializedProperty framesObj;
        AudioSource source;
        List<VORecorderFrame> frames;

        CancellationTokenSource sentenceToken;
        CancellationTokenSource editFrameToken;
        DateTime startTime;
        Dictionary<VOPositions, Texture> voImages;

        Texture2D waveFormTex;
        Texture2D tex;

        bool isPrefab = false;

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
            SavePrefab();
        }


        void RefreshProperties()
        {
            recorder = (VORecorder)target;
            isPrefab = CheckIsPrefab(recorder.gameObject);
            source = recorder.GetComponent<AudioSource>();
            recorder.clipLength = Mathf.FloorToInt(source.clip.length * 1000);
            frames = recorder.frames;
            //finalFrameDelayObj = serializedObject.FindProperty("finalFrameDelay");
            //framesObj = serializedObject.FindProperty("frames").Copy();

            //L.Log(LogEventType.STRING, $"framesObj type: {framesObj.arrayElementType}, size: {framesObj.arraySize}");

            //for (int i = 0; i < framesObj.arraySize; i++)
            //{
            //    var obj = framesObj.GetArrayElementAtIndex(i);
            //    L.Log(LogEventType.BOOL, $"type: {obj.GetType()}");
            //}

            //if (framesObj.isArray)
            //{
            //    int arraylength = 0;

            //    framesObj.Next(true); // skip generic field
            //    framesObj.Next(true); // advance to array size field

            //    arraylength = framesObj.intValue;

            //    framesObj.Next(true); // advance to first array index

            //    frames = new List<VORecorderFrame>(arraylength);

            //    L.Log(LogEventType.STRING, $"frams arrayLength: {arraylength}");

            //    for (int i = 0; i < arraylength; i++)
            //    {
            //        var frame = framesObj.objectReferenceValue;// as object as VORecorderFrame;
            //        //var frame = framesObj.GetArrayElementAtIndex(i) as object as VORecorderFrame;
            //        //frames.Add(frame);
            //        L.Log(LogEventType.STRING, $"frame added to frames: {frame.GetType()}");
            //        //if (i < arraylength - 1) framesObj.Next(false);
            //    }
            //}
        }


        void SetupWaveformProperties()
        {
            areaWidth = EditorGUIUtility.currentViewWidth;
            waveRect.x = 20.0f;
            waveRect.xMax = areaWidth - waveRect.x;
            waveRect.yMax = 100;

            tex = new Texture2D((int)areaWidth, 100, TextureFormat.RGBA32, false);
            if (waveFormTex == null && source != null)
            {
                waveFormTex = XLSoundUtils.PaintWaveformSpectrum(source.clip, (int)areaWidth, 100, Color.green);
            }
        }


        bool CheckIsPrefab(GameObject g)
        {
            bool exists = false;
            GameObject prefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(g);
            if (prefab != null) exists = true;
            else
            {
                var path = AssetDatabase.GetAssetPath(g);
                exists = Path.GetExtension(path).Contains(".prefab");
            }

            return exists;
        }


        public override void OnInspectorGUI()
        {
            //serializedObject.Update();
            RefreshProperties();



            Event e = Event.current;

            GUILayout.Label("VO Recorder", EditorStyles.boldLabel);
            GUILayout.Space(10f);

            SetupWaveformProperties();

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
                DrawNormalEditor(e);
                frames = new List<VORecorderFrame>();
                if (recorder.frames != null) frames.AddRange(recorder.frames);
            }


            ValidateAndSaveFrames();
            //serializedObject.ApplyModifiedProperties();

            // update prefab AFTER the changed values are applied to the SoundClip object
            if (GUI.changed)
            {
                L.Log(LogEventType.BOOL, $"GUI has changed", true);
                var inScenePrefab = PrefabUtility.GetOutermostPrefabInstanceRoot(recorder.gameObject);
                if (inScenePrefab != null)
                {
                    L.Log(LogEventType.METHOD, $"should update prefab FROM scene");
                    // gameobject is IN the scene
                    PrefabUtility.ApplyPrefabInstance(recorder.gameObject, InteractionMode.UserAction);
                }
                else if (isPrefab)
                {
                    var path = AssetDatabase.GetAssetPath(recorder.gameObject); // this works IF we're selecting the prefab in project view
                    L.Log(LogEventType.BOOL, $"isPrefab - path: {path}", true);

                }
            }
        }


        void SavePrefab()
        {
            if (isPrefab)
            {
                if (PrefabUtility.SavePrefabAsset(recorder.gameObject, out bool saved))
                {
                    L.Log(LogEventType.BOOL, $"prefab saved", true);
                }
                else L.Log(LogEventType.BOOL, $"prefab NOT saved", true);
            }
        }


        void ValidateAndSaveFrames()
        {
            if (frames != null && frames.Count > 0)
            {
                frames.Sort((x, y) => x.frameTime - y.frameTime);
            }

            //framesObj.Get
            //framesObj.objectReferenceValue = frames.ToArray();
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
        void DrawNormalEditor(Event e)
        {
            DrawFrameList();

            GUILayout.Space(10f);
            recorder.finalFrameDelay = EditorGUILayout.IntField("Final Frame Duration:", recorder.finalFrameDelay);
            GUILayout.Space(10f);

            DrawWaveForm(e);

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
                    var perc = (frame.frameTime * 0.001f) / source.clip.length;

                    if (GUILayout.Button($"{frame.position}: {frame.frameTime}"))
                    {
                        editedFrame = frame;
                        UpdateTimeStampsAndLine(perc); // make this the red line
                        PlayAudioFromPercentage(perc);
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
            if (point == 0 || waveRect.width == 0) return 0;

            p = point / waveRect.width;
            UpdateTimeStampsAndLine(p);

            return p;
        }

        // updates RED line - mouse clicks/drags
        void UpdateTimeStampsAndLine(float p)
        {
            time = source.clip.length * p;
            currentTimeStamp = Mathf.FloorToInt(time * 1000);
            lastLine = GetLineVector(p);
        }


        VORecorderFrame editedFrame;
        int dots = 0;
        void DrawWaveForm(Event e)
        {
            //Event e = Event.current;
            GUILayout.Space(10f);

            Graphics.CopyTexture(waveFormTex, tex);

            if (frames != null)
            {
                foreach (VORecorderFrame frame in frames)
                {
                    var perc = (frame.frameTime * 0.001f) / source.clip.length;
                    var thisLine = GetLineVector(perc);
                    XLSoundUtils.DrawTimeMarkerLine(tex, new Vector2(thisLine.x, thisLine.y), new Vector2(thisLine.z, thisLine.w), Color.gray);
                }
            }

            if (lastLine != null)
            {
                XLSoundUtils.DrawTimeMarkerLine(tex, new Vector2(lastLine.x, lastLine.y), new Vector2(lastLine.z, lastLine.w), Color.red);
            }
            var centeredStyle = GUI.skin.GetStyle("Label");
            centeredStyle.alignment = TextAnchor.MiddleCenter;
            GUILayout.Label(tex, centeredStyle);

            bool isOnBox = false;
            waveRect.y = GUILayoutUtility.GetLastRect().y;

            if (waveRect.Contains(Event.current.mousePosition))
            {
                isOnBox = true;
            }
            else isOnBox = false;



            if (isOnBox && (e.type == EventType.MouseDrag || e.type == EventType.MouseUp))
            {
                L.Log(LogEventType.INT, $"0: {e.rawType}");
                var point = e.mousePosition.x - waveRect.x;
                UpdateFrameTimePoint(point);

                if (e.type == EventType.MouseUp)
                {
                    L.Log(LogEventType.INT, $"2");
                    PlayAudioFromPercentage(p);
                }
                else XLSoundUtils.StopAllClips();

                Repaint();

                //EditorGUILayout.LabelField("Condition:", $"{isOnBox.ToString()}, type: {e.type}");
            }

            //dots = dots > 20 ? 0 : dots + 1;
            //var str = $"{isOnBox}";
            //for (int i = 0; i < dots; i++) str += ".";
            //EditorGUILayout.LabelField("Condition:", $"{str}");


            if (IsEditingFrame)
            {
                editedFrame.frameTime = EditorGUILayout.IntField("Time Stamp:", currentTimeStamp);
                editedFrame.position = (VOPositions)EditorGUILayout.EnumPopup("Primitive to create:", editedFrame.position);
                if (GUILayout.Button("Done"))
                {
                    L.Log(LogEventType.BOOL, $"finished editing frame");
                    SavePrefab();
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


        Vector4 GetLineVector(float p)
        {
            return new Vector4
            {
                x = areaWidth * p,
                y = 0,
                z = areaWidth * p,
                w = 100
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
                //Repaint();
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
                if (!didStartAudio)
                {
                    XLSoundUtils.PlayClip(source.clip);
                    didStartAudio = true;
                }
                await Task.Delay(frame.span);
                nextTexture = frame.texture;


                Repaint();
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
                L.Log(LogEventType.INT, $"span for {v.position}: {v.span}");
                lastTime = v.frameTime;
                que.Enqueue(v);
            }

            return que;
        }
    }

#endif
}

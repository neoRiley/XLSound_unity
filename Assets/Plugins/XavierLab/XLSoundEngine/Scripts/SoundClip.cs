using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace XavierLab
{
    public enum SoundClipTypes
    {
        SOUND_2D,
        SOUND_3D
    }

    [ExecuteInEditMode]
    [Serializable]
    public class SoundClip : MonoBehaviour
    {
        [SerializeField]
        [HideInInspector]
        private int id = -1;
        public int ID
        {
            get => id;
            set => id = value;
        }

        [SerializeField]
        [HideInInspector]
        protected string soundName = "";
        public string SoundName
        {
            get
            {
                if (String.IsNullOrEmpty(soundName)) soundName = gameObject.name;
                return soundName;
            }
            set
            {
                nameChanged = true;
                L.Log(LogEventType.BOOL, $"soundname changed: {nameChanged}, value: {value}");
                soundName = value;
#if UNITY_EDITOR
                MainEditorDispatcher.Invoke(() =>
                {
                    if (nameChanged)
                    {
                        nameChanged = false;
                        var prefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(gameObject);
                        L.Log(LogEventType.STRING, $"soundname changed: {prefab.name}");
                        gameObject.name = prefab.name;
                    }
                });
#endif
            }
        }
        bool nameChanged = false;

        [SerializeField]
        public AudioClip audioClip;

        [SerializeField]
        [HideInInspector]
        private AudioMixer mixer;
        public AudioMixer Mixer
        {
            get => mixer;
            set => mixer = value;
        }

        [SerializeField]
        private AudioMixerGroup mixerGroup;
        public AudioMixerGroup MixerGroup
        {
            get => mixerGroup;
            set => mixerGroup = value;
        }

        [SerializeField]
        [HideInInspector]
        public SoundClipTypes soundType = SoundClipTypes.SOUND_2D;

        [SerializeField]
        public bool autoPlay = false;

        [SerializeField]
        public bool loop = false;

        [SerializeField]
        public AudioMixerSnapshot snapshot;

        [SerializeField]
        [HideInInspector]
        public int tags = 0;

        [SerializeField]
        [HideInInspector]
        public string tagsList;

        [SerializeField]
        [HideInInspector]
        public string prefabPath;

        [SerializeField]
        [HideInInspector]
        public float pitchRange = 0;

        public Sounds Sound
        {
            get
            {
                return XLSound.GetEnumForString<Sounds>(SoundName);
            }
        }

        [HideInInspector]
        protected AudioSource audioSource;
        public AudioSource AudioSource { get => audioSource; }

        void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }


        public void PlaySound()
        {
            if (!pitchRange.Equals(0))
            {
                float r = UnityEngine.Random.Range(-pitchRange, pitchRange);
                audioSource.pitch = 1 + r;
            }
            else audioSource.pitch = 1;

            audioSource.Play();
        }

    }

#if UNITY_EDITOR
    [CustomEditor(typeof(SoundClip))]
    [CanEditMultipleObjects]
    public class SoundClipInspector : Editor
    {
        SoundClip soundClip;
        SerializedProperty soundNameObj;
        SerializedProperty audioClipObj;
        SerializedProperty mixerObj;
        SerializedProperty mixerGroupObj;
        SerializedProperty soundTypeObj;
        SerializedProperty snapshotObj;
        SerializedProperty autoPlayObj;
        SerializedProperty loopObj;
        SerializedProperty pitchRangeObj;
        List<AudioMixerSnapshot> snapshots;

        string[] tagStr;

        bool IsPlayingSound
        {
            get
            {
                if (playbackToken != null) return !playbackToken.IsCancellationRequested;
                return false;
            }
        }

        CancellationTokenSource playbackToken;

        public void OnEnable()
        {
            tagStr = UnityEditorInternal.InternalEditorUtility.tags;
        }

        public void OnDisable()
        {
            if (IsPlayingSound)
            {
                XLSoundUtils.StopAllClips();
                playbackToken.Cancel();
            }
        }

        void RefreshProperties()
        {
            soundClip = (SoundClip)target;
            soundNameObj = serializedObject.FindProperty("soundName");
            audioClipObj = serializedObject.FindProperty("audioClip");
            mixerObj = serializedObject.FindProperty("mixer");
            mixerGroupObj = serializedObject.FindProperty("mixerGroup");
            soundTypeObj = serializedObject.FindProperty("soundType");
            autoPlayObj = serializedObject.FindProperty("autoPlay");
            loopObj = serializedObject.FindProperty("loop");
            snapshotObj = serializedObject.FindProperty("snapshot");
            pitchRangeObj = serializedObject.FindProperty("pitchRange");
        }

        public override void OnInspectorGUI()
        {
            // updates the serializedObject with the current objects data
            serializedObject.Update();

            RefreshProperties();

            // set comparison values
            var audioClipName = soundClip.audioClip != null ? soundClip.audioClip.name : "";
            var mixerName = soundClip.Mixer != null ? soundClip.Mixer.name : "";
            var mixerGroupName = soundClip.MixerGroup != null ? soundClip.MixerGroup.name : "";
            var soundName = soundClip.name;

            var isPrefab = CheckIsPrefab(soundClip.gameObject);

            GUILayout.Label("Sound Clip Editor", EditorStyles.boldLabel);
            GUILayout.Space(10f);

            if (audioClipObj.objectReferenceValue != null &&
                mixerGroupObj.objectReferenceValue != null)
            {
                var s = GetSafeSoundName();
                EditorGUILayout.LabelField("SoundName: ", s);
                soundNameObj.stringValue = s;
                if (!isPrefab) soundClip.name = s; // only update if not a prefab
            }
            else
            {
                GUIStyle s = new GUIStyle(EditorStyles.textField);
                s.normal.textColor = Color.grey;
                EditorGUILayout.LabelField("SoundName: ", "[Set AudioClip to edit]", s);
            }

            bool createOrUpdatePrefab = false;
            bool changed = DrawDefaultInspector();

            GUILayout.Space(10f);

            pitchRangeObj.floatValue = EditorGUILayout.Slider(new GUIContent("Pitch Range:", "Sound clip's pitch will randomly range from 1 (normal) to this negative and positive range. 0 will always play at normal pitch."), pitchRangeObj.floatValue, 0.0f, 3.0f);

            GUILayout.Space(10f);
            var clipType = XLSound.GetEnumForInt<SoundClipTypes>(soundTypeObj.intValue);
            soundTypeObj.intValue = EditorGUILayout.EnumPopup("Sound Type: ", clipType).GetHashCode();

            //==================== [TAGS] ===================
            GUI.enabled = soundTypeObj.intValue == 0 ? false : true;
            if (!GUI.enabled) serializedObject.FindProperty("tags").intValue = 0;

            SerializedProperty tagsObj = serializedObject.FindProperty("tags");

            serializedObject.FindProperty("tags").intValue = EditorGUILayout.MaskField("Tags: ", tagsObj.intValue, tagStr);

            if (tagsObj.intValue > 0)
            {
                int mask = tagsObj.intValue; // This is my maskfield's result
                string tagsList = "";

                for (int i = 0; i < tagStr.Length; i++)
                {
                    int layer = 1 << i;
                    if ((mask & layer) != 0)
                    {
                        tagsList += tagStr[i] + ",";
                    }
                }

                tagsList = tagsList.Substring(0, tagsList.Length - 1);

                serializedObject.FindProperty("tagsList").stringValue = tagsList;

                EditorGUILayout.LabelField("Tags List: ", tagsList);
            }
            else serializedObject.FindProperty("tagsList").stringValue = "";

            GUI.enabled = true;
            //====================

            var source = soundClip.GetComponent<AudioSource>();
            GUI.enabled = source == null ? false : true;
            if (!IsPlayingSound)
            {
                if (GUILayout.Button("Play"))
                {
                    playbackToken = new CancellationTokenSource();
                    XLSoundUtils.PlayClip(source.clip);
                    L.Log(LogEventType.ERROR, $"playback started: clip length: {source.clip.length}", true);
                    MonitorAudioClip(source.clip);
                    return;
                }
            }
            else
            {
                if (GUILayout.Button("Stop"))
                {
                    playbackToken.Cancel();
                    XLSoundUtils.StopAllClips();
                }

                return;
            }
            GUI.enabled = true;

            if (audioClipObj.objectReferenceValue != null && !isPrefab)
            {
                GUILayout.Space(10f);
                createOrUpdatePrefab = GUILayout.Button("Create or update prefab");
            }

            if (changed || GUI.changed)
            {
                L.Log(LogEventType.STRING, $"TagsList: {serializedObject.FindProperty("tagsList").stringValue}");
                var audioSource = soundClip.gameObject.GetComponent<AudioSource>();

                if (mixerGroupObj.objectReferenceValue != null)
                {
                    var mixerGroup = mixerGroupObj.objectReferenceValue as AudioMixerGroup;

                    if (!mixerGroupName.Equals(mixerGroup.name))
                    {
                        mixerObj.objectReferenceValue = mixerGroup.audioMixer;
                        audioSource.outputAudioMixerGroup = mixerGroup;
                    }
                }

                if (audioClipObj.objectReferenceValue != null)
                {
                    AudioClip clip = audioClipObj.objectReferenceValue as AudioClip;
                    if (!audioClipName.Equals(clip.name))
                    {
                        audioSource.clip = clip;
                        L.Log(LogEventType.BOOL, $"AudioClip changed: {clip.name}, old clip: {audioClipName}, has component: {soundClip.gameObject.GetComponent<AudioSource>() != null}");
                    }
                }

                audioSource.spatialBlend = soundTypeObj.intValue == 1 ? 1.0f : 0.0f;
                audioSource.playOnAwake = autoPlayObj.boolValue;
                audioSource.loop = loopObj.boolValue;

                if (isPrefab)
                {
                    var path = AssetDatabase.GetAssetPath(soundClip.gameObject); // this works IF we're selecting the prefab in project view
                    if (String.IsNullOrEmpty(path))
                    {
                        // this works if the asset is in the scene, is a prefab and is selected
                        GameObject prefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(soundClip.gameObject);
                        path = AssetDatabase.GetAssetPath(prefab);
                    }

                    //var g = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    //audioSource = g.GetComponent<AudioSource>();
                    //if (audioSource != null)
                    //{
                    //    L.Log(LogEventType.BOOL, $"prefabs AudioSource NOT null {soundTypeObj.intValue}");
                    //    audioSource.spatialBlend = soundTypeObj.intValue == 1 ? 1.0f : 0.0f;
                    //    audioSource.playOnAwake = autoPlayObj.boolValue;
                    //    audioSource.loop = loopObj.boolValue;
                    //}

                    if (!String.IsNullOrEmpty(path))
                    {
                        soundClip.prefabPath = path;
                        var fileName = Path.GetFileNameWithoutExtension(path);
                        if (!fileName.Equals(GetSafeSoundName()))
                        {
                            // prefab is selected in the scene, update the gameObject
                            soundClip.SoundName = GetSafeSoundName();
                            L.Log(LogEventType.BOOL, $"prefabs path: {path}");
                            AssetDatabase.RenameAsset(path, GetSafeSoundName());
                        }
                    }

                }
            }

            serializedObject.ApplyModifiedProperties();

            
            // update prefab AFTER the changed values are applied to the SoundClip object
            if (changed || GUI.changed)
            {
                var inScenePrefab = PrefabUtility.GetOutermostPrefabInstanceRoot(soundClip.gameObject);
                if (inScenePrefab != null)
                {
                    L.Log(LogEventType.METHOD, $"should update prefab FROM scene");
                    // gameobject is IN the scene
                    PrefabUtility.ApplyPrefabInstance(soundClip.gameObject, InteractionMode.UserAction);
                }
            }

            if (createOrUpdatePrefab)
            {
                L.Log(LogEventType.BOOL, $"Create or update prefab");
                CreatePrefab(soundClip.gameObject);
            }
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
                playbackToken.Cancel();
                XLSoundUtils.StopAllClips();
                Repaint();
            });
        }


        bool CheckIsPrefab(GameObject g)
        {
            bool exists = false;
            GameObject prefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(soundClip.gameObject);
            if (prefab != null) exists = true;
            else
            {
                var path = AssetDatabase.GetAssetPath(g);
                exists = Path.GetExtension(path).Contains(".prefab");
            }

            return exists;
        }


        string GetSafeSoundName()
        {
            var audioClipName = (audioClipObj.objectReferenceValue as AudioClip).name;
            //var soundID = soundClip.ID.ToString();
            var name = $"{audioClipName}";
            name = name.Replace(" ", "_");
            return name;
        }


        public static void CreatePrefab(GameObject source)
        {
            try
            {
                var path = Path.Combine(Application.dataPath, "Audio", "Resources", "XLSoundPrefabs");

                bool created = false;
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, Path.Combine(path, source.name + ".prefab").Replace(@"\", "/"), out created);
                L.Log(LogEventType.BOOL, $"prefab '{source.name}' created: {created}");
                if (!Application.isPlaying) GameObject.DestroyImmediate(source);
            }
            catch (Exception err)
            {
                L.Log(LogEventType.WARN, $"error: {err.Message}, {err.StackTrace}");
            }
        }
    }
#endif
}

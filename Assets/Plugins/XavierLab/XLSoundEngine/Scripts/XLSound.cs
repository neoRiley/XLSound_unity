﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace XavierLab
{
    public class XLSound
    {
        public static event Action<Sounds> OnVOStarted;
        public static event Action<VOPositions, Sounds> OnVOPositionChanged;
        public static event Action<Sounds> OnVOCompleted;

        public static bool initialized = false;

        static GameObject soundsContainer;
        static List<Sounds> currentSounds = new List<Sounds>();
        static Dictionary<Sounds, GameObject> soundPointers = new Dictionary<Sounds, GameObject>();
        static Dictionary<string, List<GameObject>> tagPointers = new Dictionary<string, List<GameObject>>();

        static string prefabsPath = Path.Combine("XLSoundPrefabs");

        static AudioMixer mainMixer;

        [RuntimeInitializeOnLoadMethod]
        public static async Task Initialize()
        {
            L.Log(LogEventType.NORMAL,
            $"{L.Style("XL", LogEventType.ERROR, true)} " +
            $"{L.Style("Sound Engine", Clrs.WHITE, true)} " +
            $"{L.Style("Initialized", LogEventType.BOOL, true, true)}"
            );

            mainMixer = Resources.Load<AudioMixer>("Mixers/Master") as AudioMixer;

            initialized = true;
        }


        public static void Mute(bool immediately = false)
        {
            if (mainMixer != null)
            {
                var snapshot = mainMixer.FindSnapshot(MasterSnapshots.NoVolume.ToString());
                snapshot.TransitionTo((float)(immediately ? 0.0 : 5.0f));
            }
            else L.Log(LogEventType.ERROR, $"mainMixer does not exist.  Cannot mute globally.");
        }


        public static void UnMute(bool immediately = false)
        {
            if (mainMixer != null)
            {
                var snapshot = mainMixer.FindSnapshot(MasterSnapshots.FullVolume.ToString());
                snapshot.TransitionTo((float)(immediately ? 0.0 : 5.0f));
            }
            else L.Log(LogEventType.ERROR, $"mainMixer does not exist.  Cannot UnMute globally.");
        }


        /// <summary>
        /// Play a sound.
        /// </summary>
        /// <param name="sound"></param>
        public static void PlaySound(Sounds sound)
        {
            SoundClip soundClip = GetSoundClipForSound(sound);
            if (soundClip != null)
            {
                soundClip.PlaySound();
            }
            else L.Log(LogEventType.ERROR, $"SoundClip for {sound} is null");
        }


        /// <summary>
        /// Play a sound with a transition/snapshot.
        /// </summary>
        /// <param name="sound"></param>
        /// <param name="duration"></param>
        public static void PlaySound(Sounds sound, float duration = 2.0f)
        {
            SoundClip soundClip = GetSoundClipForSound(sound);
            if (soundClip != null && soundClip.snapshot != null)
            {
                soundClip.snapshot.TransitionTo(duration);
                PlaySound(sound);
            }
            else L.Log(LogEventType.ERROR, $"Transition not found for {sound}. SoundClip is null and likely needs to be added to a SoundLoader's list of audio clips", true);
        }


        /// <summary>
        /// Play the sound at a certain position in the scene.  Typically, a sound is not assigned
        /// to any tag name, so there is only the single instance to play.
        /// </summary>
        /// <param name="sound"></param>
        /// <param name="position"></param>
        public static void PlaySound(Sounds sound, Vector3 position)
        {
            SoundClip soundClip = GetSoundClipForSound(sound);
            if (soundClip != null)
            {
                soundClip.transform.position = position;
                PlaySound(sound);
            }
            else L.Log(LogEventType.ERROR, $"Audio source for {sound} is null");
        }


        /// <summary>
        /// Play all sounds assigned to the tag name passed in.
        /// </summary>
        /// <param name="tagName"></param>
        public static void PlaySound(string tagName)
        {
            var clips = GetSoundClipsForTag(tagName);

            if (clips.Count > 0)
            {
                foreach (SoundClip clip in clips)
                {
                    PlaySound(clip.Sound);
                }
            }
        }


        /// <summary>
        /// Play a sound attached to this game Object.  If you set a tag for a soundclip, it will be parented
        /// to a GameObject and that object can be passed in to play the sound.
        /// </summary>
        /// <param name="obj"></param>
        public static void PlaySound(GameObject obj)
        {
            SoundClip clip = obj.GetComponent<SoundClip>();
            if (clip == null)
            {
                clip = obj.GetComponentInChildren<SoundClip>();
                if (clip != null)
                {
                    PlaySound(clip.Sound);
                }
                else L.Log(LogEventType.ERROR, $"No audioSource component found on {obj.name}");
            }
        }


        public static async void PlayVOSound(Sounds sound, Action<VOPositions> onMouthChange = null, Action onComplete = null)
        {
            SoundClip soundClip = GetSoundClipForSound(sound);
            if (soundClip == null) return;
            VORecorder recorder = soundClip.GetComponent<VORecorder>();

            try
            {
                if (recorder != null)
                {
                    List<VORecorderFrame> list = recorder.frames;
                    Queue<VORecorderFrame> frames = GetVOQue(list);
                    VORecorderFrame frame;
                    onMouthChange?.Invoke(VOPositions.SilentMB);
                    OnVOPositionChanged?.Invoke(VOPositions.SilentMB, sound);
                    OnVOStarted?.Invoke(sound);

                    L.Log(LogEventType.INT, $"PlayVOSound: {sound}");

                    PlaySound(sound);

                    while (frames.Count > 0)
                    {
                        frame = frames.Dequeue();

                        while ((soundClip.AudioSource.time * 1000) < frame.frameTime) await Task.Delay(1);

                        onMouthChange?.Invoke(frame.position);
                        OnVOPositionChanged?.Invoke(frame.position, sound);
                    }

                    await Task.Delay(recorder.finalFrameDelay);
                    onComplete?.Invoke();
                    OnVOCompleted?.Invoke(sound);
                }
                else
                {
                    L.Log(LogEventType.ERROR, $"No VORecorder component found on {soundClip.name}");
                    PlaySound(sound);
                    onComplete?.Invoke();
                    OnVOCompleted?.Invoke(sound);
                }
            }
            catch (Exception err)
            {
                L.Log(LogEventType.ERROR, $"{err.Message}, {@err.StackTrace}");
            }
        }


        public static bool IsPlaying(Sounds sound)
        {
            SoundClip soundClip = GetSoundClipForSound(sound);
            if (soundClip != null)
            {
                return soundClip.IsPlaying;
            }

            return false;
        }


        public static void StopSound(Sounds sound)
        {
            AudioSource audio = GetAudioSourceForSound(sound);
            if (audio != null)
            {
                audio.Stop();
            }
            else L.Log(LogEventType.ERROR, $"Audio source for {sound} is null");
        }


        /// <summary>
        /// Stop all sounds assigned to the tag name passed in.
        /// </summary>
        /// <param name="tagName"></param>
        public static void StopSound(string tagName)
        {
            var clips = GetSoundClipsForTag(tagName);

            if (clips.Count > 0)
            {
                foreach (SoundClip clip in clips)
                {
                    StopSound(clip.Sound);
                }
            }
        }

        /// <summary>
        /// Stop sound parented to this GameObject.
        /// </summary>
        /// <param name="obj"></param>
        public static void StopSound(GameObject obj)
        {
            AudioSource audio = obj.GetComponent<AudioSource>();
            if (audio == null)
            {
                audio = obj.GetComponentInChildren<AudioSource>();
                if (audio != null)
                {
                    audio.Stop();
                }
                else L.Log(LogEventType.ERROR, $"No audioSource component found on {obj.name}");
            }
        }


        /// <summary>
        /// Stop a sound by fading it out with a transition.  This requires you assign a Fade Out Snapshot to the soundclip
        /// </summary>
        /// <param name="sound"></param>
        /// <param name="duration"></param>
        public static void StopSound(Sounds sound, float duration = 2.0f)
        {
            SoundClip soundClip = GetSoundClipForSound(sound);
            if (soundClip != null && soundClip.fadeOutSnapshot != null)
            {
                soundClip.fadeOutSnapshot.TransitionTo(duration);
                soundClip.StopAfterDelay(duration);
            }
            else L.Log(LogEventType.ERROR, $"Transition not found for {sound}. SoundClip is null and likely needs to be added to a SoundLoader's list of audio clips", true);
        }


        /// <summary>
        /// Pass a List<Sounds> list to have prefab Sounds loaded.  Use SceneSounds component on a
        /// GameObject in the scene or call this directly.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="mode"></param>
        public static void LoadSoundsForScene(List<Sounds> list)
        {
            if (soundsContainer == null)
            {
                soundsContainer = new GameObject("_XLSoundManager");
                GameObject.DontDestroyOnLoad(soundsContainer);
            }

            List<Sounds> toBeAdded = new List<Sounds>();
            List<Sounds> toBeRemoved = new List<Sounds>();

            if (currentSounds.Count > 0)
            {
                // remove items in currentSounds from list
                toBeAdded = list.Except(currentSounds).ToList();
            }
            else
            {
                toBeAdded.AddRange(list);
                currentSounds.AddRange(list);
            }

            L.Log(LogEventType.STRING, $"toBeAdded: {String.Join(", ", toBeAdded)}", true);
            L.Log(LogEventType.STRING, $"currentSounds: {String.Join(", ", currentSounds)}", true);

            if (toBeAdded.Count > 0)
            {
                foreach (Sounds sound in toBeAdded)
                {
                    var p = Path.Combine(prefabsPath, sound.ToString()).Replace(@"\", "/");
#if UNITY_EDITOR
                    var g = (GameObject)PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>(p));
#else
                    var g = GameObject.Instantiate<GameObject>(Resources.Load<GameObject>(p));
#endif
                    if (g != null)
                    {
                        g.transform.SetParent(soundsContainer.transform);
                        var soundClip = g.GetComponent<SoundClip>();
                        if (soundClip != null && soundClip.tagsList.Length > 0)
                        {
                            CreateTagSounds(soundClip);
                        }
                        soundPointers.Add(sound, g);
                    }
                    else L.Log(LogEventType.ERROR, $"Failed to load {p}");

                }
            }
        }


        static void CreateTagSounds(SoundClip soundClip)
        {
            L.Log(LogEventType.SERVICE_EVENT, $"Tags string {soundClip.tagsList}, {soundClip.name}");
            string[] tags = soundClip.tagsList.Split(',');
            foreach (string s in tags)
            {
                GameObject[] gos = GameObject.FindGameObjectsWithTag(s);
                L.Log(LogEventType.SERVICE_EVENT, $"GameObjects found: {gos.Length}");

                if (gos.Length.Equals(1) && tags.Length.Equals(1))
                {
                    soundClip.transform.SetParent(gos[0].transform);
                    soundClip.transform.localPosition = Vector3.zero;
                }
                else if (gos.Length > 0)
                {
                    foreach (GameObject g in gos)
                    {
                        var p = Path.Combine(prefabsPath, soundClip.name).Replace(@"\", "/");
#if !UNITY_EDITOR
				    GameObject prefab = GameObject.Instantiate<GameObject>(Resources.Load<GameObject>(p));
#else
                        GameObject prefab = PrefabUtility.InstantiatePrefab(Resources.Load<GameObject>(p)) as GameObject;
#endif
                        prefab.transform.parent = g.transform;
                        prefab.transform.localPosition = Vector3.zero;
                    }
                }
            }
        }


        public static void RemoveSoundsForScene(List<Sounds> list)
        {
            List<Sounds> toBeRemoved = new List<Sounds>();

            if (currentSounds.Count > 0)
            {
                toBeRemoved = list.Except(currentSounds).ToList();
                L.Log(LogEventType.STRING, $"toBeRemoved: {String.Join(", ", toBeRemoved)}");
            }
            else return; // nothing to remove

            if (toBeRemoved.Count > 0)
            {
                foreach (Sounds sound in toBeRemoved)
                {
                    if (soundPointers.TryGetValue(sound, out GameObject g))
                    {
                        // TODO: remove instances added to tags?
                        GameObject.Destroy(g);
                        soundPointers.Remove(sound);
                    }
                }
            }
        }


        static List<SoundClip> GetSoundClipsForTag(string tagName)
        {
            GameObject[] gos = GameObject.FindGameObjectsWithTag(tagName);
            List<SoundClip> clips = new List<SoundClip>();

            if (gos.Length > 0)
            {
                List<GameObject> list = new List<GameObject>(gos);
                list = list.Where(x =>
                {
                    var soundClip = x.GetComponentInChildren<SoundClip>();
                    if (soundClip != null && soundClip.tagsList.Contains(tagName))
                    {
                        clips.Add(soundClip);
                        return true;
                    }
                    return false;
                }).ToList();

            }

            return clips;
        }


        static AudioSource GetAudioSourceForSound(Sounds sound)
        {
            AudioSource source = null;
            if (soundPointers.TryGetValue(sound, out GameObject g))
            {
                source = g.GetComponent<AudioSource>();
            }

            return source;
        }


        static SoundClip GetSoundClipForSound(Sounds sound)
        {
            SoundClip source = null;
            if (soundPointers.TryGetValue(sound, out GameObject g))
            {
                if (g != null) source = g.GetComponent<SoundClip>();
            }

            return source;
        }


        static Queue<VORecorderFrame> GetVOQue(List<VORecorderFrame> list)
        {
            Queue<VORecorderFrame> que = new Queue<VORecorderFrame>();

            int lastTime = 0;
            foreach (VORecorderFrame v in list)
            {
                v.span = v.frameTime - lastTime;
                lastTime = v.frameTime;
                que.Enqueue(v);
            }

            return que;
        }


        public static T GetEnumForString<T>(string value) where T : struct
        {
            if ((typeof(T).IsEnum))
            {
                foreach (T eValue in Enum.GetValues(typeof(T)))
                {
                    if (eValue.ToString().Equals(value)) return eValue;
                }
            }

            return default;
        }


        public static T GetEnumForInt<T>(int value) where T : struct
        {
            if ((typeof(T).IsEnum))
            {
                foreach (T eValue in Enum.GetValues(typeof(T)))
                {
                    if (eValue.GetHashCode().Equals(value)) return eValue;
                }
            }

            return default;
        }
    }
}

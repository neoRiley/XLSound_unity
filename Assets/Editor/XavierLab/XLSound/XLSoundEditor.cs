using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Threading.Tasks;
using System.IO;
using System;
using System.Linq;
using UnityEngine.Audio;

namespace XavierLab
{
    [InitializeOnLoad]
    public class XLSoundEditor
    {
        static XLSoundEditor()
        {
            // should run when editor starts up or change is made to class
            L.Log(LogEventType.BOOL, $"{L.Style("XL", LogEventType.ERROR)} SoundEditor Loaded");

            Task.Run(async () => { 
                await CheckFolderPaths();

                var mainMixer = Resources.Load<AudioMixer>("Mixers/Master") as AudioMixer;

                if( mainMixer == null )
                {
                    L.Log(LogEventType.ERROR, $"A 'Master.mixer' does not exist in Audio/Resources/Mixers'.  You will not be able to Mute/Unmute globally without creating one and assigning all sub mixers to it.");
                }
            });
        }

        static async Task CheckFolderPaths()
        {
            var path = Path.Combine(Application.dataPath, "Audio", "Resources", "XLSoundPrefabs");
            await CheckAndCreateDirectory(path);

            path = Path.Combine(Application.dataPath, "Audio", "Resources", "Mixers");
            await CheckAndCreateDirectory(path);

            path = Path.Combine(Application.dataPath, "Plugins", "XavierLab", "XLSoundEngine", "Scripts", "enums");
            await CheckAndCreateDirectory(path);
        }

        [MenuItem("XL Sound Engine/Create Sound Clip")]
        static async Task CreateSoundClip()
        {
            var path = Path.Combine(Application.dataPath, "Audio", "Resources", "XLSoundPrefabs");
            await CheckAndCreateDirectory(path);

            var g = new GameObject("SoundClip");
            var soundClip = g.AddComponent<SoundClip>();
            soundClip.ID = GetNextID();
            var audioSource = g.AddComponent<AudioSource>();
        }


        [MenuItem("XL Sound Engine/Update Enums")]
        static async Task UpdateEnumFile()
        {
            try
            {
                var path = Path.Combine(Application.dataPath, "Plugins", "XavierLab", "XLSoundEngine", "Scripts", "enums");
                await CheckAndCreateDirectory(path);

                var sounds = Path.Combine(Application.dataPath, "Audio", "Resources", "XLSoundPrefabs");

                List<string> files = new List<string>(Directory.GetFiles(sounds));
                files = files.Where(x => !x.Contains(".meta")).ToList();
                files = files.Select(x => $"\t{Path.GetFileNameWithoutExtension(x)}={GetIDForFile(x)}").ToList();

                var msg = String.Join(",\n", files);

                var str = $"public enum Sounds:int\n{{\n{msg}\n}}";

                File.WriteAllText(Path.Combine(path, "Sounds.cs"), str);

                AssetDatabase.Refresh();
            }
            catch(Exception err)
            {
                L.Log(LogEventType.WARN, $"{err.Message}, {err.StackTrace}");
            }
        }


        static int GetIDForFile(string path)
        {
            path = path.Replace(Application.dataPath, "Assets");
            var g = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var soundClip = g.GetComponent<SoundClip>();
            return soundClip.ID;
        }


        public static async Task CreatePrefab(GameObject source)
        {
            try
            {
                var path = Path.Combine(Application.dataPath, "Audio", "Resources", "XLSoundPrefabs");
                await CheckAndCreateDirectory(path);

                bool created = false;
                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(source, Path.Combine(path, source.name + ".prefab").Replace(@"\", "/"), out created);
                GameObject.DestroyImmediate(source);
                L.Log(LogEventType.BOOL, $"prefab created: {created}");
            }
            catch (Exception err)
            {
                L.Log(LogEventType.WARN, $"error: {err.Message}, {err.StackTrace}");
            }
        }


        static async Task CheckAndCreateDirectory(string dirToCreate)
        {
            await Task.Run(() => 
            { 
                if (!Directory.Exists(dirToCreate)) Directory.CreateDirectory(dirToCreate);
            });

            return;
        }


        static int GetNextID()
        {
            int id = 0;
            if( PlayerPrefs.HasKey("SoundClipIDSource") )
            {
                id = PlayerPrefs.GetInt("SoundClipIDSource") + 1;
            }

            PlayerPrefs.SetInt("SoundClipIDSource", id);

            return id;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using XavierLab;

public class GlobalSettings : MonoBehaviour {

	public static INIParser _parser;
    static string rootDir = "";
    static string folderName = "";
    static string initPath = "";

    /// <summary>
    /// InitPath uses Directory.GetCurrentDirectory() instead of Application.dataPath because
    /// Application.dataPath needs to run on the Main thread and this MIGHT be called from a Task
    /// running on it's own thread
    /// </summary>
    public static string InitPath
    {
        get
        {
            rootDir = Directory.GetCurrentDirectory();
#if UNITY_EDITOR
            initPath = Path.Combine(rootDir, "Assets", "StreamingAssets", "init");
#else
            folderName = new DirectoryInfo(rootDir).Name;
            initPath = Path.Combine(rootDir, "../_init", folderName);
            CheckAndCopy();
#endif
            return initPath;
        }
    }


    public static INIParser parser
    {
		get
        {
			if(_parser == null)
            {
                
                _parser = new INIParser();
                _parser.Open(Path.Combine(InitPath, "settings.ini"));
            }
			return _parser;
		}
	}
    

    public static void Reset()
    {
		_parser = null;
	}

    /// <summary>
    /// If we're in the editor, we'll use the files in StreamingAssets/init, since
    /// we may want to introduce new settings or changed settings that the other end
    /// needs to adopt.
    /// </summary>
    static void CheckAndCopy()
    {
        if (!Directory.Exists(initPath))
        {
            L.Log(LogEventType.METHOD, $"Path does not exist - creating now: {initPath}", true);
            Directory.CreateDirectory(initPath);            
        }

        var dataFolder = new List<string>(Directory.GetDirectories(rootDir)).Where(x => x.ToLower().Contains("_data")).ToList()[0];
        List<string> files = new List<string>(Directory.GetFiles(Path.Combine(dataFolder, "StreamingAssets", "init")));

        files = files.Where(x => !x.Contains(".meta")).ToList();
        foreach (string file in files)
        {
            var dest = Path.Combine(initPath, Path.GetFileName(file));
            if( !File.Exists(dest) ) File.Copy(file, dest);
        }
    }
}
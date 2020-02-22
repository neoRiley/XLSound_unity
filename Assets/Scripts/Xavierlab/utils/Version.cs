using UnityEngine;

public class Version : MonoBehaviour 
{
    public static string AppVersion
    {
        get
        {
            return Version.instance.major + "." + Version.instance.minor + "." + Version.instance.revision + "." + Version.instance.dateVersion;
        }
    }

    public static Version instance;

    public int major = 0;
    public int minor = 0;
    public int revision = 0;
    public string dateVersion = "201903080633";

    private void Awake()
    {
        instance = this;
    }


    void Start () 
    {
        Debug.Log("<color=teal><b>App Version: </b></color>" + Version.AppVersion);
        CreateVersionFile(Version.AppVersion);
	}
	
	void CreateVersionFile(string value)
    {
        System.IO.File.WriteAllText(Application.streamingAssetsPath + "/_version.txt", value);
    }
}

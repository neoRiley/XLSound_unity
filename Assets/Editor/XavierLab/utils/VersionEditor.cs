using UnityEngine;
using UnityEditor;
using System.Collections;
using XavierLab;

[CustomEditor(typeof(Version))]
public class VersionEditor : Editor
{
    SerializedProperty major;
    SerializedProperty minor;
    SerializedProperty revision;
    SerializedProperty dateVersion;

    private void OnEnable()
    {
        major = serializedObject.FindProperty("major");
        minor = serializedObject.FindProperty("minor");
        revision = serializedObject.FindProperty("revision");
        dateVersion = serializedObject.FindProperty("dateVersion");
    }


    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        EditorGUILayout.PropertyField(major);
        EditorGUILayout.PropertyField(minor);
        EditorGUILayout.PropertyField(revision);
        EditorGUILayout.PropertyField(dateVersion);        

        if( GUILayout.Button("Update Date Version & Save"))
        {
            dateVersion.stringValue = ConversionUtils.GetVersionDate();
            string v = major.intValue + "." + minor.intValue + "." + revision.intValue + "." + dateVersion.stringValue;
            Debug.Log($"Version updated: {v}");
            WriteVersionFile(v);
        }

        serializedObject.ApplyModifiedProperties();
    }

    
    protected void WriteVersionFile(string value)
    {
        System.IO.File.WriteAllText(Application.streamingAssetsPath + "/_version.txt", value);
    }
}

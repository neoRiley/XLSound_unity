using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using XavierLab;

public class ApplicationLogger
{
    string filePath = "";
    
    public ApplicationLogger()
    {
#if !UNITY_EDITOR
        filePath = Path.Combine(GlobalSettings.InitPath, "DebugOutput_current.txt");

        try
        {
            Task.Run(async () => { 
                await SwapCurrentAndPrevious();

                Application.logMessageReceived += (condition, stackTrace, type) =>
                {
                    var msg = $"[{DateTime.Now.ToString("HH:mm:ss.ffff")}]{condition}\n{stackTrace}\n";
                    File.AppendAllText(filePath, msg);
                };
            });
        }
        catch (Exception err)
        {
            L.Log(LogEventType.ERROR, $"Error: {err.Message} :: {err.StackTrace}");
        } 
#endif
    }


    async Task SwapCurrentAndPrevious()
    {
        if( File.Exists(filePath) )
        {
            try
            { 
                var previous = Path.Combine(GlobalSettings.InitPath, "DebugOutput_previous.txt");
                if (File.Exists(previous)) File.Delete(previous);
                while (File.Exists(previous)) await Task.Delay(5);
                File.Move(filePath, previous);
            }
            catch (Exception err)
            {
                L.Log(LogEventType.ERROR, $"Error: {err.Message} :: {err.StackTrace}");
            }
        }
    }


    public string GetVersionDateTime(bool includeSeconds = false)
    {
        DateTime now = DateTime.Now;
        string dateValue = now.ToString("yyyyMMddHHmm");
        if (includeSeconds) dateValue = now.ToString("yyyyMMddHHmm.ss.fff");
        return dateValue;
    }
}

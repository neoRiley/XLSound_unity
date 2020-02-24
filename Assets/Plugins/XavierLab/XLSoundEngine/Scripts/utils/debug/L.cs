using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;


namespace XavierLab
{
    public enum LogEventType
    {
        NORMAL,
        EVENT,
        METHOD,
        PROPERTY,
        FLOAT,
        INT,
        STRING,
        SERVICE_EVENT,
        BOOL,
        ERROR,
        WARN
    }

    [Serializable]
    public struct LogTypes
    {
        public LogEventType logType;
        public Color color;
    }

    [ExecuteInEditMode]
    public sealed class Clrs
    {
        private Clrs() { }

        public static readonly string WHITE = "#FFFFFF";
        public static readonly string RED = "#FF0000";
        public static readonly string BLACK = "#000000";
        public static readonly string BLUE = "#0000FF";
        public static readonly string BLUE_RICH = "#005599";
        public static readonly string BLUE_SKY = "#00AAFF";
        public static readonly string BABY_BLUE = "#AFF8FF";
        public static readonly string STRONG_BLUE = "#4984FF";
        public static readonly string GREEN = "#00FF00";
        public static readonly string LIME_GREEN = "#E4FF00";
        public static readonly string ORANGE = "orange";
        public static readonly string BURNT_ORANGE = "#FFBE33";
        public static readonly string TEAL = "teal";
        public static readonly string YELLOW = "yellow";
        public static readonly string MAGENTA = "magenta";
        public static readonly string PINK = "#FF00F3";
    }

    [ExecuteInEditMode]
    public class L
    {
        private static LogTypes[] logTypesList;
        public static LogTypes[] LogTypesList
        {
            get
            {
                if (logTypesList == null || logTypesList.Length < 1) logTypesList = FillLogTypesWithDefaults();
                return logTypesList;
            }

            set
            {
                logTypesList = value;
            }
        }

        static List<string> colors = new List<string>();
        static Dictionary<string, string> classColors = new Dictionary<string, string>();


        public static void Log(LogEventType eventType, string msg, bool bold = false, bool italics = false, [CallerFilePath] string memberName = "", [CallerLineNumber] int lineNumber = 0)
        {
            foreach (LogTypes lts in LogTypesList)
            {
                if (lts.logType.Equals(eventType))
                {
                    string c = GetColorForLogEventType(eventType);
                    msg = Style(msg, c, bold, italics);
                    break;
                }
            }

            Log(msg, "random", bold, italics, memberName, lineNumber);
        }


        public static void Log(string msg, string clr = "random", bool bold = false, bool italics = false, [CallerFilePath] string memberName = "", [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string callerName = "")
        {
            if (clr.Equals("random")) clr = GetClassColor(GetClassName(memberName));

            string title = Style($"[{GetClassName(memberName)}: {lineNumber}]", clr, true, false);
            msg = Style(msg, "#cccccc", bold, italics);

            Debug.Log($"{title} {msg}");
        }


        public static string Style(string msg, LogEventType eventType, bool bold = false, bool italics = false)
        {
            string c = GetColorForLogEventType(eventType);
            return Style(msg, c, bold, italics);
        }


        public static string Style(string msg, string clr, bool bold = false, bool italics = false)
        {
            msg = $"<color={clr}>{msg}</color>";
            if (bold) msg = $"<b>{msg}</b>";
            if (italics) msg = $"<i>{msg}</i>";

            return msg;
        }

        
        public static void SetClassToColor(string clr, [CallerFilePath] string memberName = "")
        {
            string className = GetClassName(memberName);

            if (!classColors.ContainsKey(className))
            {
                if (!clr.Contains("#")) clr = "#" + clr;
                classColors.Add(className, clr);
            }
        }


        //public static void GenerateRandomColors()
        //{
        //    int n = 30;
        //    colors = new List<string>();
        //    for (int i = 0; i < n; i++)
        //    {
        //        string clr = "#" + ColorUtility.ToHtmlStringRGB(UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f));
        //        colors.Add(clr);
        //    }
        //}


        public static LogTypes[] FillLogTypesWithDefaults()
        {
            var list = new LogTypes[11];
            var i = 0;
            foreach (LogEventType eventType in Enum.GetValues(typeof(LogEventType)))
            {
                list[i] = GetLogTypesForEventType(eventType);
                i++;
            }

            return list;
        }


        public static string GetClassName(string name)
        {
            int index = name.LastIndexOf('/');
            if (index.Equals(-1)) index = name.LastIndexOf('\\');
            name = name.Replace(".cs", "");
            return name.Substring(index + 1);
        }


        static string GetClassColor(string className)
        {
            string clr = "#FFFFFF";
            if (classColors.ContainsKey(className)) classColors.TryGetValue(className, out clr);
            return clr;
        }       


        static string GetColorForLogEventType(LogEventType eventType)
        {
            string clr = Clrs.WHITE;

            foreach (LogTypes lts in LogTypesList)
            {
                if (lts.logType.Equals(eventType))
                {
                    clr = "#" + ColorUtility.ToHtmlStringRGB(lts.color);
                }
            }

            return clr;
        }


        static LogTypes GetLogTypesForEventType(LogEventType eventType)
        {
            Color clr = Color.white;

            switch (eventType)
            {
                case LogEventType.BOOL:
                    ColorUtility.TryParseHtmlString("#23B9FF", out clr);
                    break;

                case LogEventType.EVENT:
                    ColorUtility.TryParseHtmlString("#D0FF45", out clr);
                    break;

                case LogEventType.FLOAT:
                    ColorUtility.TryParseHtmlString("#FF8000", out clr);
                    break;

                case LogEventType.INT:
                    ColorUtility.TryParseHtmlString("#FF8000", out clr);
                    break;

                case LogEventType.METHOD:
                    ColorUtility.TryParseHtmlString("#E281FF", out clr);
                    break;

                case LogEventType.NORMAL:
                    ColorUtility.TryParseHtmlString("#CFCFCF", out clr);
                    break;

                case LogEventType.PROPERTY:
                    ColorUtility.TryParseHtmlString("#FF00CE", out clr);
                    break;

                case LogEventType.SERVICE_EVENT:
                    ColorUtility.TryParseHtmlString("#00F8FF", out clr);
                    break;

                case LogEventType.STRING:
                    ColorUtility.TryParseHtmlString("#00FF15", out clr);
                    break;

                case LogEventType.ERROR:
                    ColorUtility.TryParseHtmlString("#FF0000", out clr);
                    break;

                case LogEventType.WARN:
                    ColorUtility.TryParseHtmlString("#FFA300", out clr);
                    break;
            }

            var lt = new LogTypes
            {
                logType = eventType,
                color = clr
            };

            return lt;
        }
    }
}

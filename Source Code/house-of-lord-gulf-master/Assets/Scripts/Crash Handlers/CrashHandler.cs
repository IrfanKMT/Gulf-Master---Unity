using System.IO;
using UnityEngine;

public class CrashHandler : MonoBehaviour
{
    void Awake()
    {
        DontDestroyOnLoad(this);
        Application.logMessageReceived += HandleLog;
    }

    void OnApplicationQuit()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        File.AppendAllText(Path.Join(Application.persistentDataPath,"/CrashReport.txt") , "################################### LOG STRING ###################################\n\n" + logString + "################################### STACKTRACE ###################################\n\n" + stackTrace);
    }
}

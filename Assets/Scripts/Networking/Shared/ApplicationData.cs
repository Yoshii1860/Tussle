using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class ApplicationData
{
    private static string ip = null;
    private static int port = 0;
    private static int queryPort = 0;
    private static string mode = null;
    private static string logPath = null;
    private static bool logHandlerRegistered = false;

    private Dictionary<string, Action<string>> m_CommandDictionary = new Dictionary<string, Action<string>>(StringComparer.OrdinalIgnoreCase);

    const string k_IPCmd = "ip";
    const string k_PortCmd = "port";
    const string k_QueryPortCmd = "queryport";
    const string k_ModeCmd = "mode";
    const string k_LogCmd = "log";

    public static string IP()
    {
        return ip ?? (Application.isEditor ? "172.27.205.68" : "127.0.0.1"); // Default to WSL2 IP in Editor
    }

    public static int Port()
    {
        return port != 0 ? port : 7777;
    }

    public static int QPort()
    {
        return queryPort != 0 ? queryPort : 7787;
    }

    public static string Mode()
    {
        return mode ?? (Application.isEditor ? "client" : "server");
    }

    public static string LogPath()
    {
        return logPath;
    }

    public ApplicationData()
    {
        if (logHandlerRegistered) { return; }

        Debug.unityLogger.logHandler = new UnityLogHandler(Debug.unityLogger.logHandler);

        m_CommandDictionary["-" + k_IPCmd] = SetIP;
        m_CommandDictionary["-" + k_PortCmd] = SetPort;
        m_CommandDictionary["-" + k_QueryPortCmd] = SetQueryPort;
        m_CommandDictionary["-" + k_ModeCmd] = SetMode;
        m_CommandDictionary["-" + k_LogCmd] = SetLogPath;

        ProcessCommandLinearguments(Environment.GetCommandLineArgs());
        ValidateMultiplayArguments();

        logHandlerRegistered = true;
    }

    void ProcessCommandLinearguments(string[] args)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Launch Args:");
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            var nextArg = i + 1 < args.Length ? args[i + 1] : null;

            if (EvaluatedArgs(arg, nextArg))
            {
                sb.Append(arg);
                sb.Append(" : ");
                sb.AppendLine(nextArg);
                i++;
            }
        }
        Debug.Log(sb.ToString());
    }

    bool EvaluatedArgs(string arg, string nextArg)
    {
        if (!IsCommand(arg))
            return false;
        if (nextArg == null || IsCommand(nextArg))
            return false;

        m_CommandDictionary[arg].Invoke(nextArg);
        return true;
    }

    void SetIP(string ipArgument)
    {
        ip = ipArgument;

#if UNITY_SERVER
        Debug.Log($"ApplicationData: Set IP to: {ip}");
#endif

    }

    void SetPort(string portArgument)
    {
        if (int.TryParse(portArgument, out int parsedPort))
        {
            port = parsedPort;
            if (Mode() == "server")
                Debug.Log($"ApplicationData: Set Port to: {port}");
        }
        else
        {
            Debug.LogError($"ApplicationData: {portArgument} does not contain a parseable port!");
        }
    }

    void SetQueryPort(string qPortArgument)
    {
        if (int.TryParse(qPortArgument, out int parsedQPort))
        {
            queryPort = parsedQPort;
            if (Mode() == "server")
                Debug.Log($"ApplicationData: Set QueryPort to: {queryPort}");
        }
        else
        {
            Debug.LogError($"ApplicationData: {qPortArgument} does not contain a parseable query port!");
        }
    }

    void SetMode(string modeArgument)
    {
        if (modeArgument == "host" || modeArgument == "client" || modeArgument == "server")
        {
            mode = modeArgument;
            Debug.Log($"ApplicationData: Set Mode to: {mode}");
        }
        else
        {
            Debug.LogWarning($"ApplicationData: Invalid mode: {modeArgument}. Using default: client");
            mode = "client";
        }
    }

    void SetLogPath(string logPathArgument)
    {
        logPath = logPathArgument;

#if UNITY_SERVER
        Debug.Log($"ApplicationData: Set Log Path to: {logPath}");
#endif

    }

    bool IsCommand(string arg)
    {
        return !string.IsNullOrEmpty(arg) && m_CommandDictionary.ContainsKey(arg) && arg.StartsWith("-");
    }

    void ValidateMultiplayArguments()
    {
        if (mode != "server")
            return;

        bool hasRequiredArgs = true;
        if (string.IsNullOrEmpty(ip))
        {
            Debug.LogError("ApplicationData: Multiplay Hosting: Missing required argument '-ip'. Server will not function correctly.");
            hasRequiredArgs = false;
        }
        if (port == 0)
        {
            Debug.LogError("ApplicationData: Multiplay Hosting: Missing required argument '-port'. Server will not function correctly.");
            hasRequiredArgs = false;
        }
        if (queryPort == 0)
        {
            Debug.LogError("ApplicationData: Multiplay Hosting: Missing required argument '-queryport'. Server will not function correctly.");
            hasRequiredArgs = false;
        }

        if (!hasRequiredArgs)
        {
            Debug.LogWarning("ApplicationData: Falling back to default values, but this may cause issues in Multiplay Hosting.");
        }
    }

    private class UnityLogHandler : ILogHandler
    {
        private ILogHandler defaultLogHandler;

        public UnityLogHandler(ILogHandler defaultLogHandler)
        {
            this.defaultLogHandler = defaultLogHandler;
        }

        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            defaultLogHandler.LogFormat(logType, context, format, args);
            Console.WriteLine(string.Format(format, args));
        }

        public void LogException(Exception exception, UnityEngine.Object context)
        {
            defaultLogHandler.LogException(exception, context);
            Console.WriteLine(exception.ToString());
        }
    }
}
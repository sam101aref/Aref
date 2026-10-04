using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Arash.Core
{
    /// <summary>Where analytics events and crash reports go, e.g. Firebase Analytics + Crashlytics.</summary>
    public interface ITelemetryBackend
    {
        void LogEvent(string name, IDictionary<string, object> parameters);
        void LogException(string message, string stackTrace);
    }

    /// <summary>
    /// Analytics and crash reporting (F-37). Events and uncaught exceptions are always written to a
    /// small rotating log on the device (persistentDataPath/telemetry.log), which is useful for bug
    /// reports. Nothing leaves the device unless a <see cref="Backend"/> is installed.
    /// </summary>
    public static class Telemetry
    {
        const string FileName = "telemetry.log";
        const long MaxBytes = 256 * 1024;

        /// <summary>Optional remote backend; null keeps everything local.</summary>
        public static ITelemetryBackend Backend { get; set; }

        static bool initialized;
        static string path;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Initialize()
        {
            if (initialized)
                return;
            initialized = true;
            path = Path.Combine(Application.persistentDataPath, FileName);
            Application.logMessageReceived += OnLog;
            Event("app_start", "version", Application.version, "platform", Application.platform.ToString());
        }

        /// <summary>Logs an event with name/value pairs: Event("level_end", "won", true, "stars", 3).</summary>
        public static void Event(string name, params object[] pairs)
        {
            var parameters = new Dictionary<string, object>();
            for (var i = 0; i + 1 < pairs.Length; i += 2)
                parameters[Convert.ToString(pairs[i])] = pairs[i + 1];

            Write("event " + name + " " + Format(parameters));
            if (Backend != null)
            {
                try
                {
                    Backend.LogEvent(name, parameters);
                }
                catch (Exception e)
                {
                    Write("backend error " + e.Message);
                }
            }
        }

        public static string Format(IDictionary<string, object> parameters)
        {
            var builder = new StringBuilder();
            foreach (var pair in parameters)
            {
                if (builder.Length > 0)
                    builder.Append(' ');
                builder.Append(pair.Key).Append('=').Append(pair.Value);
            }
            return builder.ToString();
        }

        static void OnLog(string message, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Assert)
                return;
            Write("exception " + message + "\n" + stackTrace);
            if (Backend != null)
            {
                try
                {
                    Backend.LogException(message, stackTrace);
                }
                catch (Exception)
                {
                    // never let reporting crash the game
                }
            }
        }

        static void Write(string line)
        {
            if (path == null)
                return;
            try
            {
                if (File.Exists(path) && new FileInfo(path).Length > MaxBytes)
                    File.Delete(path);
                File.AppendAllText(path, DateTime.UtcNow.ToString("u") + " " + line + "\n");
            }
            catch (Exception)
            {
                // the log is best-effort
            }
        }
    }
}

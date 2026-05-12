using UnityEngine;
using System.Collections.Generic;

namespace BrmnModules.Common {
    public class ScreenDebugger : MonoBehaviour
    {
        private static ScreenDebugger Instance;
        private List<string> logs = new();
        private GUIStyle style;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Application.logMessageReceived += OnLog;
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            string prefix = type == LogType.Error ? "<color=red>[ERR]</color>" :
                            type == LogType.Warning ? "<color=yellow>[WRN]</color>" : "[LOG]";
            logs.Add($"{prefix} {condition}");
            if (logs.Count > 20) logs.RemoveAt(0);
        }

        private void OnGUI()
        {
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 18,
                    richText = true
                };
                style.normal.background = MakeBackground(2, 2, new Color(0, 0, 0, 0.6f));
            }

            float y = 10f;
            foreach (string log in logs)
            {
                GUI.Label(new Rect(10, y, Screen.width - 20, 24), log, style);
                y += 24f;
            }
        }

        private Texture2D MakeBackground(int w, int h, Color c)
        {
            var tex = new Texture2D(w, h);
            for (int x = 0; x < w; x++) {
                for (int y = 0; y < h; y++) tex.SetPixel(x, y, c);
            }
            tex.Apply();
            return tex;
        }
    }
}
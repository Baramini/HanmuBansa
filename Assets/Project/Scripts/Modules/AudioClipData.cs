using UnityEngine;
using System.Collections.Generic;

namespace BrmnModules.Audio
{
    [System.Serializable]
    public class AudioClipEntry
    {
        public string key;
        public AudioClip clip;
    }

    public abstract class AudioClipData : ScriptableObject
    {
        [SerializeField] private List<AudioClipEntry> entries;

        private Dictionary<string, AudioClip> dict;

        private void OnEnable()
        {
            dict = null;
        }

        private void BuildDict()
        {
            dict = new Dictionary<string, AudioClip>();
            if (entries == null) return;

            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry.key) || entry.clip == null) continue;
                dict[entry.key] = entry.clip;
            }
        }

        public AudioClip Get(string key)
        {
            if (dict == null || dict.Count == 0) BuildDict();
            return dict.TryGetValue(key, out AudioClip clip) ? clip : null;
        }

        public bool Has(string key)
        {
            if (dict == null) BuildDict();
            return dict.ContainsKey(key);
        }
    }
}
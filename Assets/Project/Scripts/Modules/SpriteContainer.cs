using UnityEngine;
using System.Collections.Generic;

// Generic sprite container base class.
// Uses Dictionary for flexible key-based and index-based lookup.

namespace BrmnModules.DataManagement
{
    public abstract class SpriteContainer : MonoBehaviour
    {
        [SerializeField] private List<SpriteEntry> entries;

        [System.Serializable]
        public class SpriteEntry
        {
            public string key;
            public Sprite sprite;
        }

        private Dictionary<string, Sprite> _spriteDict;
        private List<Sprite> _spriteList;

        protected virtual void Awake()
        {
            _spriteDict = new Dictionary<string, Sprite>();
            _spriteList = new List<Sprite>();

            foreach (var entry in entries)
            {
                if (entry.sprite == null) continue;
                _spriteDict[entry.key] = entry.sprite;
                _spriteList.Add(entry.sprite);
            }
        }

        // -- Get by index (existing usage) --
        public Sprite GetSprite(int index)
        {
            if (index < 0 || _spriteList == null || index >= _spriteList.Count)
                return null;
            return _spriteList[index];
        }

        // -- Get by key --
        public Sprite GetSprite(string key)
        {
            if (_spriteDict == null || !_spriteDict.ContainsKey(key))
                return null;
            return _spriteDict[key];
        }

        // -- Check if key exists --
        public bool HasSprite(string key) => _spriteDict?.ContainsKey(key) ?? false;

        public int Count => _spriteList?.Count ?? 0;
    }
}
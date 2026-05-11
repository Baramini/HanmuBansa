using UnityEngine;
using System.Collections.Generic;

namespace BrmnModules.DataManagement
{
    public abstract class SpriteContainer : MonoBehaviour
    {
        [SerializeField] private List<SpriteEntry> entries;

        [System.Serializable] public class SpriteEntry
        {
            public string key;
            public Sprite sprite;
        }

        private Dictionary<string, Sprite> spriteDict;
        private List<Sprite> spriteList;

        protected virtual void Awake()
        {
            spriteDict = new Dictionary<string, Sprite>();
            spriteList = new List<Sprite>();

            foreach (var entry in entries)
            {
                if (entry.sprite == null) continue;
                spriteDict[entry.key] = entry.sprite;
                spriteList.Add(entry.sprite);
            }
        }

        // Get by index
        public Sprite GetSprite(int index)
        {
            if (index < 0 || spriteList == null || index >= spriteList.Count) return null;
            return spriteList[index];
        }

        // Get by key
        public Sprite GetSprite(string key)
        {
            if (spriteDict == null || !spriteDict.ContainsKey(key)) return null;
            return spriteDict[key];
        }

        // Find by Key
        public bool HasSprite(string key) => spriteDict?.ContainsKey(key) ?? false;

        public int Count => spriteList?.Count ?? 0;
    }
}
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using MyToolz.DataStructures;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MyToolz.Player.FPS
{

    [CreateAssetMenu(fileName = "AssetDatabaseMapping", menuName = "Utilities/Asset database dictionaryMapping")]
    public class AssetDatabaseMappingSO : ScriptableObject
    {
        [SerializeField] private List<GUIDAssetEntry> AssetEntries = new List<GUIDAssetEntry>();
        //private Dictionary<string, GameObject> entriesMapping;
        private BiDictionary<string, GameObject> entriesMapping;

        [Button("Sort inactiveList")]
        public void SortEntries()
        {
            AssetEntries.Sort((a, b) => a.Guid.CompareTo(b.Guid));
        }


        public bool TryGetPrefab(string guid, out GameObject gameObject)
        {
            if (entriesMapping == null)
            {
                InitializeAssetMap();
            }
            return entriesMapping.TryGetValue(guid, out gameObject);
        }

        public void InitializeAssetMap()
        {
            if (!(entriesMapping == null)) return;
            entriesMapping = new BiDictionary<string, GameObject>();
            foreach (var entry in AssetEntries)
            {
                if (entry.Asset != null)
                {
                    entriesMapping.TryAdd(entry.Guid, entry.Asset);
                }
            }
        }

        public bool TryGetGuid(GameObject gameObject, out string guid)
        {
            if (entriesMapping == null)
            {
                InitializeAssetMap();
            }
            return entriesMapping.TryGetValue(gameObject, out guid);
        }

#if UNITY_EDITOR
        private void Awake()
        {
            LightRefresh();
        }

        [Button("Light refresh")]
        public void LightRefresh()
        {
            foreach (var entry in AssetEntries)
            {
                if (string.IsNullOrEmpty(entry.Guid) && entry.Asset != null)
                {
                    string assetPath = AssetDatabase.GetAssetPath(entry.Asset);

                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        entry.Guid = AssetDatabase.AssetPathToGUID(assetPath);
                    }
                }
            }
            EditorUtility.SetDirty(this);
        }

        [Button("Force refresh")]
        public void ForceRefresh()
        {
            List<GUIDAssetEntry> toRemove = new List<GUIDAssetEntry>();
            foreach (var entry in AssetEntries)
            {
                if (entry.Asset != null)
                {
                    string assetPath = AssetDatabase.GetAssetPath(entry.Asset);

                    if (!string.IsNullOrEmpty(assetPath))
                    {
                        entry.Guid = AssetDatabase.AssetPathToGUID(assetPath);
                    }
                }
                else
                {
                    toRemove.Add(entry);
                }
            }
            AssetEntries.RemoveAll((a) => toRemove.Contains(a));
            EditorUtility.SetDirty(this);
        }
#endif
    }

    [System.Serializable]
    public class GUIDAssetEntry
    {
        public string Guid;
        public GameObject Asset;
    }

    public class AssetEntryGUIDComparer : IComparer<GUIDAssetEntry>
    {
        public int Compare(GUIDAssetEntry x, GUIDAssetEntry y)
        {
            return x.Guid.CompareTo(y.Guid);
        }
    }
}
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "SFXLibrary", menuName = "Audio/SFX Library")]
public class SFXLibrary : ScriptableObject
{
    [Serializable]
    public class SFXEntry
    {
        public SFXType type;
        public AudioClip[] clips; // Несколько вариантов для разнообразия
    }

    [SerializeField] private SFXEntry[] entries;

    public AudioClip GetClip(SFXType type)
    {
        foreach (var entry in entries)
        {
            if (entry.type == type && entry.clips != null && entry.clips.Length > 0)
            {
                return entry.clips[UnityEngine.Random.Range(0, entry.clips.Length)];
            }
        }
        
        Debug.LogWarning($"[SFXLibrary] Clip not found: {type}");
        return null;
    }
}
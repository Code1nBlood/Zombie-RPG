using UnityEngine;
using System;

[CreateAssetMenu(fileName = "MusicLibrary", menuName = "Audio/Music Library")]
public class MusicLibrary : ScriptableObject
{
    [Serializable]
    public class MusicEntry
    {
        public MusicType type;
        public AudioClip clip;
    }

    [SerializeField] private MusicEntry[] entries;

    public AudioClip GetClip(MusicType type)
    {
        foreach (var entry in entries)
        {
            if (entry.type == type)
                return entry.clip;
        }
        
        Debug.LogWarning($"[MusicLibrary] Music not found: {type}");
        return null;
    }
}
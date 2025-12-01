using System.Collections.Generic;
using UnityEngine;

public class InventoryData : MonoBehaviour
{
    public static InventoryData Instance { get; private set; }
    
    [System.Serializable]
    public class SaveData
    {
        public List<string> collectedPotionNames = new List<string>();
        public List<string> collectedBoostNames = new List<string>();
        public string[] potionSlotNames = new string[2];
        public string[] boostSlotNames = new string[3];
        public int[] boostMatchesRemaining = new int[3];
    }
    
    public SaveData currentData = new SaveData();
    public List<Potion> allPotions;
    public List<Boost> allBoosts;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
    }
    
    public void ClearAllData()
    {
        currentData.collectedPotionNames.Clear();
        currentData.collectedBoostNames.Clear();
        for (int i = 0; i < currentData.potionSlotNames.Length; i++) currentData.potionSlotNames[i] = null;
        for (int i = 0; i < currentData.boostSlotNames.Length; i++) currentData.boostSlotNames[i] = null;
        for (int i = 0; i < currentData.boostMatchesRemaining.Length; i++) currentData.boostMatchesRemaining[i] = 0;
    }

     public Potion GetPotionByName(string name)
    {
        return allPotions.Find(p => p.potionName == name);
    }

    public Boost GetBoostByName(string name)
    {
        return allBoosts.Find(b => b.boostName == name);
    }    
}

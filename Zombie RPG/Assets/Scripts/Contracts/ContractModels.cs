using System;
using UnityEngine;

public enum ContractType
{
    KillZombies,
    Headshots,
    SurviveLowHP
}

[Serializable]
public class Contract
{
    public string Id;
    public string Name;
    public string Description;
    public string RewardText;
    public int Cost;
    
    public ContractType Type;
    public int TargetAmount; // Сколько нужно сделать 
    public int CurrentProgress; // Сколько сделано

    public bool IsCompleted => CurrentProgress >= TargetAmount;

    public Contract(string name, string desc, string reward, int cost, ContractType type, int target)
    {
        Id = Guid.NewGuid().ToString();
        Name = name;
        Description = desc;
        RewardText = reward;
        Cost = cost;
        Type = type;
        TargetAmount = target;
        CurrentProgress = 0;
    }
}
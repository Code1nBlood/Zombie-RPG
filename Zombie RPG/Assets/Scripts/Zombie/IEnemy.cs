using UnityEngine;

public interface IEnemy
{
    int ExperienceReward { get; }
    void Die();
}
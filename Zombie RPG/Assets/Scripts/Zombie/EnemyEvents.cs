using UnityEngine;

public static class EnemyEvents
{
    public static event System.Action<IEnemy> OnEnemyKilled;

    public static void EnemyKilled(IEnemy enemy)
    {
        OnEnemyKilled?.Invoke(enemy);
    }
}
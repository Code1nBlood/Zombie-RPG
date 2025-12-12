using UnityEngine;

public class Noise : MonoBehaviour
{
    [Header("Шум")]
    public float baseRadius = 50f;      // Базовый радиус
    public float noiseCooldown = 0.3f;  // КД между шумами
    public LayerMask zombieMask;        // Слой зомби

    private float lastNoiseTime = -999f;

    /// <summary>
    /// power = множитель громкости (0.2f - тихо, 3f - ОЧЕНЬ громко)
    /// </summary>
    public void makeNoise(float power)
    {
        if (Time.time - lastNoiseTime < noiseCooldown) return;
        lastNoiseTime = Time.time;

        float radius = baseRadius * power;
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, zombieMask);

        foreach (var hit in hits)
        {
            ZombieAi zombie = hit.GetComponent<ZombieAi>();
            if (zombie != null)
            {
                // Debug.Log("Зомби услышал шум");
                zombie.OnHeardNoise(transform.position);
            }
        }

    }
}

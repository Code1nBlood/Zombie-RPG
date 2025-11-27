using UnityEngine;

public class Noise : MonoBehaviour
{
    float noiseRadius =50f;
    float noiseCooldown = 0.3f;
    float lastNoiseTime = 0f;

    public void makeNoise(float power)
    {
        if(Time.time-lastNoiseTime<noiseCooldown)return;
        lastNoiseTime = Time.time;
        float radius = noiseRadius*power;
        Collider[] hits = Physics.OverlapSphere(transform.position, radius);
        foreach(var hit in hits)
        {
            ZombieAi zombie = hit.GetComponent<ZombieAi>();
            if (zombie != null)
            {
                print("Зомби найден!");
                zombie.OnHeardNoise(transform.position);
            }
        }
    }
}

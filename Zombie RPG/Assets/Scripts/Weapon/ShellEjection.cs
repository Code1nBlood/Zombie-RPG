using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShellEjection : MonoBehaviour
{
    
    [Header("Траектория")]
    public float ejectionForce = 3f;
    
    public Vector3 ejectionDirection = new Vector3(0.5f, 0.5f, 0f); 
    
    [Header("Жизненный цикл")]
    public float lifetime = 5f;
    public float minTorque = 5f; 
    public float maxTorque = 15f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        
        Destroy(gameObject, lifetime); 
    }
    public void Launch(float forceOverride)
    {
        if (forceOverride > 0)
        {
            ejectionForce = forceOverride;
        }

        rb.AddForce(transform.TransformDirection(ejectionDirection) * ejectionForce, ForceMode.Impulse);
        
        float torque = Random.Range(minTorque, maxTorque);
        rb.AddTorque(Random.insideUnitSphere * torque, ForceMode.Impulse);
    }
}
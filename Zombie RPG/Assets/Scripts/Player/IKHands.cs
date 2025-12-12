using UnityEngine;

public class IKHands : MonoBehaviour
{
    public Transform weaponSocket;      // закинь сюда WeaponSocket
    public GameObject weaponPrefab;     // префаб твоего makarov

    void Start()
    {
        var gun = Instantiate(weaponPrefab, weaponSocket);
        gun.transform.localPosition = Vector3.zero;
        gun.transform.localRotation = Quaternion.identity;
    }
}

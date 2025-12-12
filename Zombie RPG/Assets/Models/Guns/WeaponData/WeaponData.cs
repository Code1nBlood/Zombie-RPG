using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Weapon/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Базовые характеристики")]
    public string weaponName = "Pistol";
    public int maxAmmoInClip = 8;
    public float damage = 10f;
    public float fireRate = 2f; // Выстрелов в секунду
    public float range = 30f;
    public float shellEjectionForce = 3f;

    [Header("Ссылки на ресурсы")]
    public GameObject weaponPrefab; // Модель, которая будет отображаться
    public RuntimeAnimatorController weaponAnimatorController; // Animator Controller
    public AudioClip fireSound;
    public AudioClip reloadSound;
    public GameObject shellCasingPrefab;
}
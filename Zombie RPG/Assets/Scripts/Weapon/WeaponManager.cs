using UnityEngine;
using System.Collections.Generic; // Для списка оружия

public class WeaponManager : MonoBehaviour
{
    // Список всех доступных чертежей оружия
    public List<WeaponData> availableWeapons; 
    
    // Активное оружие (этот объект, который мы видим)
    private GameObject currentWeaponObject; 
    
    // Активный скрипт стрельбы, который мы будем использовать
    private ActiveWeaponLogic activeWeapon; 
    
    // Индекс текущего оружия
    private int currentWeaponIndex = 0;

    void Start()
    {
        // Запускаем с первым оружием в списке
        if (availableWeapons.Count > 0)
        {
            EquipWeapon(availableWeapons[currentWeaponIndex]);
        }
    }
    
    // Вызывается при переключении
    public void EquipWeapon(WeaponData data)
    {
        // 1. Уничтожаем старую модель (если есть)
        if (currentWeaponObject != null)
        {
            Destroy(currentWeaponObject);
        }
        
        // 2. Создаем новую модель
        currentWeaponObject = Instantiate(data.weaponPrefab, transform); // transform - это рука игрока или камера
        
        // 3. Добавляем компонент логики оружия, передавая ему данные
        activeWeapon = currentWeaponObject.AddComponent<ActiveWeaponLogic>();
        activeWeapon.Initialize(data, this); // Передаем данные и ссылку на себя

        // 4. (Опционально) Обновляем Animator Controller, если он вложен в модель
        // (Это сложнее, лучше настроить Animator на самом префабе оружия)
    }
    
    // ... Добавьте логику переключения оружия (например, через колесо мыши) ...
}
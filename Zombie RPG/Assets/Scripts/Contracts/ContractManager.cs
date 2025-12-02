using System;
using System.Collections.Generic;
using UnityEngine;

public class ContractManager : MonoBehaviour
{
    public static ContractManager Instance { get; private set; }
    public Contract ActiveContract { get; private set; }

    public event Action OnContractUpdated;
    private HashSet<string> completedContractIds = new HashSet<string>();

    private const string NEXT_REFRESH_KEY = "NextFreeRefreshTime";
    private const int REFRESH_HOURS = 3;
    private const int REFRESH_COST = 100;
    public DateTime NextFreeRefreshTime { get; private set; }
    public event Action OnShopRefreshed;
    private List<Contract> currentShopContracts;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        LoadRefreshTime();
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
    }

    private void LoadRefreshTime()
    {
        string savedTime = PlayerPrefs.GetString(NEXT_REFRESH_KEY, "");
        if (long.TryParse(savedTime, out long binaryTime))
        {
            NextFreeRefreshTime = DateTime.FromBinary(binaryTime);
        }
        else
        {
            NextFreeRefreshTime = DateTime.Now;
        }
    }

    public TimeSpan GetTimeUntilFreeRefresh()
    {
        var diff = NextFreeRefreshTime - DateTime.Now;
        return diff.TotalSeconds > 0 ? diff : TimeSpan.Zero;
    }

    public bool TryRefreshShop(bool isPaid)
    {
        if (isPaid)
        {
            // Проверяем деньги
            if (InventoryData.Instance.TrySpendMoney(REFRESH_COST))
            {
                RefreshShop(false); // Платное не сбрасывает таймер бесплатного
                Debug.Log("Магазин обновлен платно");
                return true;
            }
            return false;
        }
        else
        {
            // Бесплатное обновление
            if (DateTime.Now >= NextFreeRefreshTime)
            {
                RefreshShop(true);
                return true;
            }
            return false;
        }
    }

    private void RefreshShop(bool resetTimer)
    {
        // Генерируем новые контракты (в реальном проекте тут рандом)
        currentShopContracts = GenerateNewRandomContracts();
        
        // Если это бесплатный рефреш, ставим таймер на 3 часа вперед
        if (resetTimer)
        {
            NextFreeRefreshTime = DateTime.Now.AddHours(REFRESH_HOURS);
            PlayerPrefs.SetString(NEXT_REFRESH_KEY, NextFreeRefreshTime.ToBinary().ToString());
            PlayerPrefs.Save();
        }

        OnShopRefreshed?.Invoke();
    }

    public List<Contract> GetCurrentShopContracts()
    {
        if (currentShopContracts == null) RefreshShop(true);
        return currentShopContracts;
    }

    // Метод для выбора контракта (из Главного Меню)
    public void SetActiveContract(Contract contract)
    {
        ActiveContract = contract;
        ActiveContract.CurrentProgress = 0;
        Debug.Log($"Контракт активирован: {contract.Name}");
    }

    public bool IsContractCompleted(string id)
    {
        return completedContractIds.Contains(id);
    }

    // --- Логика обновления прогресса (вызывается из геймплея) ---

    public void ReportKill(bool isHeadshot)
    {
        if (ActiveContract == null || ActiveContract.IsCompleted) return;

        bool progressMade = false;

        switch (ActiveContract.Type)
        {
            case ContractType.KillZombies:
                ActiveContract.CurrentProgress++;
                progressMade = true;
                break;

            case ContractType.Headshots:
                if (isHeadshot)
                {
                    ActiveContract.CurrentProgress++;
                    progressMade = true;
                }
                break;
        }

        if (progressMade)
        {
            CheckCompletion();
            OnContractUpdated?.Invoke();
        }
    }

    public void ReportSurvivalTick(float currentHp)
    {
        // Пример логики: если здоровье <= 10, засчитываем "выживание"
        if (ActiveContract == null || ActiveContract.IsCompleted) return;
        
        if (ActiveContract.Type == ContractType.SurviveLowHP && currentHp <= 20)
        {
            ActiveContract.CurrentProgress++; // Допустим, считаем секунды или тики
            CheckCompletion();
            OnContractUpdated?.Invoke();
        }
    }

    private void CheckCompletion()
    {
        if (ActiveContract.CurrentProgress >= ActiveContract.TargetAmount)
        {
            ActiveContract.CurrentProgress = ActiveContract.TargetAmount;
            completedContractIds.Add(ActiveContract.Id);
            Debug.Log($"КОНТРАКТ ВЫПОЛНЕН: {ActiveContract.Name} -> Награда: {ActiveContract.RewardText}");
            //  метод выдачи награды
        }
    }
    
    // Генератор контрактов для меню
    private List<Contract> GenerateNewRandomContracts()
    {
        var list = new List<Contract>();

        int seed = UnityEngine.Random.Range(1, 1000);
        
        list.Add(new Contract("Точность", "Убить 10 в голову", "Убийца", 50, ContractType.Headshots, 5) { Id = $"head_{seed}" });
        list.Add(new Contract("На грани", "Выжить с HP < 20", "Герой", 100, ContractType.SurviveLowHP, 10) { Id = $"surv_{seed}" });
        list.Add(new Contract("Мясорубка", "Убить 20 зомби", "Стрелок", 75, ContractType.KillZombies, 15) { Id = $"kill_{seed}" });
        
        return list;
    }
}

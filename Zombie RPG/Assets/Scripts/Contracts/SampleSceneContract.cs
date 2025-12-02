using UnityEngine;
using UnityEngine.UIElements;

public class SampleSceneContract : MonoBehaviour
{
    [SerializeField] private UIDocument escMenuDocument;
    
    private VisualElement root;
    private Label contractNameLabel;
    private Label conditionLabel;
    private ProgressBar contractProgressBar;

    private void OnEnable()
    {
        root = escMenuDocument.rootVisualElement;

        contractNameLabel = root.Q<Label>("ContractName");
        conditionLabel = root.Q<Label>("Condition");
        
        contractProgressBar = root.Q<ProgressBar>("ContractProgress");

        if (ContractManager.Instance != null)
        {
            ContractManager.Instance.OnContractUpdated += UpdateContractUI;
            UpdateContractUI(); 
        }
    }

    private void OnDisable()
    {
        if (ContractManager.Instance != null)
        {
            ContractManager.Instance.OnContractUpdated -= UpdateContractUI;
        }
    }

    private void UpdateContractUI()
    {
        var active = ContractManager.Instance.ActiveContract;

        if (active == null)
        {
            contractNameLabel.text = "НЕТ АКТИВНОГО КОНТРАКТА";
            conditionLabel.text = "Выберите контракт в главном меню";
            if (contractProgressBar != null) 
            {
                contractProgressBar.value = 0;
                contractProgressBar.title = "0%";
            }
            return;
        }

        contractNameLabel.text = active.Name.ToUpper(); // НАЗВАНИЕ
        conditionLabel.text = active.Description; // Условие

        if (contractProgressBar != null)
        {
            contractProgressBar.lowValue = 0;
            contractProgressBar.highValue = active.TargetAmount;
            contractProgressBar.value = active.CurrentProgress;
            
            contractProgressBar.title = $"{active.CurrentProgress} / {active.TargetAmount}";
        }
        
        if (active.IsCompleted)
        {
            conditionLabel.style.color = new StyleColor(Color.green);
            conditionLabel.text += " (ВЫПОЛНЕНО!)";
        }
        else
        {
            
        }
    }
}
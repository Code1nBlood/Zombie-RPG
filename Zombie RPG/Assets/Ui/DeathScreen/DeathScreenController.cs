using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections;

public class DeathScreenController : MonoBehaviour
{
    [Header("UI Document")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Настройки анимации")]
    [SerializeField] private float statsCountDuration = 1.5f;


    // UI элементы
    private VisualElement root;
    private VisualElement deathScreenRoot;
    private VisualElement vignette;
    private Label deathTitle;
    private VisualElement skullIcon;
    private VisualElement statsContainer;
    private Label zombiesValue;
    private Label roundsValue;
    private VisualElement buttonsContainer;
    private Button menuButton;

    private bool isShowing = false;

    public static DeathScreenController Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        InitializeUI();
    }

    private void InitializeUI()
    {
        if (uiDocument == null)
        {
            uiDocument = GetComponent<UIDocument>();
        }

        if (uiDocument == null)
        {
            Debug.LogError("DeathScreenController: UIDocument не назначен!");
            return;
        }
        uiDocument.sortingOrder = 100;

        root = uiDocument.rootVisualElement;

        deathScreenRoot = root.Q<VisualElement>("death-screen-root");
        vignette = root.Q<VisualElement>("vignette");
        deathTitle = root.Q<Label>("death-title");
        skullIcon = root.Q<VisualElement>("skull-icon");
        statsContainer = root.Q<VisualElement>("stats-container");
        zombiesValue = root.Q<Label>("zombies-value");
        roundsValue = root.Q<Label>("rounds-value");
        buttonsContainer = root.Q<VisualElement>("buttons-container");
        menuButton = root.Q<Button>("menu-button");

        // Подписка на кнопку
        if (menuButton != null)
        {
            menuButton.clicked += OnMenuClicked;
            Debug.Log("DeathScreen: Кнопка menu-button найдена и подписана");
        }
        else
        {
            Debug.LogError("DeathScreen: Кнопка menu-button НЕ НАЙДЕНА!");
        }

        HideInstant();
    }

    public void Show()
    {
        if (isShowing) return;

        int zombiesKilled = 0;
        int roundsSurvived = 0;

        if (RoundManager.Instance != null)
        {
            var stats = RoundManager.Instance.GetDeathStats();
            zombiesKilled = stats.zombiesKilled;
            roundsSurvived = stats.roundsSurvived;
            
            RoundManager.Instance.StopRounds();
        }

        Show(zombiesKilled, roundsSurvived);
    }

    // Показать экран смерти с заданной статистикой

    public void Show(int zombiesKilled, int roundsSurvived)
    {
        if (isShowing) return;
        isShowing = true;

        Debug.Log($"DeathScreen: Показываем экран смерти. Убито: {zombiesKilled}, Раунды: {roundsSurvived}");

        StartCoroutine(ShowSequence(zombiesKilled, roundsSurvived));
    }

    private IEnumerator ShowSequence(int zombiesKilled, int roundsSurvived)
    {
        deathScreenRoot.RemoveFromClassList("death-screen-hidden");
        deathScreenRoot.AddToClassList("death-screen-visible");
        deathScreenRoot.style.display = DisplayStyle.Flex;
        deathScreenRoot.pickingMode = PickingMode.Position;

        yield return new WaitForSecondsRealtime(0.2f);

        vignette?.AddToClassList("vignette-pulse");

        deathTitle?.AddToClassList("death-title-animated");

        yield return new WaitForSecondsRealtime(0.3f);

        skullIcon?.AddToClassList("skull-icon-animated");

        yield return new WaitForSecondsRealtime(0.4f);

        statsContainer?.AddToClassList("stats-container-animated");

        yield return new WaitForSecondsRealtime(0.5f);

        yield return StartCoroutine(AnimateStats(zombiesKilled, roundsSurvived));

        buttonsContainer?.AddToClassList("buttons-container-animated");

        if (menuButton != null)
        {
            menuButton.SetEnabled(true);
            menuButton.pickingMode = PickingMode.Position;
            menuButton.focusable = true;
        }

        Time.timeScale = 0f;
        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
    }

    private IEnumerator AnimateStats(int targetZombies, int targetRounds)
    {
        float elapsed = 0f;
        int currentZombies = 0;
        int currentRounds = 0;

        zombiesValue?.AddToClassList("stat-value-pulse");
        roundsValue?.AddToClassList("stat-value-pulse");

        while (elapsed < statsCountDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / statsCountDuration);

            // Easing для более интересной анимации
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            int newZombies = Mathf.RoundToInt(Mathf.Lerp(0, targetZombies, eased));
            int newRounds = Mathf.RoundToInt(Mathf.Lerp(0, targetRounds, eased));

            if (newZombies != currentZombies && zombiesValue != null)
            {
                currentZombies = newZombies;
                zombiesValue.text = currentZombies.ToString();
            }

            if (newRounds != currentRounds && roundsValue != null)
            {
                currentRounds = newRounds;
                roundsValue.text = currentRounds.ToString();
            }

            yield return null;
        }

        // Финальные значения
        if (zombiesValue != null) zombiesValue.text = targetZombies.ToString();
        if (roundsValue != null) roundsValue.text = targetRounds.ToString();

        yield return new WaitForSecondsRealtime(0.2f);

        zombiesValue?.RemoveFromClassList("stat-value-pulse");
        roundsValue?.RemoveFromClassList("stat-value-pulse");
        zombiesValue?.AddToClassList("stat-value-normal");
        roundsValue?.AddToClassList("stat-value-normal");
    }

    public void HideInstant()
    {
        isShowing = false;

        if (deathScreenRoot != null)
        {
            deathScreenRoot.RemoveFromClassList("death-screen-visible");
            deathScreenRoot.AddToClassList("death-screen-hidden");
            deathScreenRoot.style.display = DisplayStyle.None;
        }

        ResetAnimationClasses();
    }

    private void ResetAnimationClasses()
    {
        vignette?.RemoveFromClassList("vignette-pulse");
        deathTitle?.RemoveFromClassList("death-title-animated");
        skullIcon?.RemoveFromClassList("skull-icon-animated");
        statsContainer?.RemoveFromClassList("stats-container-animated");
        buttonsContainer?.RemoveFromClassList("buttons-container-animated");
        zombiesValue?.RemoveFromClassList("stat-value-pulse");
        zombiesValue?.RemoveFromClassList("stat-value-normal");
        roundsValue?.RemoveFromClassList("stat-value-pulse");
        roundsValue?.RemoveFromClassList("stat-value-normal");

        if (zombiesValue != null) zombiesValue.text = "0";
        if (roundsValue != null) roundsValue.text = "0";
    }

    private void OnMenuClicked()
    {
        Debug.Log("DeathScreen: Выход в меню");
        
        Time.timeScale = 1f;
        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        
        SceneManager.LoadScene("MainMenu");
    }

    private void OnDestroy()
    {

        if (menuButton != null)
        {
            menuButton.clicked -= OnMenuClicked;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }
}
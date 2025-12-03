using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;


public class EscMenuController : MonoBehaviour
{
    [SerializeField] private UIDocument escMenuUI;
    [SerializeField] private HUDController hudController;

    private VisualElement root;
    private bool isMenuOpen;

    private void Awake()
    {
        if (SceneManager.GetActiveScene().name != "SampleScene")
        {
            Destroy(gameObject);
            return;
        }
        
        root = escMenuUI.rootVisualElement;
        root.style.display = DisplayStyle.None;

        var continueBtn = root.Q<Button>("ContinueButton");
        var exitBtn     = root.Q<Button>("ExitToMainButton");

        continueBtn.clicked += ToggleMenu;
        exitBtn.clicked     += ExitToMainMenu;
    }

    private void OnDisable()        // <-- отписка
    {
        var continueBtn = root.Q<Button>("ContinueButton");
        var exitBtn     = root.Q<Button>("ExitToMainButton");
        continueBtn.clicked -= ToggleMenu;
        exitBtn.clicked     -= ExitToMainMenu;
        
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != "SampleScene") return;

        if (IsPlayerDead())
        {
            if (isMenuOpen)
            {
                ForceCloseMenu();
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape)) ToggleMenu();
    }

    private bool IsPlayerDead()
    {
        if (PlayerMovement.Instance != null && PlayerMovement.Instance.IsDead)
        {
            return true;
        }

        return false;
    }

    private void ForceCloseMenu()
    {
        isMenuOpen = false;
        root.style.display = DisplayStyle.None;
    }

    private void ToggleMenu()
    {
        if (!isMenuOpen && IsPlayerDead())
        {
            return;
        }

        isMenuOpen = !isMenuOpen;
        if (isMenuOpen)
        {
            root.style.display = DisplayStyle.Flex;
            hudController.HideHUD();
            Time.timeScale = 0f;
            UnityEngine.Cursor.lockState = CursorLockMode.None;
            UnityEngine.Cursor.visible = true;
        }
        else
        {
            root.style.display = DisplayStyle.None;
            hudController.ShowHUD();
            Time.timeScale = 1f;
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;
            UnityEngine.Cursor.visible = false;
        }
    }

    private void ExitToMainMenu()
    {
        Time.timeScale = 1f;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        SceneManager.LoadScene("MainMenu");
    }
}
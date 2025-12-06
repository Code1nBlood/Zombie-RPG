using UnityEngine;
using UnityEngine.SceneManagement;
public class Bootstrap : MonoBehaviour
{
    [SerializeField] private AudioManager audioManagerPrefab;

    private void Awake()
    {
        if (AudioManager.Instance == null)
        {
            Instantiate(audioManagerPrefab);
        }
    }

    private void Start()
    {
        AudioManager.Instance.LoadVolumeSettings();
        
        SceneManager.LoadScene("MainMenu");
    }
}
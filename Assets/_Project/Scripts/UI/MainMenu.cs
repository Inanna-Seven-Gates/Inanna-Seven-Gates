using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameScene = "SampleScene";
    [SerializeField] private Button btnContinue;
    [SerializeField] private GameObject creditsPanel;
    [SerializeField] private GameObject menuButtons;

    private void Start()
    {
        btnContinue.interactable = SaveSystem.HasSave();
        creditsPanel.SetActive(false);
    }

    public void NewGame()
    {
        SaveSystem.DeleteSave();
        SceneManager.LoadScene(gameScene);
    }

    public void Continue()
    {
        if (!SaveSystem.HasSave()) return;
        SceneManager.LoadScene(SaveSystem.LoadScene());
    }

    public void AbrirCreditos()
    {
        menuButtons.SetActive(false);
        creditsPanel.SetActive(true);
    }
    public void FecharCreditos()
    {
        creditsPanel.SetActive(false);
        menuButtons.SetActive(true);
    }


    public void Exit()
    {
        Debug.Log("Saindo do jogo...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
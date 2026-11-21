using UnityEngine;
using UnityEngine.InputSystem;
public class GameLoader : MonoBehaviour
{
    [SerializeField] private Transform player; 

    private void Start()
    {
        if (SaveSystem.HasSave() && SaveSystem.LoadScene() == gameObject.scene.name)
        {
            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false; 

            player.position = SaveSystem.LoadPosition();

            if (cc != null) cc.enabled = true;
        }
    }

    public void SaveGame()
    {
        SaveSystem.SaveGame(gameObject.scene.name, player.position);
    }

    private void Update()
    {
        if (Keyboard.current.f5Key.wasPressedThisFrame) SaveGame();
    }
}
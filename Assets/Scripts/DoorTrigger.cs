using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorTrigger : MonoBehaviour
{
    public string requiredKey = "MainKey";
    public string nextSceneName = "Level2"; // Asigură-te că scena există în Build Settings

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerInventory inventory = other.GetComponent<PlayerInventory>();
            if (inventory != null && inventory.HasKey(requiredKey))
            {
                SceneManager.LoadScene(nextSceneName); // Teleportează la următorul nivel
            }
            else
            {
                Debug.Log("Ai nevoie de cheia pentru a trece!");
            }
        }
    }
}

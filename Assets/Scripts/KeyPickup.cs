using UnityEngine;

public class KeyPickup : MonoBehaviour
{
    public string keyID = "MainKey"; // Poți folosi mai multe chei cu ID-uri diferite

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerInventory inventory = other.GetComponent<PlayerInventory>();
            if (inventory != null)
            {
                inventory.AddKey(keyID);
                Destroy(gameObject); // Distruge cheia după colectare
            }
        }
    }
}

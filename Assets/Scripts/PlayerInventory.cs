using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    private HashSet<string> keys = new();

    public void AddKey(string key)
    {
        keys.Add(key);
        Debug.Log("Ai luat cheia: " + key);
    }

    public bool HasKey(string key)
    {
        return keys.Contains(key);
    }
}

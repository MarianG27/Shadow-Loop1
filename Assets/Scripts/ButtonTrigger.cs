// ButtonTrigger.cs
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ButtonTrigger : MonoBehaviour
{
    [Header("Identity")]
    public int buttonID;

    [Header("Sprites")]
    public Sprite offSprite;
    public Sprite onSprite;

    [Header("Platform refs")]
    public GameObject platformUp;
    public GameObject platformDown;

    private SpriteRenderer sr;
    private bool isActive = false;
    private int activatedRound = -1;

    // player proximity
    private bool playerNearby = false;
    private PlayerController playerRef;

    // registry for lookup
    private static readonly Dictionary<int, ButtonTrigger> registry = new Dictionary<int, ButtonTrigger>();

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        SetVisual(false);

        if (registry.ContainsKey(buttonID))
        {
            Debug.LogWarning($"[ButtonTrigger] duplicate buttonID {buttonID} - overwriting registry entry.");
            registry[buttonID] = this;
        }
        else
        {
            registry.Add(buttonID, this);
        }

        // initial platform states
        SetPlatformState(platformUp, true);
        SetPlatformState(platformDown, false);
    }

    void OnDestroy()
    {
        if (registry.ContainsKey(buttonID) && registry[buttonID] == this)
            registry.Remove(buttonID);
    }

    public static bool TryGetButton(int id, out ButtonTrigger btn) => registry.TryGetValue(id, out btn);

    void Update()
    {
        // doar playerul apasa E
        if (playerNearby && !isActive && Input.GetKeyDown(KeyCode.E))
        {
            // verificăm dacă există deja un task pentru această rundă => prevenim duplicate
            bool alreadyThisRound = false;
            foreach (var t in GameManager.allButtonTasks)
            {
                if (t.buttonID == buttonID && t.round == GameManager.CurrentCycleIndex)
                {
                    alreadyThisRound = true;
                    break;
                }
            }

            if (!alreadyThisRound)
            {
                float rel = Time.time - GameManager.CurrentRoundStartTime;
                // îl înregistrăm în GameManager (va face "steal" automat)
                GameManager.Instance.RegisterButtonPress(buttonID, rel, GameManager.CurrentCycleIndex);

                // activare imediată vizuală pentru jucător
                ActivateButton(GameManager.CurrentCycleIndex);

                if (playerRef != null)
                    playerRef.StartButtonPressAnimation();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerNearby = true;
        playerRef = other.GetComponent<PlayerController>();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        playerNearby = false;
        playerRef = null;
    }

    // apelat de GameManager la timpul programat
    public void ActivateButton(int roundIndex)
    {
        if (isActive) return;
        isActive = true;
        activatedRound = roundIndex;
        SetVisual(true);
        SetPlatformState(platformUp, false);
        SetPlatformState(platformDown, true);
        Debug.Log($"[ButtonTrigger] button {buttonID} activated for round {roundIndex}");
    }

    public void ResetVisual()
    {
        isActive = false;
        activatedRound = -1;
        SetVisual(false);
        SetPlatformState(platformUp, true);
        SetPlatformState(platformDown, false);
    }

    private void SetVisual(bool on)
    {
        if (sr != null) sr.sprite = on ? onSprite : offSprite;
    }

    private void SetPlatformState(GameObject platform, bool active)
    {
        if (platform == null) return;
        platform.SetActive(active);
        var c = platform.GetComponent<Collider2D>();
        if (c != null) c.enabled = active;
    }

    public static void ResetAllButtons()
    {
        // reset vizual pentru toate butoanele
        foreach (var b in FindObjectsOfType<ButtonTrigger>()) b.ResetVisual();
    }
}

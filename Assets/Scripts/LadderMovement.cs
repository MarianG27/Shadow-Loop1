using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class LadderMovement : MonoBehaviour
{
    [Header("Climb settings")]
    public float speed = 5f;
    public Vector2 climbSize = new Vector2(0.5f, 1.5f);

    [Header("Align settings")]
    public float alignSpeed = 10f;      // cât de repede se aliniază la centru (Lerp)
    public float alignThreshold = 0.05f; // când considerăm că suntem suficient de centrați

    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private BoxCollider2D playerCollider;

    private Vector2 normalOffset;
    private Vector2 normalSize;

    // urmărim toate collider-ele 'Ladder' în care suntem (stack safe)
    private List<Collider2D> activeLadders = new List<Collider2D>();
    private Collider2D currentLadderCollider;

    // stări
    private bool isInLadderZone = false;
    private float verticalInput;

    // referință la PlayerController
    private PlayerController playerController;

    private void Start()
    {
        if (playerCollider == null)
            Debug.LogError("LadderMovement: playerCollider nu e setat în Inspector!");

        normalOffset = playerCollider.offset;
        normalSize = playerCollider.size;

        playerController = GetComponent<PlayerController>();
        if (playerController == null)
            Debug.LogWarning("LadderMovement: PlayerController lipsă pe același GameObject.");
    }

    private void Update()
    {
        verticalInput = Input.GetAxisRaw("Vertical");

        // actualizare stări în funcție de zone active
        isInLadderZone = activeLadders.Count > 0;
        currentLadderCollider = isInLadderZone ? activeLadders[activeLadders.Count - 1] : null;

        if (isInLadderZone)
        {
            // dacă există input vertical pornim mecanica de aliniere / climb
            if (Mathf.Abs(verticalInput) > 0.01f)
            {
                AlignToLadderCenterSmooth();

                // când suntem aproape de centrul scării, ajustăm colliderul la dimensiunea de climb
                float targetX = GetLadderCenterX();
                float dx = Mathf.Abs(transform.position.x - targetX);

                if (dx <= alignThreshold)
                {
                    // ajustăm collider și notificăm player
                    playerCollider.size = climbSize;
                    playerCollider.offset = new Vector2(0f, normalOffset.y); // center pe X
                    playerController?.SetLadderColliderAdjusted(true);
                }
                else
                {
                    // dacă nu am ajuns încă în centru, păstrăm collider normal (opțional)
                    playerCollider.size = normalSize;
                    playerCollider.offset = normalOffset;
                    // nu forțăm SetLadderColliderAdjusted(true) până nu suntem centrat
                }
            }
            else
            {
                // dacă nu este input vertical, păstrăm starea de "in zone" dar nu schimbăm colliderul
                // opțiune: dacă vrei să se schimbe colliderul chiar la intrare în zonă, comentează secțiunea de mai sus
            }
        }
        else
        {
            // nu mai suntem în niciun ladder -> restaurăm collider normal și notificăm player
            playerCollider.size = normalSize;
            playerCollider.offset = normalOffset;
            playerController?.SetLadderColliderAdjusted(false);
        }
    }

    private void AlignToLadderCenterSmooth()
    {
        if (currentLadderCollider == null) return;

        float targetX = GetLadderCenterX();
        Vector3 pos = transform.position;
        pos.x = Mathf.Lerp(transform.position.x, targetX, Mathf.Clamp01(alignSpeed * Time.deltaTime));
        transform.position = pos;
    }

    private float GetLadderCenterX()
    {
        if (currentLadderCollider != null)
        {
            return currentLadderCollider.bounds.center.x;
        }
        return currentLadderCollider != null ? currentLadderCollider.transform.position.x : transform.position.x;
    }

    // gestionează intrarea în trigger
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ladder"))
        {
            // adăugăm în stack
            if (!activeLadders.Contains(other))
                activeLadders.Add(other);

            // actualizăm
            isInLadderZone = true;
            currentLadderCollider = activeLadders[activeLadders.Count - 1];
        }
        else if (other.CompareTag("LadderExit"))
        {
            // WHEN entering a LadderExit zone we force exit immediately
            ForceExitLadder();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Ladder"))
        {
            // scoatem din stack
            if (activeLadders.Contains(other))
                activeLadders.Remove(other);

            // dacă nu mai sunt laddere -> exit
            if (activeLadders.Count == 0)
            {
                ForceExitLadder();
            }
            else
            {
                currentLadderCollider = activeLadders[activeLadders.Count - 1];
            }
        }
        else if (other.CompareTag("LadderExit"))
        {
            // dacă ieșim din zona LadderExit, și nu mai suntem în ladder, asigurăm exit
            if (activeLadders.Count == 0)
                ForceExitLadder();
        }
    }

    private void ForceExitLadder()
    {
        activeLadders.Clear();
        currentLadderCollider = null;
        isInLadderZone = false;

        // restaurăm collider
        playerCollider.offset = normalOffset;
        playerCollider.size = normalSize;

        // notificăm player controller să revină la modul normal
        playerController?.SetLadderColliderAdjusted(false);
    }

    // utilă publică dacă vrei forțare din exterior
    public void ForceAlignToLadder(float smooth = 0f)
    {
        if (currentLadderCollider == null) return;
        if (smooth > 0f)
        {
            Vector3 pos = transform.position;
            pos.x = Mathf.Lerp(transform.position.x, GetLadderCenterX(), Mathf.Clamp01(smooth * Time.deltaTime));
            transform.position = pos;
        }
        else
        {
            Vector3 pos = transform.position;
            pos.x = GetLadderCenterX();
            transform.position = pos;
        }
    }
}

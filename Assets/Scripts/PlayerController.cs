using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;
    public float jumpForce = 7f;
    public float climbSpeed = 3f;

    [Header("Auto-Freeze (optional)")]
    public bool enableAutoFreeze = false;
    public float playDuration = 10f;
    public float freezeDuration = 2f;

    [Header("Button Animation")]
    public float buttonAnimDuration = 1f;

    private Rigidbody2D rb;
    private Vector2 input;
    private bool canMove = true;
    private bool isGrounded;
    private bool facingRight = true;

    [SerializeField] private Animator animator;

    private bool ladderColliderAdjusted = false;
    private bool isClimbing = false;

    private float prevAnimatorSpeed = 1f;
    private bool prevIsRunning = false;

    private float autoTimer = 0f;
    private Coroutine autoFreezeCoroutine = null;
    private bool autoFrozen = false;
    private bool externalBlock = false;

    private HashSet<GameObject> triggeredButtons = new();
    private Coroutine buttonCoroutine = null;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (animator == null)
            Debug.LogWarning("PlayerController: Animator not set in Inspector.");
    }

    void Update()
    {
        if (autoFrozen || !canMove)
        {
            if (ladderColliderAdjusted) HandleClimbingInput();
            return;
        }

        if (ladderColliderAdjusted)
        {
            HandleClimbingInput();
            return;
        }

        float rawH = Input.GetAxisRaw("Horizontal");
        bool pressLeft = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        bool pressRight = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);

        bool pressingH = Mathf.Abs(rawH) > 0.01f || pressLeft || pressRight;

        if (!canMove) input = Vector2.zero;
        else
        {
            float h = pressingH ? rawH : 0f;
            if (Mathf.Abs(h) < 0.01f)
            {
                if (pressLeft) h = -1f;
                else if (pressRight) h = 1f;
            }
            input.x = h;
        }

        bool runningNow = Mathf.Abs(input.x) > 0.01f;
        if (animator != null) animator.SetBool("IsRunning", runningNow);

        if (runningNow)
        {
            if (input.x > 0 && !facingRight) Flip();
            else if (input.x < 0 && facingRight) Flip();
        }

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        if (enableAutoFreeze && !ladderColliderAdjusted)
        {
            autoTimer += Time.deltaTime;
            if (autoTimer >= playDuration && !externalBlock) StartAutoFreeze();
        }
    }

    void FixedUpdate()
    {
        if (!ladderColliderAdjusted && canMove)
            rb.linearVelocity = new Vector2(input.x * speed, rb.linearVelocity.y);
    }

    private void HandleClimbingInput()
    {
        float rawV = Input.GetAxisRaw("Vertical");
        bool pressUp = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        bool pressDown = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        bool pressingV = Mathf.Abs(rawV) > 0.01f || pressUp || pressDown;

        float v = pressingV ? rawV : 0f;
        if (Mathf.Abs(v) < 0.01f)
        {
            if (pressUp) v = 1f;
            else if (pressDown) v = -1f;
        }

        isClimbing = true;
        if (animator != null) animator.SetBool("IsClimbing", true);

        if (pressingV) animator.speed = 1f;
        else animator.speed = 0f;

        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(0f, v * climbSpeed);

        if (animator != null) animator.SetBool("IsRunning", false);
    }

    private void StartAutoFreeze()
    {
        if (autoFreezeCoroutine != null) return;
        autoFreezeCoroutine = StartCoroutine(AutoFreezeRoutine());
    }

    private IEnumerator AutoFreezeRoutine()
    {
        autoFrozen = true;
        SetCanMove(false);
        yield return new WaitForSeconds(freezeDuration);
        SetCanMove(true);
        autoTimer = 0f;
        autoFrozen = false;
        autoFreezeCoroutine = null;
    }

    public void ResetAutoFreezeTimer()
    {
        autoTimer = 0f;
        if (autoFreezeCoroutine != null)
        {
            StopCoroutine(autoFreezeCoroutine);
            autoFreezeCoroutine = null;
            autoFrozen = false;
        }
        canMove = !externalBlock;
    }

    public void SetCanMove(bool value)
    {
        canMove = value;
        externalBlock = !value;
        if (!value) rb.linearVelocity = Vector2.zero;
        else autoTimer = 0f;
    }

    public bool CanMove() => canMove;

    public void ForceClimb(bool state)
    {
        isClimbing = state;
        animator?.SetBool("IsClimbing", state);

        if (state)
        {
            ladderColliderAdjusted = true;
            prevAnimatorSpeed = animator != null ? animator.speed : 1f;
            prevIsRunning = animator != null ? animator.GetBool("IsRunning") : false;
            animator?.SetBool("IsRunning", false);
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            ladderColliderAdjusted = false;
            animator.speed = prevAnimatorSpeed;
            animator.SetBool("IsClimbing", false);
            animator.SetBool("IsRunning", prevIsRunning);
            isClimbing = false;
            rb.gravityScale = 1f;
        }
    }

    public void SetLadderColliderAdjusted(bool adjusted)
    {
        if (ladderColliderAdjusted == adjusted) return;

        ladderColliderAdjusted = adjusted;

        if (adjusted)
        {
            prevAnimatorSpeed = animator != null ? animator.speed : 1f;
            prevIsRunning = animator != null ? animator.GetBool("IsRunning") : false;

            animator?.SetBool("IsRunning", false);
            animator?.SetBool("IsClimbing", true);
            animator.speed = 0f;
            rb.linearVelocity = Vector2.zero;
            rb.gravityScale = 0f;
        }
        else
        {
            animator.speed = prevAnimatorSpeed;
            animator.SetBool("IsClimbing", false);
            animator.SetBool("IsRunning", prevIsRunning);
            isClimbing = false;
            rb.gravityScale = 1f;
            autoTimer = 0f;
        }
    }

    public bool IsClimbingState() => isClimbing;
    public bool IsLadderAdjusted() => ladderColliderAdjusted;

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.contacts.Length > 0 && collision.contacts[0].normal.y > 0.5f)
            isGrounded = true;
    }

    void OnCollisionExit2D(Collision2D collision)
    {
        isGrounded = false;
    }

    void Flip()
    {
        facingRight = !facingRight;
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    // ---------------------------
    // Button press animation handling (public API)
    // ---------------------------
    public void StartButtonPressAnimation()
    {
        if (buttonCoroutine == null)
        {
            buttonCoroutine = StartCoroutine(PlayButtonAnim());
        }
    }

    private IEnumerator PlayButtonAnim()
    {
        SetCanMove(false);
        rb.linearVelocity = Vector2.zero;
        input = Vector2.zero;

        if (animator != null)
        {
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsButton", true);
        }

        yield return new WaitForSeconds(buttonAnimDuration);

        if (animator != null)
        {
            animator.SetBool("IsButton", false);
        }

        SetCanMove(true);
        buttonCoroutine = null;
    }

    // ---------------------------
    // Reset la Idle pentru GameManager
    // ---------------------------
    public void ResetToIdle()
    {
        rb.linearVelocity = Vector2.zero;
        input = Vector2.zero;
        isClimbing = false;
        ladderColliderAdjusted = false;

        rb.gravityScale = 1f;

        if (animator != null)
        {
            animator.SetBool("IsRunning", false);
            animator.SetBool("IsClimbing", false);
            animator.SetBool("IsButton", false);
            animator.Play("Idle"); // trebuie să ai statul "Idle" în Animator
        }
    }
}

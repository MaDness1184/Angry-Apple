using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

public enum PlayerState
{ 
    normal,
    staggered,
    punch,
    airPunch,
    dead
}

public class Player : MonoBehaviour
{
    [Header("Player Stats")]
    public int health = 6;
    public int basicAttackDamage = 1;

    [Header("Player Velocity")]
    public float walkSpeed = 4.5f;
    public float jumpThrust = 10f;
    public float climbSpeed = 10f;
    public Vector2 rightHurtKnockback = new Vector2(10f, 10f);
    public Vector2 leftHurtKnockback = new Vector2(-10f, 10f);

    [Header("Sounds")]
    public AudioClip[] punchSounds;
    public AudioClip[] hurtSounds;
    public AudioClip[] deathSounds;

    [Header("Timing (Don't Change Unless Tweaking Animation)")]
    public float punchDelay = 1f;
    public float airPunchDelay = 0.32f;
    public float hurtDelay = 0.2f;
    public int involnerabilityTime = 100;
    public float spriteFlashDelay = 0.1f;

    [Header("Player State")]
    public PlayerState currentState;
    public bool involnerable = false;
    public bool isAlive = true;

    [Header("Chached References")]
    public GameObject sprite;

    // Private Chached References
    private Rigidbody2D myRigidbody;
    private Animator myAnimator;
    private AudioSource myAudioSource;
    private CapsuleCollider2D myBodyCollider;
    private BoxCollider2D myFeetCollider;
    private float gravityScaleAtStart;

    // Private Variables

    // Start is called before the first frame update
    void Start()
    {
        myRigidbody = GetComponent<Rigidbody2D>();
        myAnimator = GetComponent<Animator>();
        myAudioSource = GetComponent<AudioSource>();
        myBodyCollider = GetComponent<CapsuleCollider2D>();
        myFeetCollider = GetComponent<BoxCollider2D>();
        myAnimator.SetFloat("moveX", 1);
        gravityScaleAtStart = myRigidbody.gravityScale;
    }

    // Update is called once per frame
    void Update()
    {
        if (currentState == PlayerState.dead)
            return;
        Death();
        if (currentState != PlayerState.staggered
            && currentState != PlayerState.punch
            && currentState != PlayerState.airPunch)
        {
            Run();
            Jump();
            Punch();
            AirPunch();
            Climb();
            if (!involnerable)
            {
                Hurt();
            }
        }
    }

    private void Run()
    {
        float controlThrow = CrossPlatformInputManager.GetAxisRaw("Horizontal") * walkSpeed;
        Vector2 playerVelocity = new Vector2(controlThrow, myRigidbody.velocity.y);
        myRigidbody.velocity = playerVelocity;

        bool playerHasHorizontalSpeed = Mathf.Abs(myRigidbody.velocity.x) > Mathf.Epsilon;
        float direction = Mathf.Sign(myRigidbody.velocity.x); // returns a value that is between -1 to +1
        if (playerHasHorizontalSpeed)
        {
            myAnimator.SetBool("isWalking", true);
            myAnimator.SetFloat("moveX", direction);
        }
        else
        {
            myAnimator.SetBool("isWalking", false);
        }
    }

    private void Jump()
    {
        bool isAbleToJump = myFeetCollider.IsTouchingLayers(LayerMask.GetMask("Ground"))
                    || myFeetCollider.IsTouchingLayers(LayerMask.GetMask("Hazards"));

        if (isAbleToJump)
        {
            if (CrossPlatformInputManager.GetButtonDown("Jump"))
            {
                Debug.Log("Jumping");
                Vector2 jumpVelocityToAdd = new Vector2(0f, jumpThrust);
                myRigidbody.velocity += jumpVelocityToAdd;
            }
        }
    }

    private void Punch()
    {
        bool isAbleToPunch = myFeetCollider.IsTouchingLayers(LayerMask.GetMask("Ground"));

        if (isAbleToPunch)
        {
            if (CrossPlatformInputManager.GetButtonDown("Punch"))
            {
                    StartCoroutine(PunchCo());
            }
        }
    }

    private void AirPunch()
    {
        bool isAbleToAirPunch = !myFeetCollider.IsTouchingLayers(LayerMask.GetMask("Ground"))
                && !myFeetCollider.IsTouchingLayers(LayerMask.GetMask("Hazards"))
                && !myFeetCollider.IsTouchingLayers(LayerMask.GetMask("Climbable"));

        if (isAbleToAirPunch)
        {
            if (CrossPlatformInputManager.GetButtonDown("Punch"))
            {
                StartCoroutine(AirPunchCo());
            }
        }
    }

    private void Climb()
    {
        bool isNotAbleToClimb = !myFeetCollider.IsTouchingLayers(LayerMask.GetMask("Climbable"));

        if (isNotAbleToClimb)
        {
            myAnimator.SetBool("isClimbing", false);
            myRigidbody.gravityScale = gravityScaleAtStart;
            return;
        }

        float controlThrow = CrossPlatformInputManager.GetAxis("Vertical");
        Vector2 climbVelocity = new Vector2(myRigidbody.velocity.x, controlThrow * climbSpeed);
        myRigidbody.velocity = climbVelocity;
        myRigidbody.gravityScale = 0f;

        bool playerHasVerticalSpeed = Mathf.Abs(myRigidbody.velocity.y) > Mathf.Epsilon;
        myAnimator.SetBool("isClimbing", playerHasVerticalSpeed);

    }

    private void Hurt()
    {
        bool isAbleToHurt = myBodyCollider.IsTouchingLayers(LayerMask.GetMask("Hazards"))
                || myFeetCollider.IsTouchingLayers(LayerMask.GetMask("Hazards"))
                || myBodyCollider.IsTouchingLayers(LayerMask.GetMask("Enemy"));

         if (isAbleToHurt)
         {
            health--;
             if (health >= 1) // This if prevents double knockback and involnerability when dead
             {
                Knockback();
                StartCoroutine(InvulnerableCo());
                StartCoroutine(HurtCo());
             }
         }
    }

    private void Death()
    {
        if(health <= 0)
        {
            currentState = PlayerState.dead;
            Knockback();
            myAnimator.SetTrigger("deathTrigger");
            myAudioSource.PlayOneShot(deathSounds[Random.Range(0, deathSounds.Length)]);
        }
    }

    private void Knockback()
    {
        float direction = Mathf.Sign(myRigidbody.velocity.x);

        if (direction < 0)
        {
            myRigidbody.AddForce(rightHurtKnockback, ForceMode2D.Impulse);
        }
        else
        {
            myRigidbody.AddForce(leftHurtKnockback, ForceMode2D.Impulse);
        }
    }

    private IEnumerator PunchCo()
    {
        myRigidbody.velocity = Vector2.zero;
        currentState = PlayerState.punch;
        myAnimator.SetBool("isPunching", true);
        myAudioSource.PlayOneShot(punchSounds[Random.Range(0, punchSounds.Length)]);
        yield return new WaitForSeconds(punchDelay);
        myAnimator.SetBool("isPunching", false);
        currentState = PlayerState.normal;
    }

    private IEnumerator AirPunchCo()
    {
        currentState = PlayerState.airPunch;
        myAnimator.SetBool("isAirPunching", true);
        myAudioSource.PlayOneShot(punchSounds[Random.Range(0, punchSounds.Length)]);
        yield return new WaitForSeconds(airPunchDelay);
        myAnimator.SetBool("isAirPunching", false);
        currentState = PlayerState.normal;
    }

    private IEnumerator HurtCo()
    {
        currentState = PlayerState.staggered;
        myAnimator.SetBool("isHurt", true);
        myAudioSource.PlayOneShot(hurtSounds[Random.Range(0, hurtSounds.Length)]);
        yield return new WaitForSeconds(hurtDelay);
        myAnimator.SetBool("isHurt", false);
        currentState = PlayerState.normal;
    }

    private IEnumerator InvulnerableCo()
    {
        involnerable = true;
        yield return new WaitForSeconds(0.5f);
        for (int n = 0; n < involnerabilityTime; n++)
        {
            sprite.GetComponent<SpriteRenderer>().color = new Color (0, 0, 0, 0);
            yield return new WaitForSeconds(spriteFlashDelay);
            sprite.GetComponent<SpriteRenderer>().color = Color.white;
            yield return new WaitForSeconds(spriteFlashDelay + 0.15f);
        }
        involnerable = false;
    }
}

// 參考教學：
// wall sliding & wall jumping: https://www.youtube.com/watch?v=KCzEnKLaaPc

using UnityEngine;
using UnityEngine.Events;

//需要解決在空中無法二段跳的問題
public class CharacterController2D : MonoBehaviour
{
    // m_ prefix for class member, k_ prefix for constant
    [SerializeField] private float m_JumpForce = 800f;                          // Amount of force added when the player jumps.
    [SerializeField] private float m_xWallForce = 30f;                          // Amount of force added when the player jumping on the wall for x axis.
    [SerializeField] private float m_yWallForce = 60f;                          // Amount of force added when the player jumping on the wall for y axis.
    [SerializeField] private float m_WallJumpingTime = .05f;                          // Time limit when the player jumping on the wall.
    [Range(0, 1)] [SerializeField] private float m_CrouchSpeed = .36f;          // Amount of maxSpeed applied to crouching movement. 1 = 100%
    [Range(0, 1)] [SerializeField] private float m_WallSlidingSpeed = .3f;          // Amount of maxSpeed applied to sliding wall. 1 = 100%
    [SerializeField] private float m_WallJumpingSpeed = .6f;          // Amount of maxSpeed applied to sliding wall. 1 = 100%
    [Range(0, .3f)] [SerializeField] private float m_MovementSmoothing = .05f;  // How much to smooth out the movement
    [SerializeField] private bool m_AirControl = true;                         // Whether or not a player can steer while jumping;
    [SerializeField] private LayerMask m_WhatIsGround;                          // A mask determining what is ground to the character
    [SerializeField] private Transform m_GroundCheck;                           // A position marking where to check if the player is grounded.
    [SerializeField] private Transform m_CeilingCheck;                          // A position marking where to check for ceilings
    [SerializeField] private Transform m_FrontCheck;                           // A position marking where to check if the player meets sth front.
    [SerializeField] private Collider2D m_CrouchDisableCollider;                // A collider that will be disabled when crouching

    const float k_GroundedRadius = .2f; // Radius of the overlap circle to determine if grounded
    private bool m_Grounded;            // Whether or not the player is grounded.
    const float k_CeilingRadius = .2f; // Radius of the overlap circle to determine if the player can stand up
    private Rigidbody2D m_Rigidbody2D;
    private bool m_FacingRight = true;  // For determining which way the player is currently facing.
    private Vector3 m_Velocity = Vector3.zero;
    const float k_FrontCheckRadius = .2f; // Radius of the overlap circle to determine if the player can side on wall.
    private bool m_TouchingFront;            // Whether or not the player touched something in fornt of him/her.
    private bool m_WallSliding;            // Whether or not the player is sliding on wall.
    private bool m_WallJumping;            // Whether or not the player is jumping on wall.

    [Header("Events")]
    [Space]

    public UnityEvent OnLandEvent;

    [System.Serializable]
    public class BoolEvent : UnityEvent<bool> { }

    public BoolEvent OnCrouchEvent;
    private bool m_wasCrouching = false;
    private bool m_wasDoubleJump = false;

    public UserModel UserModelScript;
    public KnightControl AnimControlScript;

    private void Awake()
    {
        m_Rigidbody2D = GetComponent<Rigidbody2D>();

        if (OnLandEvent == null)
            OnLandEvent = new UnityEvent();

        if (OnCrouchEvent == null)
            OnCrouchEvent = new BoolEvent();
    }

    private void FixedUpdate()
    {

        bool wasGrounded = m_Grounded;
        m_Grounded = false;
        m_TouchingFront = false;

        // The player is grounded if a circlecast to the groundcheck position hits anything designated as ground
        // This can be done using layers instead but Sample Assets will not overwrite your project settings.
        Collider2D[] colliders = Physics2D.OverlapCircleAll(m_GroundCheck.position, k_GroundedRadius, m_WhatIsGround);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].gameObject != gameObject)
            {
                m_Grounded = true;
                if (!wasGrounded)
                    OnLandEvent.Invoke();
            }
        }

        // For the character to slide down on the wall.
        colliders = Physics2D.OverlapCircleAll(m_FrontCheck.position, k_FrontCheckRadius, m_WhatIsGround);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].gameObject != gameObject)
            {
                m_TouchingFront = true;
            }
        } 
    }


    public void Move(float move, bool crouch, bool jump)
    {
        // Back to idle animation
        if (move == 0 && !jump && !crouch)
        {
            AnimControlScript.idle();
        }
        else if (move > 0 || move < 0)
        {
            AnimControlScript.walking();
        }
        if (!m_Grounded)
        {
            AnimControlScript.jump();
        }

        // If crouching, check to see if the character can stand up
        if (!crouch)
        {
            // If the character has a ceiling preventing them from standing up, keep them crouching
            if (Physics2D.OverlapCircle(m_CeilingCheck.position, k_CeilingRadius, m_WhatIsGround))
            {
                crouch = true;
            }
        }

        //only control the player if grounded or airControl is turned on
        if (m_Grounded || m_AirControl)
        {
            //if (jump)
            //{
            //    AnimControlScript.jump();
            //}
            // If crouching
            if (crouch)
            {
                if (!m_wasCrouching)
                {
                    m_wasCrouching = true;
                    OnCrouchEvent.Invoke(true);
                }

                // Reduce the speed by the crouchSpeed multiplier
                move *= m_CrouchSpeed;

                // Disable one of the colliders when crouching
                if (m_CrouchDisableCollider != null)
                {
                    m_CrouchDisableCollider.enabled = false;
                }
            }
            else
            {
                // Enable the collider when not crouching
                if (m_CrouchDisableCollider != null)
                    m_CrouchDisableCollider.enabled = true;

                if (m_wasCrouching)
                {
                    m_wasCrouching = false;
                    OnCrouchEvent.Invoke(false);
                }
            }

            // Move the character by finding the target velocity
            Vector3 targetVelocity = new Vector2(move * 10f, m_Rigidbody2D.velocity.y);
            // And then smoothing it out and applying it to the character
            m_Rigidbody2D.velocity = Vector3.SmoothDamp(m_Rigidbody2D.velocity, targetVelocity, ref m_Velocity, m_MovementSmoothing);

            // If the input is moving the player right and the player is facing left...
            if (move > 0 && !m_FacingRight)
            {
                // ... flip the player.
                Flip();
            }
            // Otherwise if the input is moving the player left and the player is facing right...
            else if (move < 0 && m_FacingRight)
            {
                // ... flip the player.
                Flip();
            }
        }

        // sliding on the wall
        if (m_TouchingFront && !m_Grounded && move != 0)
        {
            m_WallSliding = true;
        }
        else
        {
            m_WallSliding = false;
        }

        if (m_WallSliding)
        {
            m_Rigidbody2D.velocity = new Vector2(m_Rigidbody2D.velocity.x, Mathf.Clamp(m_Rigidbody2D.velocity.y, -m_WallSlidingSpeed, float.MaxValue));
        }

        //jumping on the wall
        if (jump && m_WallSliding)
        {
            m_WallJumping = true;
            Invoke("SetWallJumpingToFalse", m_WallJumpingTime);
        }

        // If the player should jump...
        if (!m_TouchingFront) m_WallJumping = false;

        if (m_WallJumping)
        {
            print("wall jumping");
            m_Rigidbody2D.velocity = new Vector2(m_xWallForce * -move * m_WallJumpingSpeed, m_yWallForce);
        }
        else if (m_Grounded && jump)
        {
            print("jump");
            // Add a vertical force to the player.
            m_Grounded = false;
            m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce));
        } else if (UserModelScript.GetAbility("Double Jump") && !m_Grounded && jump && !m_wasDoubleJump)
        {
            m_wasDoubleJump = true;
            //二段跳
            print("Double Jump");
            m_Grounded = false;
            m_Rigidbody2D.AddForce(new Vector2(0f, m_JumpForce));
        }
        else if (UserModelScript.GetAbility("Double Jump") && m_Grounded && m_wasDoubleJump)
        {
            //回地面時二段跳重置
            m_wasDoubleJump = false;
        }

        
    }


    private void Flip()
    {
        // Switch the way the player is labelled as facing.
        m_FacingRight = !m_FacingRight;

        // Multiply the player's x local scale by -1.
        Vector3 theScale = transform.localScale;
        theScale.x *= -1;
        transform.localScale = theScale;
    }

    private void SetWallJumpingToFalse()
    {
        m_WallJumping = false;
    }

    public bool IsGround()
    {
        return m_Grounded;
    }

  
}

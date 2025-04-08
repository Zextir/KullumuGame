using UnityEngine;

public class Jump : MonoBehaviour
{
    [SerializeField, Range(0, float.PositiveInfinity)] float jumpForce = 5f;
    [SerializeField, Range(0, float.PositiveInfinity)] float maxJumpHeight = 1f;
    [SerializeField, Range(0, float.PositiveInfinity)] float jumpCooldown = 1f;

    float jumpHeight = 0f;
    float jumpTimer = 0f;
    Rigidbody player;
    GravityInDenseAir gravityController;

    bool finishedJumping = false;
    bool holding = false;

    Vector3 customGravity = Physics.gravity;

    float jumpAmount = 0f;


    private void Start()
    {
        player = GetComponent<Rigidbody>();
        gravityController = GetComponent<GravityInDenseAir>();
        player.useGravity = false;
    }

    void Update()
    {
        if (jumpTimer > 0f) jumpTimer -= Time.deltaTime;
        jumpAmount = Input.GetAxis("Jump");
        customGravity.y = gravityController.Gravity;
    }

    private void FixedUpdate()
    {
        player.AddForce(customGravity * player.mass);

        if (jumpAmount > 0)
        {
            if (jumpTimer <= 0 && !holding)
            {
                holding = true;
                jumpTimer = jumpCooldown;
            }

            if (finishedJumping) return;

            if (holding)
            {
                jumpHeight += jumpAmount * jumpForce * Time.fixedDeltaTime; // maybe sqrt
                jumpHeight = Mathf.Clamp(jumpHeight, 0, maxJumpHeight);
                Vector3 newVelocity = player.velocity + new Vector3(0, jumpHeight, 0);
                player.velocity = newVelocity;
            }

            if (jumpHeight >= maxJumpHeight) finishedJumping = true;
        }
        else
        {
            ResetJump();
        }
    }

    public void ResetJump()
    {
        holding = false;
        jumpHeight = 0f;
        finishedJumping = false;
    }
}



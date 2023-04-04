using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 5f;
    public float jumpForce = 7f;
    public float groundCheckDistance = 0.2f;
    public float coyoteTime = 0.1f;
    public float gravityScale = 1f;
    public string groundTag = "Ground";

    private Rigidbody2D rb2d;
    private bool isGrounded = false;
    private bool isJumping = false;
    private float coyoteTimer = 0f;

    private void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
        rb2d.gravityScale = gravityScale;
    }

    private void Update()
    {
        coyoteTimer -= Time.deltaTime;
        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
        }

        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) && (isGrounded || coyoteTimer > 0))
        {
            rb2d.velocity = new Vector2(rb2d.velocity.x, jumpForce);
            isJumping = true;
            coyoteTimer = 0;
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            rb2d.gravityScale = 3f;
        }
        else
        {
            rb2d.gravityScale = gravityScale;
        }
    }

    private void FixedUpdate()
    {
        float moveHorizontal = Input.GetAxis("Horizontal");

        Vector2 movement = new Vector2(moveHorizontal, 0f);

        rb2d.velocity = new Vector2(movement.x * speed, rb2d.velocity.y);

        isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, LayerMask.GetMask(groundTag));

        if (movement.x > 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else if (movement.x < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }

        if (!isGrounded)
        {
            coyoteTimer -= Time.deltaTime;
        }

        if (!isJumping && rb2d.velocity.y < 0)
        {
            rb2d.velocity += Vector2.up * Physics2D.gravity.y * (1.5f) * Time.deltaTime;
        }

        isJumping = false;
    }
}

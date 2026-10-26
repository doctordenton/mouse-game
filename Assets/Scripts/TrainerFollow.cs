using UnityEngine;
using UnityEngine.InputSystem;

public class TrainerFollow : MonoBehaviour

{
    public float moveSpeed = 3f;
    public float stopDistance = 1.5f;
    Animator myAnimator;
    SpriteRenderer mySpriteRenderer;

    void Start()
    {
        myAnimator = GetComponent<Animator>();
        mySpriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {

        // set mouselocation to current mouse positon
        Vector3 mouseLocation = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mouseLocation.z = 0;
        // distance is the distance between trainers position and mouse location
        float distance = Vector2.Distance(transform.position, mouseLocation);
        // if our distance from the cursor is greater than our stop distance, we are moving, set ourselves as moving
        bool isMoving = distance > stopDistance;

        if (isMoving)
        { // if we're moving, we're moving towards the mouse position 
            transform.position = Vector2.MoveTowards(transform.position, mouseLocation, moveSpeed * Time.deltaTime);

        }

        // if we are moving, tell the animator we're moving
        myAnimator.SetBool("isMoving", isMoving);
        // if the mouse's x position is greater than the player's position, turn the left facing sprite  -> facing right
        mySpriteRenderer.flipX = mouseLocation.x > transform.position.x;

    }
}
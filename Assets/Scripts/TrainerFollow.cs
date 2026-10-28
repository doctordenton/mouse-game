using UnityEngine;
using UnityEngine.InputSystem;

public class TrainerFollow : MonoBehaviour

{
    public float moveSpeed = 3f;
    public float stopDistance = 1.5f;
    public float startBuffer = 0.5f;   // mouse must get this much further than stopDistance before he starts walking again
    public float flipBuffer = 0.3f;    // mouse must be this far left/right of him before he turns around
    public float catchUp = 4f;         // how quickly he speeds up the further behind he is (higher = snappier)
    bool isMoving;                     // remembered between frames so he doesn't flicker on the line
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
        // stop when we get inside stopDistance, but only START again once the mouse is a bit further away.
        // that gap stops him flickering between walk and idle when the mouse sits right on the line
        // (the 0.05 is "close enough": he slows down as he arrives, so he'd never land exactly on it)
        if (isMoving && distance <= stopDistance + 0.05f)
        {
            isMoving = false;
        }
        else if (!isMoving && distance > stopDistance + startBuffer)
        {
            isMoving = true;
        }

        if (isMoving)
        { // if we're moving, we're moving towards the mouse position
            // the further past stopDistance the mouse is, the faster he walks (up to moveSpeed).
            // so if you drag slowly, he follows slowly and keeps walking instead of dash-stop-dash-stop
            float speed = Mathf.Min(moveSpeed, (distance - stopDistance) * catchUp);
            transform.position = Vector2.MoveTowards(transform.position, mouseLocation, speed * Time.deltaTime);

        }

        // if we are moving, tell the animator we're moving
        myAnimator.SetBool("isMoving", isMoving);
        // if the mouse is clearly to the right, face right; clearly to the left, face left.
        // when it's almost straight above/below him, keep facing the same way so he doesn't flicker
        if (mouseLocation.x > transform.position.x + flipBuffer)
        {
            mySpriteRenderer.flipX = true;    // left facing sprite -> facing right
        }
        else if (mouseLocation.x < transform.position.x - flipBuffer)
        {
            mySpriteRenderer.flipX = false;   // facing left
        }

    }
}
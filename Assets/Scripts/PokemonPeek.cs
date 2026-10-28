using System.Collections; //needed for IEnumerator
using UnityEngine;


public class PokemonPeek : MonoBehaviour
{
    [Header("Hiding back in the middle (random time between min and max)")]
    public float minWait = 1f;
    public float maxWait = 3f;
    public float peakDistance = 1f;
    public float idleTime = 2f;
    public float moveSpeed = 2f;
    public float treeClearance = 1.5f;   // a peek spot closer than this to another tree counts as blocked

    [Header("Sound")]
    public AudioClip cry;

    [Header("Sprite")]
    // denote facing left, set up to swap face right
    public bool spriteFacesLeft = true;
    public bool isPeeking;   // true while it's out in the open
    public Sprite normalSprite;
    public Sprite shinySprite;
    public bool showingShiny;   // true once the shiny look is actually on screen
    public bool holdHidden;     // RoundManager sets this between rounds: go hide and stay hidden
    public bool IsHidden { get { return isHidden; } }   // lets RoundManager check if we're behind the tree
    SpriteRenderer mySpriteRenderer;

    // tree swapping: a new species waits here until we're hidden behind the tree
    bool isHidden = true;
    bool hasNewLook;
    Sprite newNormal;
    Sprite newShiny;
    AudioClip newCry;
    string newName;
    bool wantShiny;

    Vector3 hiddenPos;
    Vector3 peekPos;

    void Awake()
    {
        mySpriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        // hovering needs a collider; add one sized to the sprite if we don't have one
        if (GetComponent<Collider2D>() == null)
        {
            gameObject.AddComponent<BoxCollider2D>();
        }
        // remember the hiding spot behind the tree
        hiddenPos = transform.localPosition;
        // the two places we could peek to
        Vector3 leftPos = hiddenPos + Vector3.left * peakDistance;
        Vector3 rightPos = hiddenPos + Vector3.right * peakDistance;
        bool leftClear = IsClearOfOtherTrees(leftPos);
        bool rightClear = IsClearOfOtherTrees(rightPos);

        bool peekRight;
        if (leftClear && rightClear)
        {
            //coin flip, < 0.5 is left, otherwise right
            peekRight = Random.Range(0f, 1f) >= 0.5f;
        }
        else if (rightClear)
        {
            peekRight = true;    // left side is blocked by another tree
        }
        else if (leftClear)
        {
            peekRight = false;   // right side is blocked by another tree
        }
        else
        {
            // both blocked: pick the side with more room
            peekRight = DistanceToNearestTree(rightPos) > DistanceToNearestTree(leftPos);
        }

        if (peekRight)
        {
            peekPos = rightPos;
            mySpriteRenderer.flipX = true;   // sprite faces left, so flip it when peeking right
        }
        else
        {
            peekPos = leftPos;
        }

        StartCoroutine(PeekLoop());
    }


    IEnumerator PeekLoop()
    {
        while (true)
        {
            //  hidden behind the tree: safe to change species or turn shiny now
            isHidden = true;
            ApplyLook();

            //  hidden: wait a random time (and keep waiting while RoundManager holds us between rounds)
            float wait = Random.Range(minWait, maxWait);
            while (wait > 0 || holdHidden)
            {
                wait -= Time.deltaTime;
                yield return null;
            }
            isHidden = false;

            //  slide out (stop early if we're told to hide)
            while (transform.localPosition != peekPos && !holdHidden)
            {
                transform.localPosition = Vector3.MoveTowards(transform.localPosition, peekPos, moveSpeed * Time.deltaTime);
                yield return null;
            }

            //  idle while peeking (cut short if we're told to hide)
            if (!holdHidden)
            {
                isPeeking = true;
                float idle = idleTime;
                while (idle > 0 && !holdHidden)
                {
                    idle -= Time.deltaTime;
                    yield return null;
                }
            }
            isPeeking = false;

            // slide back (twice as fast when we're told to hide)
            while (transform.localPosition != hiddenPos)
            {
                float speed = holdHidden ? moveSpeed * 2f : moveSpeed;
                transform.localPosition = Vector3.MoveTowards(transform.localPosition, hiddenPos, speed * Time.deltaTime);
                yield return null;
            }
        }
    }

    // true if this peek spot isn't on top of someone else's tree
    bool IsClearOfOtherTrees(Vector3 localSpot)
    {
        return DistanceToNearestTree(localSpot) >= treeClearance;
    }

    // how far a peek spot is from the closest OTHER Pokémon's tree
    float DistanceToNearestTree(Vector3 localSpot)
    {
        // localSpot is measured from our tree, so convert it to a real world position
        Vector3 worldSpot = transform.parent != null ? transform.parent.TransformPoint(localSpot) : localSpot;

        float nearest = Mathf.Infinity;
        foreach (PokemonPeek other in FindObjectsByType<PokemonPeek>(FindObjectsSortMode.None))
        {
            if (other == this || other.transform.parent == null)
            {
                continue;   // skip ourselves, and any Pokémon without a tree
            }
            float distance = Vector2.Distance(worldSpot, other.transform.parent.position);
            if (distance < nearest)
            {
                nearest = distance;
            }
        }
        return nearest;
    }

    public void SetShiny(bool isShiny)
    {
        wantShiny = isShiny;

        if (!isShiny)
        {
            // turning back to normal can happen right away
            showingShiny = false;
            mySpriteRenderer.sprite = normalSprite;
        }

        // turning shiny waits until we're hidden, so nobody sees it change in the open
        if (isHidden)
        {
            ApplyLook();
        }
    }

    // RoundManager calls this to move a different species onto this tree
    public void SwapIn(Sprite normal, Sprite shiny, AudioClip newCryClip, string speciesName)
    {
        newNormal = normal;
        newShiny = shiny;
        newCry = newCryClip;
        newName = speciesName;
        hasNewLook = true;

        if (isHidden)
        {
            ApplyLook();
        }
    }

    // put on the new species (if any) and the right shiny/normal picture
    void ApplyLook()
    {
        if (hasNewLook)
        {
            normalSprite = newNormal;
            shinySprite = newShiny;
            cry = newCry;
            gameObject.name = newName;
            hasNewLook = false;
        }

        showingShiny = wantShiny;
        Sprite look = wantShiny ? shinySprite : normalSprite;   // shiny or normal picture
        if (look != null)   // empty slot? keep whatever picture we already have
        {
            mySpriteRenderer.sprite = look;
        }
    }
}

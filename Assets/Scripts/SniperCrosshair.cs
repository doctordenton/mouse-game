using UnityEngine;

// The sniper's crosshair. RoundManager calls StartHunting() when the Alert starts.
// For most of the alert it sweeps between random Pokémon, searching (so it doesn't give away the shiny).
// Near the end it locks onto the real target, shrinking and turning red as time runs out.
public class SniperCrosshair : MonoBehaviour
{
    [Header("Searching")]
    public float searchSpeed = 6f;       // how fast it sweeps around while searching
    public float lockOnTime = 1.5f;      // seconds before the end when it locks onto the real target
    public float lockOnSpeed = 15f;      // how fast it snaps onto the target

    [Header("Look")]
    public float searchScale = 1.5f;     // size while searching
    public float lockedScale = 0.5f;     // size when the shot is about to fire
    public Color searchColor = Color.white;
    public Color lockedColor = Color.red;

    SpriteRenderer mySpriteRenderer;
    PokemonPeek[] pokemon;   // everyone it can sweep past
    Transform target;        // the shiny it's really after
    Transform searchSpot;    // the Pokémon it's currently sweeping toward
    float timeLeft;

    void Awake()
    {
        mySpriteRenderer = GetComponent<SpriteRenderer>();
        Hide();
    }

    void Update()
    {
        if (target == null)
        {
            return;
        }

        timeLeft -= Time.deltaTime;

        if (timeLeft > lockOnTime)
        {
            // searching: sweep to a random Pokémon, then pick another one
            transform.position = Vector2.MoveTowards(transform.position, searchSpot.position, searchSpeed * Time.deltaTime);
            if (Vector2.Distance(transform.position, searchSpot.position) < 0.1f)
            {
                PickSearchSpot();
            }
            transform.localScale = Vector3.one * searchScale;
            mySpriteRenderer.color = searchColor;
        }
        else
        {
            // locked on: snap to the target, shrink and turn red as the shot gets closer
            transform.position = Vector2.MoveTowards(transform.position, target.position, lockOnSpeed * Time.deltaTime);
            float progress = 1f - (timeLeft / lockOnTime);   // 0 when it locks on, 1 when it fires
            transform.localScale = Vector3.one * Mathf.Lerp(searchScale, lockedScale, progress);
            mySpriteRenderer.color = Color.Lerp(searchColor, lockedColor, progress);
        }
    }

    public void StartHunting(Transform newTarget, PokemonPeek[] allPokemon, float alertTime)
    {
        target = newTarget;
        pokemon = allPokemon;
        timeLeft = alertTime;
        PickSearchSpot();
        transform.position = searchSpot.position;   // start on a random Pokémon
        mySpriteRenderer.enabled = true;
    }

    public void Hide()
    {
        target = null;
        mySpriteRenderer.enabled = false;
    }

    void PickSearchSpot()
    {
        searchSpot = pokemon[Random.Range(0, pokemon.Length)].transform;
    }
}

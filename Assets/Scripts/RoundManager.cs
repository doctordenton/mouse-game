using System.Collections;   // needed for IEnumerator (the throw coroutine)
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
public enum RoundState { Start, Calm, Alert, Throw, Break, GameOver }
public class RoundManager : MonoBehaviour
{
    public RoundState state;

    [Header("Panels")]
    public GameObject startPanel;
    public GameObject hudPanel;
    public GameObject gameOverPanel;

    [Header("Text")]
    public TMP_Text scoreText;
    public TMP_Text timerText;
    public TMP_Text finalScoreText;
    public TMP_Text highScoreText;

    [Header("Timing")]
    public float minCalm = 2f;          // shortest calm time
    public float maxCalm = 5f;          // longest calm time
    public float startAlertTime = 5f;   // seconds to save the target in round 1
    public float minAlertTime = 1f;     // never faster than this
    float alertTime;                    // this round's time (shrinks each save)
    float timer;                        // counts down in Calm and Alert

    [Header("Pokemon")]
    public PokemonPeek[] pokemon;       // drag all your Pokémon in here
    PokemonPeek target;                 // the one the sniper picked

    [Header("Sniper")]
    public SniperCrosshair crosshair;   // drag the Crosshair object in here
    public AudioClip loadSound;         // the sniper loading: plays just after the shiny's cry when the alert starts
    public AudioClip shotSound;         // the sniper fires: plays when time runs out
    public AudioClip screamSound;       // plays just after the shot (drop the Tom & Jerry scream in here)
    public float soundGap = 0.5f;       // seconds between cry -> load, and shot -> scream

    [Header("Music")]
    public AudioClip music;             // background music: loops the whole time, never restarts between rounds
    [Range(0f, 1f)]
    public float musicVolume = 0.4f;    // keep it lower than the sound effects
    AudioSource musicSource;
    public float cryVolume = 1.5f;      // shiny's cry loudness (1 = normal, above 1 boosts it)
    public float cryPitch = 1.3f;       // shiny's cry speed (1 = normal, higher = faster AND higher-pitched)

    [Header("Saving")]
    public float holdTime = 2f;         // seconds you must hover over the shiny to throw
    public UnityEngine.UI.Image holdBar; // optional: a Filled image that shows hold progress
    public Sprite pokeballSprite;       // drag pokeball.png in here: it charges up at the mouse while you hover
    public float chargeBallSize = 0.8f; // how big the charge ball gets when full (world units)
    public float drainSpeed = 2f;       // when you slip off, charge drains this many times faster than it fills
    float holdTimer;                    // how long you've been hovering so far
    SpriteRenderer chargeBall;          // the Poké Ball that appears at the mouse while charging

    [Header("Throwing")]
    public Transform trainer;           // drag the Trainer in here: the ball is thrown from him
    public float throwTime = 0.5f;      // seconds the ball is in the air
    public float arcHeight = 1.5f;      // how high the ball arcs above a straight line
    public float spinSpeed = 720f;      // degrees per second the ball spins in the air
    public AudioClip throwSound;        // optional: plays when the ball leaves his hand
    public AudioClip catchSound;        // optional: plays when the ball reaches the shiny
    public float breakTime = 3f;        // seconds between a save and the next round

    AudioSource sfxSource;            // plays the alert and shot sounds
    AudioSource crySource;              // plays the cry, separate so its pitch doesn't change the others

    int score;
    int highScore;
    int[] topScores = new int[5];       // best 5 scores, highest first (saved between games)

    // every species' look and cry, copied from the Pokémon at the start,
    // so we can shuffle them onto different trees each round
    Sprite[] speciesNormal;
    Sprite[] speciesShiny;
    AudioClip[] speciesCry;
    string[] speciesName;

    void Awake()
    {
        // make two speakers on this object, one for sound effects and one for the cry
        sfxSource = gameObject.AddComponent<AudioSource>();
        crySource = gameObject.AddComponent<AudioSource>();

        // a third speaker just for music, so sound effects never interrupt it
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;

        // make the charge ball (hidden until you hover the shiny)
        chargeBall = new GameObject("Charge Ball").AddComponent<SpriteRenderer>();
        chargeBall.sprite = pokeballSprite;
        chargeBall.sortingOrder = 30;   // on top of trees, Pokémon and the crosshair
        chargeBall.enabled = false;
    }

    void Start()
    {
        // remember each species from how you set them up in the Inspector
        int count = pokemon.Length;
        speciesNormal = new Sprite[count];
        speciesShiny = new Sprite[count];
        speciesCry = new AudioClip[count];
        speciesName = new string[count];
        for (int i = 0; i < count; i++)
        {
            speciesNormal[i] = pokemon[i].normalSprite;
            speciesShiny[i] = pokemon[i].shinySprite;
            speciesCry[i] = pokemon[i].cry;
            speciesName[i] = pokemon[i].name;
        }

        LoadTopScores();   // bring back the scores from last time

        // start the music once. nothing else ever stops it, so it plays straight through every round
        if (music != null)
        {
            musicSource.clip = music;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }
        EnterStart();
    }

    // shuffle which species sits behind which tree.
    // each Pokémon only changes while it's hidden, so you never see it swap in the open
    void ShuffleTrees()
    {
        // make a list 0,1,2,...,9 and shuffle it (Fisher-Yates shuffle)
        int[] order = new int[pokemon.Length];
        for (int i = 0; i < order.Length; i++)
        {
            order[i] = i;
        }
        for (int i = order.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);   // int Range: top left out, so 0 to i
            int temp = order[i];
            order[i] = order[j];
            order[j] = temp;
        }

        // tree i gets species order[i]
        for (int i = 0; i < pokemon.Length; i++)
        {
            int s = order[i];
            pokemon[i].SwapIn(speciesNormal[s], speciesShiny[s], speciesCry[s], speciesName[s]);
        }
    }

    void Update()
    {
        switch (state)
        {
            case RoundState.Start:
                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    score = 0;
                    SetText(scoreText, "score: " + score);
                    alertTime = startAlertTime;
                    EnterCalm();
                    HideAll(false);   // game on: let everyone start peeking
                }
                // TESTING: Delete wipes the saved top 5
                else if (Keyboard.current.deleteKey.wasPressedThisFrame)
                {
                    ResetTopScores();
                }
                break;

            case RoundState.Calm:
                timer -= Time.deltaTime;
                // only lock on once calm time is up AND the shiny is out in the open
                // (showingShiny: it has actually turned shiny, which waits until it's been behind its tree)
                if (timer <= 0 && target.isPeeking && target.showingShiny)
                {
                    EnterAlert();
                }
                break;

            case RoundState.Alert:
                timer -= Time.deltaTime;
                SetText(timerText, "time: " + Mathf.CeilToInt(timer));   // whole number for display only

                // hovering over the shiny (or its tree) fills up the hold timer,
                // moving off drains it back down instead of wiping it instantly
                if (IsMouseOverTarget())
                {
                    if (holdTimer == 0)
                    {
                        Debug.Log("Hovering the shiny " + target.name + ", charging...");   // feedback: started
                    }
                    holdTimer += Time.deltaTime;
                }
                else if (holdTimer > 0)
                {
                    holdTimer -= Time.deltaTime * drainSpeed;
                    if (holdTimer <= 0)
                    {
                        holdTimer = 0;
                        Debug.Log("Moved off the shiny, charge lost");   // feedback: lost it
                    }
                }
                SetHoldBar(holdTimer / holdTime);

                // held long enough = throw the Poké Ball (TESTING: S throws instantly)
                if (holdTimer >= holdTime || Keyboard.current.sKey.wasPressedThisFrame)
                {
                    Debug.Log("Charged! Throwing at " + target.name);   // feedback: success
                    EnterThrow();
                }
                else if (timer <= 0)       // the sniper wins
                {
                    Debug.Log("Too slow! The sniper got " + target.name);   // feedback: fail
                    // bang, then the scream half a second later
                    PlaySound(shotSound);
                    StartCoroutine(PlayAfter(screamSound, soundGap));
                    EnterGameOver();
                }
                break;

            case RoundState.Break:
                // nothing to do here: the BetweenRounds coroutine runs the pause
                break;

            case RoundState.Throw:
                // nothing to do here: the ThrowBall coroutine flies the ball,
                // and the sniper's countdown is paused because we're out of the Alert case
                break;

            case RoundState.GameOver:
                if (Keyboard.current.spaceKey.wasPressedThisFrame)
                {
                    EnterStart();
                }
                break;
        }
    }

    void EnterStart()
    {
        state = RoundState.Start;
        startPanel.SetActive(true);
        hudPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        crosshair.Hide();
        HideAll(true);   // nobody peeks until you press Space
    }

    void EnterCalm()
    {
        state = RoundState.Calm;
        startPanel.SetActive(false);
        hudPanel.SetActive(true);
        gameOverPanel.SetActive(false);
        crosshair.Hide();

        // calm lasts a random time between minCalm and maxCalm
        timer = Random.Range(minCalm, maxCalm);
        SetText(timerText, "");

        // turn the old shiny back to normal (on round 1 there isn't one yet)
        ClearTarget();

        // everybody switches trees (each one swaps the next time it hides)
        ShuffleTrees();

        // pick a new random target, but never the same one twice in a row
        PokemonPeek newTarget = pokemon[Random.Range(0, pokemon.Length)];   // Length is 10, top left out, so 0-9
        while (newTarget == target && pokemon.Length > 1)
        {
            newTarget = pokemon[Random.Range(0, pokemon.Length)];   // same one again? roll again
        }
        target = newTarget;
        target.SetShiny(true);
        Debug.Log("New round: everyone switched trees, a shiny will appear once it's been behind its tree");
    }

    void EnterAlert()
    {
        state = RoundState.Alert;
        timer = alertTime;
        holdTimer = 0;
        Debug.Log("ALERT! The sniper is hunting " + target.name + ". You have " + alertTime + " seconds");

        // the cue: the shiny's cry, then half a second later the sniper loading,
        // and the crosshair starts searching
        // (it only locks onto the shiny near the end, so it doesn't give it away)
        PlayCry(target.cry);
        StartCoroutine(PlayAfter(loadSound, soundGap));
        crosshair.StartHunting(target.transform, pokemon, alertTime);
    }

    void EnterThrow()
    {
        state = RoundState.Throw;
        SetHoldBar(0);              // hide the charge ball, it's being thrown now
        crosshair.enabled = false;  // freeze the crosshair where it is while the ball flies
        StartCoroutine(ThrowBall());
    }

    IEnumerator ThrowBall()
    {
        // no ball sprite set: skip the animation and just count the save
        if (pokeballSprite == null)
        {
            crosshair.enabled = true;
            Saved();
            yield break;
        }

        // make the ball at the trainer (or at the mouse if no trainer is set)
        SpriteRenderer ball = new GameObject("Thrown Ball").AddComponent<SpriteRenderer>();
        ball.sprite = pokeballSprite;
        ball.sortingOrder = 30;
        ball.transform.localScale = Vector3.one * (chargeBallSize / pokeballSprite.bounds.size.x);

        Vector3 start = trainer != null ? trainer.position : chargeBall.transform.position;
        PlaySound(throwSound);

        // fly from start to the shiny over throwTime seconds
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime / throwTime;

            // aim at where the shiny is NOW (it might be sliding)
            Vector3 end = target.transform.position;

            // straight line from start to end (Lerp walks along end - start, the direction vector)
            Vector3 straight = Vector3.Lerp(start, end, t);
            // plus a hump: 0 at the start, highest in the middle, 0 at the end
            float hump = Mathf.Sin(t * Mathf.PI) * arcHeight;
            ball.transform.position = straight + Vector3.up * hump;

            ball.transform.Rotate(0, 0, -spinSpeed * Time.deltaTime);
            yield return null;
        }

        // it landed: the shiny is saved
        PlaySound(catchSound);
        Destroy(ball.gameObject);
        crosshair.enabled = true;
        Debug.Log("Caught! " + target.name + " is safe");
        Saved();
    }

    void Saved()
    {
        score++;
        SetText(scoreText, "score: " + score);

        alertTime -= 0.5f;                  // each save, the sniper gets half a second faster
        if (alertTime < minAlertTime)       // went too low?
        {
            alertTime = minAlertTime;       // stay at the minimum
        }

        StartCoroutine(BetweenRounds());
    }

    // the pause after a save: everyone hides, waits, then the next round starts
    // (EnterCalm shuffles the trees while they're all still hidden)
    IEnumerator BetweenRounds()
    {
        state = RoundState.Break;
        crosshair.Hide();
        ClearTarget();
        SetText(timerText, "Saved! Next round...");
        Debug.Log("Break: everyone hide");

        // tell everyone to go hide and stay hidden
        HideAll(true);

        // wait the break time (and make sure everyone has made it behind their tree)
        float waited = 0;
        while (waited < breakTime || !AllHidden())
        {
            waited += Time.deltaTime;
            yield return null;
        }

        // new round: shuffle + new shiny happen in EnterCalm while they're all hidden, then let them go
        EnterCalm();
        HideAll(false);
    }

    // true = everyone slides behind their tree and stays there, false = back to peeking
    void HideAll(bool hide)
    {
        foreach (PokemonPeek p in pokemon)
        {
            p.holdHidden = hide;
        }
    }

    bool AllHidden()
    {
        foreach (PokemonPeek p in pokemon)
        {
            if (!p.IsHidden)
            {
                return false;
            }
        }
        return true;
    }

    void EnterGameOver()
    {
        // clear the shiny so the next game starts clean
        ClearTarget();
        crosshair.Hide();
        HideAll(true);   // everyone runs and hides behind their trees

        state = RoundState.GameOver;
        hudPanel.SetActive(false);
        gameOverPanel.SetActive(true);

        // add this game's score to the top 5 and save it to disk
        AddToTopScores(score);
        highScore = topScores[0];   // the best score is always first in the list

        SetText(finalScoreText, "score: " + score);
        SetText(highScoreText, TopScoresList());
    }

    // ---------- top 5 scores, saved between games with PlayerPrefs ----------

    // read the saved top 5 from disk (0 if nothing saved yet)
    void LoadTopScores()
    {
        for (int i = 0; i < topScores.Length; i++)
        {
            topScores[i] = PlayerPrefs.GetInt("topScore" + i, 0);
        }
        highScore = topScores[0];
    }

    // slot a new score into the list (highest first), then save
    void AddToTopScores(int newScore)
    {
        for (int i = 0; i < topScores.Length; i++)
        {
            if (newScore > topScores[i])
            {
                // push everything below this spot down one, the last one falls off
                for (int j = topScores.Length - 1; j > i; j--)
                {
                    topScores[j] = topScores[j - 1];
                }
                topScores[i] = newScore;
                break;
            }
        }

        for (int i = 0; i < topScores.Length; i++)
        {
            PlayerPrefs.SetInt("topScore" + i, topScores[i]);
        }
        PlayerPrefs.Save();
    }

    // wipe the saved scores (press Delete on the start screen)
    void ResetTopScores()
    {
        for (int i = 0; i < topScores.Length; i++)
        {
            topScores[i] = 0;
            PlayerPrefs.DeleteKey("topScore" + i);
        }
        PlayerPrefs.Save();
        highScore = 0;
        Debug.Log("Top scores reset");
    }

    // builds the text: "TOP 5" then one line per score
    string TopScoresList()
    {
        string list = "TOP 5";
        for (int i = 0; i < topScores.Length; i++)
        {
            list += "\n" + (i + 1) + ".  " + topScores[i];
        }
        return list;
    }

    // turn the current shiny back to normal
    void ClearTarget()
    {
        if (target != null)
        {
            target.SetShiny(false);
        }
        SetHoldBar(0);
    }

    // is the mouse over the shiny OR the tree it hides behind?
    // counting the tree means you keep charging when it ducks back in,
    // and a player who remembers which tree it's behind can still save it
    bool IsMouseOverTarget()
    {
        Vector3 mouseLocation = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

        // start with the shiny itself, wherever it is right now
        Bounds hoverArea = target.GetComponent<SpriteRenderer>().bounds;

        // stretch the area to cover its tree too
        if (target.transform.parent != null)
        {
            SpriteRenderer tree = target.transform.parent.GetComponent<SpriteRenderer>();
            if (tree != null)
            {
                hoverArea.Encapsulate(tree.bounds);
            }
        }

        mouseLocation.z = hoverArea.center.z;   // ignore depth, we only care about x and y
        return hoverArea.Contains(mouseLocation);
    }

    // shows hold progress (0 = empty, 1 = full) on the hold bar and the charge ball
    void SetHoldBar(float amount)
    {
        if (holdBar != null)
        {
            holdBar.fillAmount = amount;
        }

        if (chargeBall == null || pokeballSprite == null)
        {
            return;
        }

        // only show the ball while charging
        chargeBall.enabled = amount > 0;
        if (amount <= 0)
        {
            return;
        }

        // sit just up-right of the mouse
        Vector3 mouseLocation = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mouseLocation.z = 0;
        chargeBall.transform.position = mouseLocation + new Vector3(0.6f, 0.6f, 0);

        // grow from small to full size, and fade from see-through to solid
        float fullScale = chargeBallSize / pokeballSprite.bounds.size.x;
        chargeBall.transform.localScale = Vector3.one * fullScale * Mathf.Lerp(0.3f, 1f, amount);
        chargeBall.color = Color.Lerp(new Color(1f, 1f, 1f, 0.4f), Color.white, amount);

        // wiggle harder as it gets closer to full, like it's ready to throw
        chargeBall.transform.rotation = Quaternion.Euler(0, 0, Mathf.Sin(Time.time * 30f) * 20f * amount);
    }

    // writes to a text, skips it if the slot is empty (so a missing text can't crash the game)
    void SetText(TMP_Text label, string message)
    {
        if (label != null)
        {
            label.text = message;
        }
    }

    // waits a moment, then plays a sound effect
    IEnumerator PlayAfter(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);
        PlaySound(clip);
    }

    // plays a sound effect, skips it if the slot is empty
    void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    // plays the shiny's cry louder and faster than normal
    void PlayCry(AudioClip clip)
    {
        if (clip != null)
        {
            crySource.pitch = cryPitch;
            crySource.PlayOneShot(clip, cryVolume);
        }
    }
}

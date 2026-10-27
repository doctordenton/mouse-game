using UnityEngine;
using System.Collections; //needed for IEnumerator


public class PokemonPeek : MonoBehaviour
{
    [Header("Hiding back in the middle (random time between min and max)")]
    public float minWait = 1f;
    public float maxWait = 3f;
    public float peakDistance = 1f;
    public float idleTime = 2f;
    public float moveSpeed = 2f;
    public AudioClip cry;

    Vector3 hiddenPos;
    Vector3 peekPos;
    [Header("Sound")]
   


    [Header("Sprite")]
    // denote facing left, set up to swap face right
    public bool spriteFacesLeft = true;
    void Start()
    {
        transform.localPosition = hiddenPos;
        peekPos =  ???;
    }


    IEnumerator PeekLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minWait, maxWait));
        }
    }




    // Update is called once per frame
    void Update()
    {
        
    }
}

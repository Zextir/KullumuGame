using UnityEngine;

[RequireComponent(typeof(Collider))]
// typeof(AudioSource), 
public class ShrinkOnGroundHit : MonoBehaviour
{
    public float shrinkDuration = 10f;

    private Vector3 initialScale;
    private bool hasHitGround = false;
    private float timer = 0f;
    // private AudioSource audioSource;

    void Start()
    {
        initialScale = transform.localScale;
        // audioSource = GetComponent<AudioSource>();
        // audioSource.loop = false; // Ensure it plays only once
    }

    void Update()
    {
        if (hasHitGround)
        {
            timer += Time.deltaTime;
            float t = timer / shrinkDuration;
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, t);

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!hasHitGround && collision.gameObject.CompareTag("Ground"))
        {
            hasHitGround = true;
            timer = 0f;

            // if (audioSource != null && !audioSource.isPlaying)
            // {
            //    audioSource.Play(); // Plays once on first hit
            // }
        }
    }
}

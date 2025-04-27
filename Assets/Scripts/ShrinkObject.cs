using UnityEngine;

public class ShrinkObject : MonoBehaviour
{
    private Vector3 initialScale;
    private float delayBeforeShrink = 20f;
    private float shrinkDuration = 10f;
    private float timer = 0f;
    private bool shrinking = false;

    void Start()
    {
        initialScale = transform.localScale;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (!shrinking && timer >= delayBeforeShrink)
        {
            shrinking = true;
            timer = 0f; // reset timer for shrinking phase
        }

        if (shrinking)
        {
            float t = timer / shrinkDuration;
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, t);

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}

using System.Collections;
using Opsive.UltimateCharacterController.Character;
using UnityEngine;

public class BouncyPlant : MonoBehaviour
{
    enum State { IDLE, SHRINKING, GROWING }
    State state = State.IDLE;

    [SerializeField] float bounceForce;

    [SerializeField] float shrinkTime = 1f;
    [SerializeField] float shrunkScale = 0.9f;

    [SerializeField] float shrunkPauseTime = 1f;

    [SerializeField] float growTime = 0.5f;

    private IEnumerator ChangeSize(float changeTime)
    {
        if (state == State.IDLE) yield break;

        float timer = 0;

        yield return new WaitForSeconds(3 * Time.fixedDeltaTime);

        while (timer <= changeTime)
        {
            timer += Time.deltaTime;
            float t = timer / changeTime;

            if (state == State.SHRINKING)
            {
                //Debug.Log("shrink: " + t);
                //t = t * t;
                SetScaleY(Mathf.Lerp(1, shrunkScale, t));
            }
            else
            {
                //Debug.Log("grow: " + t);
                SetScaleY(Mathf.Lerp(shrunkScale, 1, t));
            }

            yield return null;
        }

        if (state == State.SHRINKING)
        {
            SetScaleY(shrunkScale);
            yield return new WaitForSeconds(shrunkPauseTime);
            state = State.GROWING;
            StartCoroutine(ChangeSize(growTime));
        }
        else
        {
            SetScaleY(1f);
            state = State.IDLE;
        }
    }

    private void SetScaleY(float scale)
    {
        foreach (Transform child in transform.parent)
        {
            Vector3 fullScale = child.localScale;
            fullScale.y = scale;
            child.localScale = fullScale;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (state != State.IDLE || !other.CompareTag("Player")) return;

        var ucl = other.GetComponent<UltimateCharacterLocomotion>();
        
        if (ucl == null || ucl.Velocity.y > 0) return;

        state = State.SHRINKING;
        StartCoroutine(ChangeSize(shrinkTime));
    }

    private void OnTriggerStay(Collider other)
    {
        if (state != State.GROWING) return;

        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb == null) return;

        Vector3 forceDirection = transform.up;
        float forceMultiplier = rb.velocity.y > 0 ? 5 : 1;
        Vector3 force = rb.mass * forceDirection * bounceForce * forceMultiplier;

        if (other.CompareTag("Player"))
        {
            var ucl = other.GetComponent<UltimateCharacterLocomotion>();
            if (ucl == null) return;
            ucl.AddForce(force);
            Debug.Log("launched player");
        }
        else
        {
            rb.AddForce(force);
        }

    }

}

using UnityEngine;

public class FloatInDenseAir : MonoBehaviour
{

    [SerializeField] float floatDuration = 2f;
    [SerializeField] float floatForce = 0.7f;

    bool floatingUpward = true;
    float floatCooldown = 0f;

    private void Start()
    {
        floatDuration += Random.Range(-0.5f, 0.5f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<DenseAir>() != null)
        {
            floatCooldown = 0f;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && other.GetComponent<DenseAir>() != null)
        {

            floatCooldown += Time.deltaTime;
            if (floatCooldown > floatDuration)
            {
                floatCooldown = 0;
                floatingUpward = !floatingUpward;
                rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            }
            rb.AddForce((floatingUpward ? Vector3.up : Vector3.down) * floatForce);
        }
    }
}

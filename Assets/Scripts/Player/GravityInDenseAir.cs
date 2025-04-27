using UnityEngine;

public class GravityInDenseAir : MonoBehaviour
{
    float gravity = Physics.gravity.y;

    public float Gravity
    {
        get => gravity;
    }

    private void OnTriggerEnter(Collider other)
    {
        DenseAir denseAir = other.GetComponent<DenseAir>();
        if (denseAir == null) return;

        float density = denseAir.Density;
        gravity = Physics.gravity.y / density;
    }

    private void OnTriggerExit(Collider other)
    {
        DenseAir denseAir = other.GetComponent<DenseAir>();
        if (denseAir == null) return;
        gravity = Physics.gravity.y;
    }
}

using UnityEngine;

public class DensityGravityHandler : MonoBehaviour
{
    float gravityMultiplier = 1;

    public float GravityMultiplier => gravityMultiplier;

    private void OnTriggerExit(Collider other)
    {
        DenseAir denseAir = other.GetComponent<DenseAir>();
        if (denseAir == null) return;
        gravityMultiplier = 1;
    }

    private void OnTriggerEnter(Collider other)
    {
        DenseAir denseAir = other.GetComponent<DenseAir>();
        if (denseAir == null) return;

        float density = denseAir.Density;
        float gravityModifier = (density - 1) * 3 + 1;
        if (gravityModifier < 0)
        {
            gravityMultiplier = 100;
        }
        else
        {
            gravityMultiplier = 1 / gravityModifier;
        }
    }


}

using Opsive.UltimateCharacterController.Character;
using UnityEngine;

public class DensityGravityHandler : MonoBehaviour
{
    float gravityMultiplier = 1;
    UltimateCharacterLocomotion ucl;
    Glide glide;

    public float GravityMultiplier => gravityMultiplier;

    private void OnEnable()
    {
        ucl = GetComponent<UltimateCharacterLocomotion>();
        if (ucl != null) glide = ucl.GetAbility<Glide>();
    }

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

    private void Update()
    {
        if (ucl == null) return;
        float modifiedGravity = gravityMultiplier;
        if (glide != null && glide.IsActive) modifiedGravity *= glide.GravityMultiplier;
        ucl.GravityAmount = modifiedGravity;
    }

}

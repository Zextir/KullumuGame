using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities;
using UnityEngine;

public class DensityGravityHandler : MonoBehaviour
{
    float gravityMultiplier = 1;
    UltimateCharacterLocomotion ucl;
    Glide glide;
    Fall fall;

    public float GravityMultiplier => gravityMultiplier;

    private void OnEnable()
    {
        ucl = GetComponent<UltimateCharacterLocomotion>();
        if (ucl != null)
        {
            glide = ucl.GetAbility<Glide>();
            fall = ucl.GetAbility<Fall>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        DenseAir denseAir = other.GetComponent<DenseAir>();
        if (denseAir == null) return;
        gravityMultiplier = 1;
    }

    private void OnTriggerStay(Collider other)
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
        if (fall != null) fall.InFloat = gravityMultiplier != 1;

        ucl.GravityAmount = modifiedGravity;
    }

}

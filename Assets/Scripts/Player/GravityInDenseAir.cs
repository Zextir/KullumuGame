using Opsive.UltimateCharacterController.Character;
using UnityEngine;

public class GravityInDenseAir : MonoBehaviour
{
    UltimateCharacterLocomotion UCharacterLocomotion;

    private void OnEnable()
    {
        UCharacterLocomotion = GetComponent<UltimateCharacterLocomotion>();
    }

    private void OnTriggerExit(Collider other)
    {
        DenseAir denseAir = other.GetComponent<DenseAir>();
        if (denseAir == null) return;
        UCharacterLocomotion.GravityAmount = 1;
    }

    private void OnTriggerEnter(Collider other)
    {
        DenseAir denseAir = other.GetComponent<DenseAir>();
        if (denseAir == null) return;

        float density = denseAir.Density;
        float gravityModifier = (density - 1) * 3 + 1;
        if (gravityModifier < 0)
        {
            UCharacterLocomotion.GravityAmount = 100;
        }
        else
        {
            UCharacterLocomotion.GravityAmount = 1 / gravityModifier;
        }
    }
}

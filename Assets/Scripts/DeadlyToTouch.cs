using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities;
using UnityEngine;

public class DeadlyToTouch : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        ActivateDeath(collision.gameObject);

    }

    private void OnTriggerEnter(Collider other)
    {
        ActivateDeath(other.gameObject);
    }

    private void ActivateDeath(GameObject toKill)
    {
        print ($"DeadlyToTouch: {toKill.name} touched {gameObject.name}");
        var ucl = toKill.GetComponent<UltimateCharacterLocomotion>();
        if (ucl != null)
        {
            // TODO: Add some timer, so you have time to avoid death
            //      -> keep in mind that if you fall too long into lava, you get below the graphics.
            ucl.GetAbility<Die>().StartAbility();
        }
    }    
}

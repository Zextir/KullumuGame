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
        var ucl = toKill.GetComponent<UltimateCharacterLocomotion>();
        if (ucl != null)
        {
            ucl.GetAbility<Die>().StartAbility();
        }
    }    
}

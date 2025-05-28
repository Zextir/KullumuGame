using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SmallCreatureActivator : MonoBehaviour
{
    public SmallCreatureAI[] creatures;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            foreach (var creature in creatures)
            {
                creature.ActivateAI();
            }

            gameObject.SetActive(false);
        }
    }
}


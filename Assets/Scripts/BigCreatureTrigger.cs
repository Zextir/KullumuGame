using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyTrigger : MonoBehaviour
{
    public GameObject enemy;
    public GameObject player;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject != player) return;
        
        enemy.SetActive(true);
        gameObject.SetActive(false);
        Debug.Log("Enemy activated!");
    }

}

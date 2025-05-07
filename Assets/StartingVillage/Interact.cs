using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interact : MonoBehaviour

{
    public GameObject popupPanel;  // The UI panel to show
    public Transform player; // The player character (to detect if inside trigger)

    private bool isPlayerInRange = false; // To track whether the player is inside the trigger

    void Start()
    {
        // Ensure the popup panel is inactive initially
        popupPanel.SetActive(false);
    }

    void Update()
    {
        // If the player is inside the trigger zone, show the popup panel
        if (isPlayerInRange)
        {
            popupPanel.SetActive(true);
        }
        else
        {
            popupPanel.SetActive(false);
        }
    }

    // When the player enters the trigger zone
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Character")) // Check if the player has entered the trigger
        {
            isPlayerInRange = true; // Player is inside the trigger zone
            Debug.Log("Player entered interaction range.");
        }
    }

    // When the player exits the trigger zone
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Character")) // Check if the player has exited the trigger
        {
            isPlayerInRange = false; // Player is no longer inside the trigger zone
            Debug.Log("Player exited interaction range.");
        }
    }
}


























// {
//     public float interactionRange = 3f; // Range to interact
//     public GameObject popupPanel; // The UI panel to show
//     public Transform rayOrigin; // Typically your Camera (the "eye" of the player)

//     private GameObject currentTarget; // The object the player is currently interacting with

//     void Update()
//     {
//         DetectInteractable(); // Detect interactable objects
//     }

//     void DetectInteractable()
//     {
//         // Cast a ray from the camera's position forward
//         Ray ray = new Ray(rayOrigin.position, rayOrigin.forward);
//         RaycastHit hit;

//         // Visualize the ray in the scene (for debugging)
//         Debug.DrawRay(ray.origin, ray.direction * interactionRange, Color.green);

//         if (Physics.Raycast(ray, out hit, interactionRange))
//         {
//             // Get the root GameObject of the hit object
//             GameObject hitObject = hit.collider.transform.root.gameObject;

//             // Check if the hit object has the "Interactable" tag
//             if (hitObject.CompareTag("Interactable"))
//             {
//                 // If we're looking at a new object, update and show the popup
//                 if (currentTarget != hitObject)
//                 {
//                     currentTarget = hitObject;
//                     popupPanel.SetActive(true); // Show the panel
//                     Debug.Log("Entered interaction range of: " + hitObject.name);
//                 }
//                 return;
//             }
//         }

//         // If the player is no longer near an interactable object, hide the popup
//         if (currentTarget != null)
//         {
//             popupPanel.SetActive(false); // Hide the panel
//             Debug.Log("Exited interaction range.");
//             currentTarget = null;
//         }
//     }
// }





















// {
//     public float interactionRange = 3f;
//     public GameObject popupPanel;
//     private GameObject currentTarget;

//     void Update()
//     {
//         DetectInteractable();

//         if (currentTarget != null && Input.GetKeyDown(KeyCode.E))
//         {
//             popupPanel.SetActive(true);
//             Debug.Log("Interaction triggered with " + currentTarget.name);
//         }
//     }

//     void DetectInteractable()
//     {
//         Ray ray = new Ray(transform.position, transform.forward);
//         RaycastHit hit;

//         if (Physics.Raycast(ray, out hit, interactionRange))
//         {
//             if (hit.collider.CompareTag("Interactable"))
//             {
//                 currentTarget = hit.collider.gameObject;
//                 return;
//             }
//         }

        

//         currentTarget = null;

//         Debug.DrawRay(transform.position, transform.forward * interactionRange, Color.red);

// if (Input.GetKeyDown(KeyCode.E))
// {
//     Debug.Log("E pressed");
//     }

// }
// }
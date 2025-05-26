using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class CutsceneTrigger : MonoBehaviour



{
    public GameObject[] objectsToActivate;
    public Transform fallingObject;
    public float fallSpeed = 5f;
    public float stopYPosition = 0f;

    public Transform lookTarget;
    public Camera playerCamera;
    public float lookDuration = 2f;
    public float lookSpeed = 5f;

    public float shakeDuration = 0.3f;
    public float shakeMagnitude = 0.2f;

    [Header("Optional: Smoothly Lower This Object")]
    public Transform smoothFallingObject;
    public float smoothFallSpeed = 2f;
    public float smoothStopY = 0f;

    [Header("Player Control Settings")]
    public string playerTag = "Player";
    public string movementScriptName = "PlayerMovement";

    [Header("Scene Change")]
    public string sceneToLoad;
    public float delayBeforeSceneChange = 2f;

    private bool triggered = false;
    private bool isFalling = false;

    private Vector3 originalCamPos;
    private MonoBehaviour movementScript;

    void OnTriggerEnter(Collider other)
    {
        if (triggered || !other.transform.root.CompareTag(playerTag)) return;

        triggered = true;

        GameObject player = other.transform.root.gameObject;

        
        movementScript = player.GetComponent(movementScriptName) as MonoBehaviour;
        if (movementScript != null)
            movementScript.enabled = false;

        foreach (var obj in objectsToActivate)
            obj.SetActive(true);

        isFalling = true;

        StartCoroutine(ForceCameraLook());
        StartCoroutine(ShakeCamera());
        StartCoroutine(SequenceAndLoadScene());
    }

    void Update()
    {
        
        if (isFalling && fallingObject != null)
        {
            Vector3 currentPosition = fallingObject.position;
            if (currentPosition.y > stopYPosition)
            {
                float step = fallSpeed * Time.deltaTime;
                currentPosition.y -= step;
                currentPosition.y = Mathf.Max(currentPosition.y, stopYPosition);
                fallingObject.position = currentPosition;
            }
            else
            {
                isFalling = false;
            }
        }

        
        if (smoothFallingObject != null && smoothFallingObject.position.y > smoothStopY)
        {
            Vector3 pos = smoothFallingObject.position;
            pos.y -= smoothFallSpeed * Time.deltaTime;
            pos.y = Mathf.Max(pos.y, smoothStopY);
            smoothFallingObject.position = pos;
        }
    }

    IEnumerator ForceCameraLook()
    {
        float elapsed = 0f;
        Quaternion startRotation = playerCamera.transform.rotation;
        Vector3 direction = lookTarget.position - playerCamera.transform.position;
        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized);

        while (elapsed < lookDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / lookDuration);
            playerCamera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
            yield return null;
        }

        playerCamera.transform.rotation = targetRotation;
    }


    IEnumerator ShakeCamera()
    {
        originalCamPos = playerCamera.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float y = Random.Range(-1f, 1f) * shakeMagnitude;
            playerCamera.transform.localPosition = originalCamPos + new Vector3(x, y, 0f);
            yield return null;
        }

        playerCamera.transform.localPosition = originalCamPos;
    }

    IEnumerator SequenceAndLoadScene()
    {
        
        yield return new WaitForSeconds(lookDuration + delayBeforeSceneChange);

        
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene("MainLevel");
        }
    }
}
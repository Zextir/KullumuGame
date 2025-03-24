using UnityEngine;

public class TestMovement : MonoBehaviour
{
    [SerializeField] float movementSpeed = 5f;

    Rigidbody player;

    Vector3 moveAmount = Vector3.zero;

    private void Start()
    {
        player = GetComponent<Rigidbody>();
    }

    void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical"); 
        moveAmount = new Vector3(x,0,z).normalized * movementSpeed;
    }

    private void FixedUpdate()
    {
        Vector3 newPosition = player.position + moveAmount * Time.fixedDeltaTime;
        player.MovePosition(newPosition);
    }
}

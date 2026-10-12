using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour
{
    public float gravity = 30f;
    // Speed the cube gets in the new direction the instant gravity flips, replacing its old momentum.
    public float flipSpeed = 4f;
    public bool flipOnlyWhenGrounded = true;

    public event Action Died;
    public event Action<Collectible> Collected;

    Rigidbody body;
    float gravityDirection = -1f;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.useGravity = false;
        body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        body.constraints = RigidbodyConstraints.FreezePositionX
                         | RigidbodyConstraints.FreezePositionZ
                         | RigidbodyConstraints.FreezeRotation;
    }

    void Update()
    {
        if (FlipPressed())
        {
            gravityDirection = -gravityDirection;
            body.linearVelocity = Vector3.up * (gravityDirection * flipSpeed);
        }
    }

    void FixedUpdate()
    {
        body.AddForce(Vector3.up * (gravityDirection * gravity), ForceMode.Acceleration);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Obstacle _))
            Died?.Invoke();
        else if (other.TryGetComponent(out Collectible collectible))
            Collected?.Invoke(collectible);
    }

    public void ResetTo(Vector3 position)
    {
        gravityDirection = -1f;
        body.linearVelocity = Vector3.zero;
        body.position = position;
        transform.position = position;
    }

    // bool IsGrounded()
    // {
    //     float reach = transform.localScale.y / 2f + 0.05f;
    //     return Physics.Raycast(transform.position, Vector3.up * gravityDirection, reach,
    //         Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
    // }

    static bool FlipPressed()
    {
        return (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);
    }
}

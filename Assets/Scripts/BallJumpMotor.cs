using UnityEngine;

/// <summary>Aplica un impulso vertical sobre el Rigidbody de la pelota.</summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class BallJumpMotor : MonoBehaviour
{
    private Rigidbody rigidbodyComponent;
    private float jumpForce;

    public void Configure(float configuredJumpForce)
    {
        rigidbodyComponent ??= GetComponent<Rigidbody>();
        jumpForce = configuredJumpForce;
    }

    public void Jump()
    {
        rigidbodyComponent ??= GetComponent<Rigidbody>();
        rigidbodyComponent.isKinematic = false;

        Vector3 velocity = rigidbodyComponent.linearVelocity;
        velocity.y = 0f;
        rigidbodyComponent.linearVelocity = velocity;
        rigidbodyComponent.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }
}

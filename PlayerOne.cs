using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerOne : MonoBehaviour
{
     // Camera
    public  Transform  cameraTarget;
    private float     _mouseLookX;
    private float     _mouseLookY;

    // Physics
    private Vector3   _targetMovement;
    private Vector3   _movement;

    private Rigidbody _rigidbody;

     // Hoverbird
    [SerializeField] private Transform _character = default;

    private void Awake() {
        // Set physics timestep to 60hz
        Time.fixedDeltaTime = 1.0f/60.0f;

        // Store a reference to the rigidbody for easy access
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void Update() {
        // Move the camera using the mouse
        RotateCamera();

        // Use WASD input and the camera look direction to calculate the movement target
        CalculateTargetMovement();
    }

    private void FixedUpdate() {
        // Move the player based on the input
        MovePlayer();

        // Animate the character to match the player movement
        AnimateCharacter();
    }

    private void RotateCamera() {
        // Get the latest mouse movement. Multiple by 4.0 to increase sensitivity.
        _mouseLookX += Input.GetAxis("Mouse X") * 4.0f;
        _mouseLookY += Input.GetAxis("Mouse Y") * 4.0f;

        // Clamp how far you can look up + down
        while (_mouseLookY < -180.0f) _mouseLookY += 360.0f;
        while (_mouseLookY >  180.0f) _mouseLookY -= 360.0f;
        _mouseLookY = Mathf.Clamp(_mouseLookY, -15.0f, 15.0f);

        // Rotate camera
        cameraTarget.localRotation = Quaternion.Euler(-_mouseLookY, _mouseLookX, 0.0f);
    }

    private void CalculateTargetMovement() {
        // Get input movement. Multiple by 6.0 to increase speed.
        Vector3 inputMovement = new Vector3();
        inputMovement.x = Input.GetAxisRaw("Horizontal") * 6.0f;
        inputMovement.z = Input.GetAxisRaw("Vertical")   * 6.0f;

        // Get the direction the camera is looking parallel to the ground plane.
        Vector3    cameraLookForwardVector = ProjectVectorOntoGroundPlane(cameraTarget.forward);
        Quaternion cameraLookForward       = Quaternion.LookRotation(cameraLookForwardVector);

        // Use the camera look direction to convert the input movement from camera space to world space
        _targetMovement = cameraLookForward * inputMovement;
    }

    private void MovePlayer() {
        // Start with the current velocity
        Vector3 velocity = _rigidbody.velocity;

        // Smoothly animate towards the target movement velocity
        _movement = Vector3.Lerp(_movement, _targetMovement, Time.fixedDeltaTime * 5.0f);
        velocity.x = _movement.x;
        velocity.z = _movement.z;
        
        // Set the velocity on the rigidbody
        _rigidbody.velocity = velocity;
    }

     // Rotate the character to face the direction we're moving. Lean towards the target movement direction.
    private void AnimateCharacter() {
        // Calculate the direction that the character is facing parallel to the ground plane
        Vector3    characterLocalForwardVector = _character.localRotation * Vector3.forward;
        Vector3    characterLookForwardVector  = ProjectVectorOntoGroundPlane(characterLocalForwardVector);
        Quaternion characterLookForward        = Quaternion.LookRotation(characterLookForwardVector);

        // Calculate the angle between the current movement direction and the target movement direction
        Vector3 targetMovementNormalized = _targetMovement.normalized;
        Vector3       movementNormalized =       _movement.normalized;
        float angle = targetMovementNormalized.sqrMagnitude > 0.0f ? SignedAngle2D(targetMovementNormalized, movementNormalized) : 0.0f;

        // Convert the delta between movement direction and the target movement direction to a lean amount. Clamp to +/- 45 degrees so the player doesn't lean too far.
        angle = angle * Mathf.Rad2Deg;
        angle = Mathf.Clamp(angle, -45.0f, 45.0f);

        // Convert the lean angle to a Quaternion that's oriented in the direction the character is facing
        Quaternion leanRotation = characterLookForward * Quaternion.Euler(0.0f, 0.0f, angle);

        // Rotate to face the direction of travel if we're moving forward
        Vector3 targetCharacterLookForwardVector = characterLookForwardVector;
        if (GetRigidbodyForwardVelocity(_rigidbody) >= 2.0f)
            targetCharacterLookForwardVector = _rigidbody.velocity.normalized;

        // Compose the target character rotation from the target look direction + target lean direction
        Quaternion targetRotation = Quaternion.LookRotation(targetCharacterLookForwardVector, leanRotation * Vector3.up);

        // Animate the character towards the target rotation
        _character.localRotation = Quaternion.Slerp(_character.localRotation, targetRotation, 5.0f * Time.fixedDeltaTime);
    }

    // Given a forward vector, get a y-axis rotation that points in the same direction that's parallel to the ground plane
    private static Vector3 ProjectVectorOntoGroundPlane(Vector3 vector) {
        Vector3 planeNormal = Vector3.up;
        Vector3.OrthoNormalize(ref planeNormal, ref vector);
        return vector;
    }

     // Get the rigidbody velocity along the ground plane
    private static float GetRigidbodyForwardVelocity(Rigidbody rigidbody) {
        Vector3 forwardVelocity = rigidbody.velocity;
        forwardVelocity.y = 0.0f;
        return forwardVelocity.magnitude;
    }

    // Get the difference between two angles along the ground plane
    private static float SignedAngle2D(Vector3 a, Vector3 b) {
        float angle = Mathf.Atan2(a.z, a.x) - Mathf.Atan2(b.z, b.x);
        if (angle <= -Mathf.PI) angle += 2.0f * Mathf.PI;
        if (angle >   Mathf.PI) angle -= 2.0f * Mathf.PI;
        return angle;
    }
}
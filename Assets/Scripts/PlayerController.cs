using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private float maxGroundSpeed = 5.0f;
    [SerializeField] [Range(0,1)]
    private float groundSpeedGrowth = 0.1f;
    [SerializeField] [Range(0, 1)]
    private float groundSpeedFalloff = 0.5f;
    [SerializeField]
    private float maxAirSpeed = 10.0f;
    [SerializeField] [Range(0,180)]
    private float turnSpeed = 30.0f;
    [SerializeField] [Range(0, 180)]
    private float rotationDeadzone = 0.1f;

    private const float groundedSweepDist = 0.1f;

    private Rigidbody rb;

    private bool isGrounded = false;
    private bool isSwinging = false;

    private Vector2 directionInput;
    
    private Vector2 lerpAmount;   // used to slowly start moving

    // Start is called before the first frame update
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    private void Update()
    {
        directionInput = Vector2.zero;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    directionInput.y += 1;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  directionInput.y -= 1;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  directionInput.x -= 1;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) directionInput.x += 1;
    }

    private void FixedUpdate()
    {
        // perform a test to see if the player is grounded
        rb.position += Vector3.up * groundedSweepDist * 0.5f;
        RaycastHit hit;     // we dont use this but its necessary for the SweepTest
        isGrounded = rb.SweepTest(Vector3.down, out hit, groundedSweepDist);
        rb.position -= Vector3.up * groundedSweepDist * 0.5f;


        if (isGrounded && !isSwinging)
        {
            // move the spider so its facing up right
            rb.rotation = Quaternion.Euler(0, rb.rotation.eulerAngles.y, 0);
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
        else if(!isGrounded && isSwinging)
        {
            rb.constraints = RigidbodyConstraints.None;
        }

        Move();
    }

    private void Move()
    {
        // change lerp amount based on keyboard input
        if (directionInput.x != 0.0f) lerpAmount.x = Mathf.Clamp(lerpAmount.x + directionInput.x * groundSpeedGrowth, -1.0f, 1.0f);
        else if (lerpAmount.x >= groundSpeedFalloff || lerpAmount.x <= -groundSpeedFalloff) lerpAmount.x -= groundSpeedFalloff * Mathf.Sign(lerpAmount.x);
        else lerpAmount.x = 0.0f;
        // same for y
        if (directionInput.y != 0.0f) lerpAmount.y = Mathf.Clamp(lerpAmount.y + directionInput.y * groundSpeedGrowth, -1.0f, 1.0f);
        else if (lerpAmount.y >= groundSpeedFalloff || lerpAmount.y <= -groundSpeedFalloff) lerpAmount.y -= groundSpeedFalloff * Mathf.Sign(lerpAmount.y);
        else lerpAmount.y = 0.0f;
        
        Vector2 cameraRelativeInput = GetRelativeCameraForward() * lerpAmount.y + GetRelativeCameraRight() * lerpAmount.x;

        Vector2 targetVelocityChange = isGrounded && !isSwinging ? maxGroundSpeed * cameraRelativeInput : maxAirSpeed * cameraRelativeInput;
        Vector2 velocity2D = new Vector2(rb.velocity.x, rb.velocity.z);

        ForceMode forceMode = ForceMode.VelocityChange;
        if (!isGrounded || isSwinging) forceMode = ForceMode.Force;

        // applies the target velocity change up until it hits the maximum
        if (targetVelocityChange.x > 0) rb.AddForce(Mathf.Clamp(targetVelocityChange.x - velocity2D.x, 0.0f, targetVelocityChange.x), 0.0f, 0.0f, forceMode);
        else if (targetVelocityChange.x < 0) rb.AddForce(Mathf.Clamp(targetVelocityChange.x - velocity2D.x, targetVelocityChange.x, 0.0f), 0.0f, 0.0f, forceMode);
        // same for y
        if (targetVelocityChange.y > 0) rb.AddForce(0.0f, 0.0f, Mathf.Clamp(targetVelocityChange.y - velocity2D.y, 0.0f, targetVelocityChange.y), forceMode);
        else if (targetVelocityChange.y < 0) rb.AddForce(0.0f, 0.0f, Mathf.Clamp(targetVelocityChange.y - velocity2D.y, targetVelocityChange.y, 0.0f), forceMode);


        // turn the player
        if (isGrounded && !isSwinging)
        {
            Vector2 heading = new Vector2(rb.velocity.x, rb.velocity.z);
            Vector2 currentForward = new Vector2(transform.forward.x, transform.forward.z);
            if (heading.sqrMagnitude <= rotationDeadzone * rotationDeadzone) heading = currentForward;
            float angle = -Vector2.SignedAngle(currentForward, heading);

            if (angle <= -turnSpeed || angle >= turnSpeed) transform.Rotate(Vector3.up * turnSpeed * Mathf.Sign(angle));
            else transform.Rotate(Vector3.up * angle);
        }
    }

    private Vector2 GetRelativeCameraForward()
    {
        Vector3 cameraForward = UnityEngine.Camera.main.transform.forward;
        return new Vector2(cameraForward.x, cameraForward.z).normalized;
    }

    private Vector2 GetRelativeCameraRight()
    {
        Vector3 cameraRight = UnityEngine.Camera.main.transform.right;
        return new Vector2(cameraRight.x, cameraRight.z).normalized;
    }
}

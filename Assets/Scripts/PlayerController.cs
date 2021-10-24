using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] [Range(0,20)]
    private float maxGroundSpeed = 8.0f;
    [SerializeField] [Range(0,20)]
    private float frictionSpeed = 20.0f;
    [SerializeField] [Range(0,5)]
    private float groundSpeedGrowth = 3.0f;
    [SerializeField] [Range(0, 5)]
    private float groundSpeedFalloff = 5.0f;
    [SerializeField] [Range(0, 1000)]
    private float airForce = 500.0f;
    [SerializeField] [Range(0,360)]
    private float turnSpeed = 360.0f;
    [SerializeField] [Range(0, 5)]
    private float rotationDeadzone = 0.1f;
    [SerializeField] [Range(0, 20)]
    private float jumpSpeed = 7;

    private const float groundedSweepDist = 0.1f;

    private Rigidbody rb;

    private bool isGrounded = false;
    private bool isSwinging = false;

    private Vector2 directionInput;
    private bool jump;

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
        if (Input.GetKeyDown(KeyCode.Space))                             jump = true;
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

        lerpAmount.x = lerpInput(isGrounded ? directionInput.x : 0, lerpAmount.x);
        lerpAmount.y = lerpInput(isGrounded ? directionInput.y : 0, lerpAmount.y);

        if (isGrounded)
        {
            Move();
            Friction();
        }
        if (!isGrounded) AirControl();
        if (!isSwinging) Turn();
        if (jump) Jump();
    }

    private void Move()
    {
        Vector2 targetVelocityChange = maxGroundSpeed * lerpAmount;

        Vector2 _ = GetRelativeCameraRight() * targetVelocityChange.x;
        Vector3 relativeX = new Vector3(_.x, 0.0f, _.y);

        _ = GetRelativeCameraForward() * targetVelocityChange.y;
        Vector3 relativeY = new Vector3(_.x, 0.0f, _.y);

        Vector3 flatRBVelocity = new Vector3(rb.velocity.x, 0.0f, rb.velocity.z);

        if (Mathf.Sign(directionInput.x) == Mathf.Sign(lerpAmount.x) && directionInput.x != 0)
        {
            Vector3 velocityDif = Vector3.ClampMagnitude(relativeX - Vector3.Project(flatRBVelocity, relativeX), maxGroundSpeed);
            if (Vector3.Dot(velocityDif, relativeX) > 0) rb.AddForce(velocityDif, ForceMode.VelocityChange);
        }
        if (Mathf.Sign(directionInput.y) == Mathf.Sign(lerpAmount.y) && directionInput.y != 0)
        {
            Vector3 velocityDif = Vector3.ClampMagnitude(relativeY - Vector3.Project(flatRBVelocity, relativeY), maxGroundSpeed);
            if (Vector3.Dot(velocityDif, relativeY) > 0) rb.AddForce(velocityDif, ForceMode.VelocityChange);
        }
    }

    private void Friction()
    {
        float friction = frictionSpeed * Time.fixedDeltaTime;

        Vector2 cameraForward = GetRelativeCameraForward();
        Vector2 cameraRight = GetRelativeCameraRight();

        Vector2 flatRBVelocity = new Vector2(rb.velocity.x, rb.velocity.z);
        Vector2 localRBVelocity = new Vector2(Vector2.Dot(flatRBVelocity, cameraRight.normalized), Vector2.Dot(flatRBVelocity, cameraForward.normalized));
        Vector2 targetVelocityChange = new Vector2(Mathf.Clamp(-localRBVelocity.x, -friction, friction), Mathf.Clamp(-localRBVelocity.y, -friction, friction));

        if (Mathf.Sign(directionInput.x) != Mathf.Sign(lerpAmount.x) || directionInput.x == 0)  // apply friction when youre not hitting buttons
        {
            Vector2 _ = targetVelocityChange.x * cameraRight;
            rb.AddForce(_.x, 0.0f, _.y, ForceMode.VelocityChange);
            if (Mathf.Abs(localRBVelocity.x) <= Mathf.Abs(-targetVelocityChange.x)) lerpAmount.x = 0.0f;
        }
        else if(Mathf.Abs(localRBVelocity.x) > maxGroundSpeed)  // apply friction to keep us below or at ground speed
        {
            Vector2 _ = Vector2.ClampMagnitude(-localRBVelocity.x * cameraRight, Mathf.Min(Mathf.Abs(localRBVelocity.x) - maxGroundSpeed, friction));
            rb.AddForce(_.x, 0.0f, _.y, ForceMode.VelocityChange);
        }
        if (Mathf.Sign(directionInput.y) != Mathf.Sign(lerpAmount.y) || directionInput.y == 0)
        {
            Vector2 _ = targetVelocityChange.y * cameraForward;
            rb.AddForce(_.x, 0.0f, _.y, ForceMode.VelocityChange);
            if (Mathf.Abs(localRBVelocity.y) <= Mathf.Abs(-targetVelocityChange.y)) lerpAmount.y = 0.0f;
        }
        else if (Mathf.Abs(localRBVelocity.y) > maxGroundSpeed)
        {
            Vector2 _ = Vector2.ClampMagnitude(-localRBVelocity.y * cameraForward, Mathf.Min(Mathf.Abs(localRBVelocity.y) - maxGroundSpeed, friction));
            rb.AddForce(_.x, 0.0f, _.y, ForceMode.VelocityChange);
        }


    }

    private void AirControl()
    {
        Vector2 force = airForce * Time.fixedDeltaTime * (GetRelativeCameraForward() * directionInput.y + GetRelativeCameraRight() * directionInput.x).normalized;
        rb.AddForce(force.x, 0.0f, force.y);
    }

    private void Turn()
    {
        Vector2 heading = new Vector2(rb.velocity.x, rb.velocity.z);
        if (heading.sqrMagnitude > rotationDeadzone * rotationDeadzone)
        {
            Vector2 currentForward = new Vector2(transform.forward.x, transform.forward.z);
            float angle = -Vector2.SignedAngle(currentForward, heading);

            if (angle <= -turnSpeed * Time.fixedDeltaTime || angle >= turnSpeed * Time.fixedDeltaTime) transform.Rotate(Vector3.up * turnSpeed * Time.fixedDeltaTime * Mathf.Sign(angle));
            else transform.Rotate(Vector3.up * angle);
        }
    }

    private void Jump()
    {
        jump = false;
        if (isGrounded) rb.AddForce(0.0f, -rb.velocity.y + jumpSpeed, 0.0f, ForceMode.VelocityChange);
    }

    private float lerpInput(float direction, float currentAmount)
    {
        if (direction != 0.0f) currentAmount = Mathf.Clamp(currentAmount + direction * groundSpeedGrowth * Time.fixedDeltaTime, -1.0f, 1.0f);
        else if (currentAmount >= groundSpeedFalloff * Time.fixedDeltaTime || currentAmount <= -groundSpeedFalloff * Time.fixedDeltaTime) currentAmount -= groundSpeedFalloff * Time.fixedDeltaTime * Mathf.Sign(currentAmount);
        else currentAmount = 0.0f;

        return currentAmount;
    }

    private Vector2 GetRelativeCameraForward()
    {
        Vector3 cameraForward = UnityEngine.Camera.main.transform.forward;
        if (cameraForward.x <= 0.1f && cameraForward.x >= -0.1f && cameraForward.z <= 0.1f && cameraForward.z >= -0.1f) // if the camera is pointing roughly up or down
        {
            if(cameraForward.y > 0) cameraForward = -UnityEngine.Camera.main.transform.up;
            else if(cameraForward.y < 0) cameraForward = UnityEngine.Camera.main.transform.up;
        }
        return new Vector2(cameraForward.x, cameraForward.z).normalized;
    }

    private Vector2 GetRelativeCameraRight()
    {
        Vector3 cameraRight = UnityEngine.Camera.main.transform.right;
        return new Vector2(cameraRight.x, cameraRight.z).normalized;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatthewCamera : MonoBehaviour
{
    [SerializeField] [Range(0,20)]
    private float radius = 10;
    [SerializeField]
    private Vector2 sensitivity;
    private float theta = 0;
    private float omega = 60;

    private Transform player;

    private Vector2 initialMouse;

    // Start is called before the first frame update
    void Start()
    {
        initialMouse.x = Input.GetAxis("Mouse X");
        initialMouse.y = Input.GetAxis("Mouse Y");

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        player = GameObject.FindGameObjectWithTag("Player").transform;

        theta = player.rotation.y - 90;

        transform.position = GetTargetPosition();
    }

    // Update is called once per frame
    void Update()
    {
        theta -= sensitivity.x * (Input.GetAxis("Mouse X") - initialMouse.x);
        omega += sensitivity.y * (Input.GetAxis("Mouse Y") - initialMouse.y);

        theta = theta % 360f;
        omega = Mathf.Clamp(omega, 0.5f, 170f);

        // push in the camera
        transform.position = GetTargetPosition();

        transform.LookAt(player);
    }

    private Vector3 GetTargetPosition()
    {
        float radiansTheta = theta * Mathf.Deg2Rad;
        float radiansOmega = omega * Mathf.Deg2Rad;
        Vector3 positionOffset = new Vector3(radius * Mathf.Cos(radiansTheta) * Mathf.Sin(radiansOmega), radius * Mathf.Cos(radiansOmega), radius * Mathf.Sin(radiansTheta) * Mathf.Sin(radiansOmega));

        // raycast to the camera and see if we hit anything
        RaycastHit[] hits = Physics.RaycastAll(player.position, positionOffset, radius);
        Vector3 nearestHit = player.position + positionOffset * radius;
        float nearestDist = float.PositiveInfinity;
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].distance < nearestDist)
            {
                nearestDist = hits[i].distance;
                nearestHit = hits[i].point;
            }
        }

        // push in the camera
        if (!float.IsInfinity(nearestDist)) return nearestHit;
        else return player.position + positionOffset;
    }
}

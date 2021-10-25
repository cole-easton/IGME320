using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MatthewCamera : MonoBehaviour
{
    [SerializeField] [Range(0,20)]
    private float radius = 10;
    [SerializeField]
    private Vector2 lookSpeed;
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
    }

    // Update is called once per frame
    void Update()
    {
        theta -= Time.deltaTime * lookSpeed.x * (Input.GetAxis("Mouse X") - initialMouse.x);
        omega += Time.deltaTime * lookSpeed.y * (Input.GetAxis("Mouse Y") - initialMouse.y);

        theta = theta % 360f;
        omega = Mathf.Clamp(omega, 0.5f, 170f);


        float radiansTheta = theta * Mathf.Deg2Rad;
        float radiansOmega = omega * Mathf.Deg2Rad;
        Vector3 positionOffset = new Vector3(radius * Mathf.Cos(radiansTheta) * Mathf.Sin(radiansOmega), radius * Mathf.Cos(radiansOmega), radius * Mathf.Sin(radiansTheta) * Mathf.Sin(radiansOmega));

        transform.position = player.position + positionOffset;
        transform.LookAt(player);

        if (transform.position.y < player.position.y) transform.position += new Vector3(0, player.position.y - transform.position.y, 0);
    }
}

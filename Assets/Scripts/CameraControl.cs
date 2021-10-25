using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Utility;

public class CameraControl : MonoBehaviour
{
    public GameObject target;
    public float adjustmentSpeed;
    public float angleAdjustmentSpeed;
    private Vector3 initialPos;
    private Vector3 initialTargetPos;
    private Vector3 cameraRotation;
    private float verticalRadius = 7.5f;
    private float horizontalRadius = 7.5f;
    private float mouseXInitialValue;
    private float mouseYInitialValue;
    private int horizontalAngle = 270;
    private int verticalAngle = 20;
    private int verticalAimingAngle = 10;
    private bool isAiming = false;
    void Start()
    {
        initialPos = new Vector3(horizontalRadius * Mathf.Cos(horizontalAngle * Mathf.PI / 180), 10, horizontalRadius * Mathf.Sin(horizontalAngle * Mathf.PI / 180));
        //gameObject.transform.position = initialPos;
        initialTargetPos = target.transform.position;
        mouseXInitialValue = Input.GetAxis("Mouse X");
        mouseYInitialValue = Input.GetAxis("Mouse Y");
        //transform.rotation = Quaternion.Euler(20, horizontalAngle - 270, 0);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Vector3 goalPos = initialPos + target.transform.position;
        gameObject.transform.position = goalPos;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        horizontalAngle -= (int)(angleAdjustmentSpeed * (Input.GetAxis("Mouse X") - mouseXInitialValue));
        if (!isAiming)
        {
            verticalAngle -= (int)(angleAdjustmentSpeed * (Input.GetAxis("Mouse Y") - mouseYInitialValue));
            if (verticalAngle < -90)
                verticalAngle = -90;
            if (verticalAngle > 90)
                verticalAngle = 90;
        }
        else if (isAiming)
        {
            verticalAimingAngle += (int)(angleAdjustmentSpeed * (Input.GetAxis("Mouse Y") - mouseYInitialValue));
            if (verticalAimingAngle < 0)
                verticalAimingAngle = 0;
            if (verticalAimingAngle > 90)
                verticalAimingAngle = 90;
        }

        /*
        if (Input.GetKey(KeyCode.H))
        {
            horizontalAngle--;
        }
        if (Input.GetKey(KeyCode.F))
        {
            horizontalAngle++;
        }

        if (Input.GetKey(KeyCode.T))
        {
            if (!isAiming && verticalAngle <= 90)
                verticalAngle++;
            else if (isAiming && verticalAimingAngle <= 90)
                verticalAimingAngle++;
        }
        if (Input.GetKey(KeyCode.G))
        {
            if (!isAiming && verticalAngle >= 0)
                verticalAngle--;
            else if (isAiming && verticalAimingAngle >= 0)
                verticalAimingAngle--;
        }
        */

        if (Input.GetKeyDown(KeyCode.Y))
        {
            isAiming = isAiming ? false : true;            
        }

        if (!isAiming)
        {
            transform.rotation = Quaternion.Euler(verticalAngle, -(horizontalAngle - 270), 0);
            if (verticalAngle > 0)
            {
                horizontalRadius = verticalRadius * Mathf.Cos(verticalAngle * Mathf.PI / 180);
                initialPos = new Vector3(horizontalRadius * Mathf.Cos(horizontalAngle * Mathf.PI / 180),
                    verticalRadius * Mathf.Sin(verticalAngle * Mathf.PI / 180) + 7.5f,
                    horizontalRadius * Mathf.Sin(horizontalAngle * Mathf.PI / 180));
            }
            
            else
            {
                initialPos = new Vector3(horizontalRadius * Mathf.Cos(horizontalAngle * Mathf.PI / 180),
                    verticalRadius * Mathf.Sin(0 * Mathf.PI / 180) + 7.5f,
                    horizontalRadius * Mathf.Sin(horizontalAngle * Mathf.PI / 180));
            }
            
            Vector3 goalPos = initialPos + target.transform.position - initialTargetPos;
            transform.position += (goalPos - transform.position) * adjustmentSpeed * Time.deltaTime;
        }
        else
        {
            transform.rotation = Quaternion.Euler(-verticalAimingAngle, -(horizontalAngle - 270), 0);
            horizontalRadius = verticalRadius * Mathf.Cos(verticalAngle * Mathf.PI / 180);
            initialPos = new Vector3(-horizontalRadius / 7.5f * Mathf.Cos(horizontalAngle * Mathf.PI / 180),
                verticalRadius / 7.5f * Mathf.Sin(verticalAimingAngle * Mathf.PI / 180) + 7.5f,
                -horizontalRadius / 7.5f * Mathf.Sin(horizontalAngle * Mathf.PI / 180));
            Vector3 goalPos = initialPos + target.transform.position - initialTargetPos;
            transform.position += (goalPos - transform.position) * adjustmentSpeed * Time.deltaTime;
        }
    }
}

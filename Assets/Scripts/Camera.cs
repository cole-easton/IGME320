using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera : MonoBehaviour
{
	public GameObject target;
	public float adjustmentSpeed = 10;
	private Vector3 initialPos;
	private Vector3 initialTargetPos;
    void Start()
    {
		initialPos = gameObject.transform.position;
		initialTargetPos = target.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
		Vector3 goalPos = initialPos + target.transform.position - initialTargetPos;
		transform.position += (goalPos - transform.position) * adjustmentSpeed * Time.deltaTime;
    }
}

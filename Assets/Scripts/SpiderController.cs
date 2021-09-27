using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpiderController : MonoBehaviour
{
	private Vector3 position;
	private Vector3 velocity;
	private bool onSurface;
	private bool swinging;
	private float swingingDist;
	private GameObject closestNode;
	private float theta;
	private float omega; //angular velocity
	private GameObject web;
	private LineRenderer webRenderer;
	public float acceleration = -4f;
	public float jumpVelocity = 4;
	public float movementSpeed = 4f;
	public GameObject[] floorPieces;
	public GameObject[] swingNodes;
    void Start()
    {
		position = gameObject.transform.position;
		velocity = Vector3.zero;
		onSurface = false;
		web = new GameObject();
		webRenderer = web.AddComponent<LineRenderer>();
		webRenderer.material = gameObject.GetComponent<SpriteRenderer>().material;
		webRenderer.endColor = Color.yellow;
		webRenderer.startColor = Color.yellow;
		webRenderer.startWidth = 0.2f;
		webRenderer.endWidth = 0.1f;
	}

    // Update is called once per frame
    void Update()
    {
		if (onSurface && Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
		{
			velocity.x = -movementSpeed;
		}
		else if (onSurface && Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
		{
			velocity.x = movementSpeed;
		}
		else if (onSurface)
		{
			velocity.x = 0;
		}

		if (!onSurface && !swinging && Input.GetKeyDown(KeyCode.Space)) //when you git space to first attach to swing node
		{
			closestNode = swingNodes[0];
			for (int i = 0; i < swingNodes.Length; i++)
			{
				if ((position - swingNodes[i].transform.position).sqrMagnitude < (position - closestNode.transform.position).sqrMagnitude)
				{
					closestNode = swingNodes[i];
				}
			}
			swinging = true;
			theta = Mathf.Atan2(position.x - closestNode.transform.position.x, closestNode.transform.position.y - position.y);
			swingingDist = (position - closestNode.transform.position).magnitude;
			omega = Vector3.Dot(velocity, new Vector3(Mathf.Cos(theta), Mathf.Sin(theta))) / swingingDist;

			web.SetActive(true);
			web.transform.position = transform.position;
			webRenderer.SetPosition(0, transform.position);
			webRenderer.SetPosition(1, closestNode.transform.position);
		}
		if (onSurface && Input.GetKeyDown(KeyCode.Space)) // when you're on a surface and press space
		{
			velocity.y = jumpVelocity;
			onSurface = false;
		}
		
		if (swinging && Input.GetKey(KeyCode.Space)) //holding space while swinging 
		{
			web.transform.position = transform.position;
			web.SetActive(true);
			webRenderer.SetPosition(0, transform.position);
			webRenderer.SetPosition(1, closestNode.transform.position);
			//DrawLine(closestNode.transform.position, transform.position, Color.blue);
			position = closestNode.transform.position + swingingDist * new Vector3(Mathf.Sin(theta), Mathf.Cos(theta), 0);
			omega += (acceleration / swingingDist) * Mathf.Sin(theta) * Time.deltaTime;
			theta += omega*Time.deltaTime;
			position = closestNode.transform.position - swingingDist * new Vector3(-Mathf.Sin(theta), Mathf.Cos(theta));
		}
		else if (swinging) // when you let go of space while swinging 
		{
			web.SetActive(false);
			swinging = false;
			velocity.x = omega * swingingDist * Mathf.Cos(theta);
			velocity.y = omega * swingingDist * Mathf.Sin(theta);
		}

		if (!swinging)
		{
			float deltaTime = Time.deltaTime;
			velocity.y += acceleration * deltaTime;
			position += velocity * deltaTime;
		}
		

		foreach(GameObject floorPiece in floorPieces)
		{
			Vector3 floorPosition = floorPiece.transform.position;
			if (position.x > floorPosition.x - floorPiece.transform.localScale.x/2 
				&& position.x < floorPosition.x + floorPiece.transform.localScale.x / 2
				&& position.y + transform.localScale.y/2 > floorPosition.y - floorPiece.transform.localScale.y/2
				&& position.y - transform.localScale.y / 2 < floorPosition.y + floorPiece.transform.localScale.y / 2)
			{
				position.y = floorPosition.y + floorPiece.transform.localScale.y / 2 + transform.localScale.y/2;
				velocity = Vector3.zero;
				onSurface = true;
			}
		}
		gameObject.transform.position = position;
	}
}

﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Web : MonoBehaviour
{
    private const float segmentLength = 0.1f;
    private const int contraintIterations = 50;

    //------------------------------------------members-----------------------------------//

    [SerializeField]
    private bool simulateOnStart;

    [Space]

    [SerializeField]
    private RopeEnd head;
    [SerializeField]
    private RopeEnd tail;


    private LineRenderer lr;

    private float length;   // the combined length of all segments in the rope

    private bool isAlive;

    private List<RopePoint> points;     // in order and 0 is the head
    private List<RopeSegment> segments; // in order and 0 is the head segment

    //----------------------------------------properties----------------------------------//

    public float Length { get { return length; } set { SetLength(value); } }
    public Vector3 HeadPosition { get { if (!isAlive) return Vector3.zero;  return head.point.position; } set { if (isAlive) head.point.position = value; } }
    public Vector3 TailPosition { get { if (!isAlive) return Vector3.zero; return tail.point.position; } set { if (isAlive) tail.point.position = value; } }
    public Vector3 HeadDirection { get { if (!isAlive) return Vector3.zero; return (head.segment.A.position - head.segment.B.position).normalized; } }
    public Vector3 TailDirection { get { if (!isAlive) return Vector3.zero; return (tail.segment.B.position - tail.segment.A.position).normalized; } }
    public Vector3 HeadVelocity { get { if (!isAlive) return Vector3.zero; return head.point.position - head.point.oldPosition; } set { if (isAlive) head.point.oldPosition = head.point.position - value; } }
    public Vector3 TailVelocity { get { if (!isAlive) return Vector3.zero; return tail.point.position - tail.point.oldPosition; } set { if (isAlive) tail.point.oldPosition = tail.point.position - value; } }
    public bool IsHeadLocked { get { if (!isAlive) return false; return head.isLocked; } set { if (isAlive) head.isLocked = value; } }
    public bool IsTailLocked { get { if (!isAlive) return false; return tail.isLocked; } set { if (isAlive) tail.isLocked = value; } }
    public bool IsHeadAttached { get { if (!isAlive) return false; return head.attachment != null; } }
    public bool IsTailAttached { get { if (!isAlive) return false; return tail.attachment != null; } }
    public Vector3 HeadAttachmentPoint { get { if (!isAlive) return Vector3.zero; return head.attachmentPoint; } set { if (isAlive) head.attachmentPoint = value; } }
    public Vector3 TailAttachmentPoint { get { if (!isAlive) return Vector3.zero; return tail.attachmentPoint; } set { if (isAlive) tail.attachmentPoint = value; } }

    //----------------------------------private methods-------------------------------------------//

    private void Start()
    {
        lr = GetComponent<LineRenderer>();
        lr.useWorldSpace = true;

        points = new List<RopePoint>();
        segments = new List<RopeSegment>();



        bool headLocked = head.isLocked;
        GameObject headAttachment = head.attachment;
        Vector3 headAttach = head.attachmentPoint;

        bool tailLocked = tail.isLocked;
        GameObject tailAttachment = tail.attachment;
        Vector3 tailAttach = tail.attachmentPoint;


        CreateRope(head.point.position, tail.point.position);


        IsHeadLocked = headLocked;
        IsTailLocked = tailLocked;

        if (headAttachment)
            AttachHead(headAttachment, headAttach);

        if (tailAttachment)
            AttachTail(tailAttachment, tailAttach);
    }

    private void Update()
    {
        Draw();
    }

    private void FixedUpdate()
    {
        Simulate();
        Constraint();
        MoveAttachments();
    }

    private void Draw()
    {
        lr.useWorldSpace = true;

        Vector3[] positions = new Vector3[points.Count];

        for (int i = 0; i < points.Count; i++)
        {
            positions[i] = points[i].position;
        }

        lr.positionCount = positions.Length;
        lr.SetPositions(positions);
    }

    private void Simulate()
    {
        for (int i = 0; i < points.Count; i++)
        {
            RopePoint point = points[i];

            // updaate the old positon to be the new positon
            Vector3 oldPos = point.oldPosition;
            point.oldPosition = point.position;

            // if the point is an end point thats isnt supposed to move by physics then don't move it
            if ((point == head.point && head.IsStatic) || (point == tail.point && tail.IsStatic))
                continue;

            // velocity is assumed to be the same as the velocity in the previous frame
            Vector3 velocity = point.position - oldPos;

            point.position += velocity;
            point.position += Physics.gravity * Time.fixedDeltaTime * Time.fixedDeltaTime;
        }
    }

    private void Constraint()
    {
        // define a buffer
        List<RopeSegment> buffer = new List<RopeSegment>();

        for (int k = 0; k < contraintIterations; k++)
        {
            // put all the points in a buffer list that we will pull from randomly
            // this helps remove jitter
            buffer.Clear();
            buffer.AddRange(segments);
            
            // if the head or tail has an attached object, move the point to the attached point on the object if the object is inside the length of the rope
            // otherwise it will be a point on the max length of the rope
            if (head.attachment)
            {
                Vector3 pos = head.attachment.transform.TransformPoint(head.attachmentPoint);   // the position of the attachment point in world space
                Vector3 dir = pos - tail.point.position;
                pos = tail.point.position + Vector3.ClampMagnitude(dir, length);
                head.point.position = pos;
            }
            if (tail.attachment)
            {
                Vector3 pos = tail.attachment.transform.TransformPoint(tail.attachmentPoint);   // the position of the attachment point in world space
                Vector3 dir = pos - head.point.position;
                pos = head.point.position + Vector3.ClampMagnitude(dir, length);
                tail.point.position = pos;
            }

            while (buffer.Count > 0)
            {
                int index = Random.Range(0, buffer.Count);
                RopeSegment segment = buffer[index];
                buffer.RemoveAt(index);

                Vector3 dir = (segment.A.position - segment.B.position).normalized;

                if (segment.A == head.point && head.IsStatic)
                {
                    segment.B.position = segment.A.position - (dir * segment.length);
                }
                else if (segment.B == tail.point && tail.IsStatic)
                {
                    segment.A.position = segment.B.position + (dir * segment.length);
                }
                else
                {
                    Vector3 midpoint = (segment.A.position + segment.B.position) / 2;
                    segment.A.position = midpoint + (dir * segment.length / 2);
                    segment.B.position = midpoint - (dir * segment.length / 2);
                }
            }
        }
    }

    private void MoveAttachments()
    {
        if (IsHeadAttached)
        {
            Rigidbody rb = head.attachment.GetComponent<Rigidbody>();
            if (rb != null && rb.isKinematic == false)
            {
                Vector3 attachPoint = head.attachment.transform.TransformPoint(head.attachmentPoint);   // the attach point in world space
                if ((attachPoint - tail.point.position).magnitude > length)
                {
                    // set the position of the attachment so that the attach point is where the end point it
                    head.attachment.transform.position = head.attachment.transform.position - (attachPoint - head.point.position);

                    Vector3 dir = HeadDirection.normalized;
                    Vector3 attachVelocity = rb.GetPointVelocity(attachPoint);
                    if (Vector3.Dot(attachVelocity, dir) > 0)
                    {
                        Vector3 forceVector = Vector3.Project(attachVelocity, dir);
                        Debug.Log(forceVector);
                        rb.AddForceAtPosition(forceVector, attachPoint, ForceMode.VelocityChange);
                    }
                }
            }
        }

        // UNFINISHED
        if (IsTailAttached)
        {
            Rigidbody rb = tail.attachment.GetComponent<Rigidbody>();
            if (rb != null && rb.isKinematic == false)
                tail.attachment.transform.position = tail.attachment.transform.position - (tail.attachment.transform.TransformPoint(tail.attachmentPoint) - tail.point.position);
        }
    }

    //---------------------------------public methods---------------------------------//

    // creates a brand new set of points going from the head to the tail in a straight line
    public void CreateRope(Vector3 headPosition, Vector3 tailPosition)
    {
        if (headPosition == tailPosition)
            return;

        // clear out the old points
        points.Clear();
        segments.Clear();

        // set the length value for the new rope
        length = (headPosition - tailPosition).magnitude;

        // place the points that are segment length away from each other starting at the head
        Vector3 dir = (tailPosition - headPosition).normalized;     // which direction to we place the new points in
        for (int i = 0; i < (int)(length / segmentLength) + 1; i++)
        {
            points.Add(new RopePoint(headPosition + dir * segmentLength * i, Vector3.zero));
        }

        // add on the last point which is the remainder away from the previous point
        float remainder = length % segmentLength;
        if (remainder != 0.0f)
            points.Add(new RopePoint(tailPosition, Vector3.zero));

        // connect all the points together into segments
        for (int i = 1; i < points.Count; i++)
        {
            segments.Add(new RopeSegment(points[i - 1], points[i], (points[i - 1].position - points[i].position).magnitude));
        }

        // finally assign the head and tail
        head = new RopeEnd(points[0], segments[0]);
        tail = new RopeEnd(points[points.Count - 1], segments[segments.Count - 1]);

        isAlive = true;
    }
    
    // not to be confused with Object.Destroy(). This method removes all the points from the current rope.
    public void DestroyRope()
    {
        points.Clear();
        segments.Clear();
        head = null;
        tail = null;
        length = 0.0f;

        isAlive = false;
    }

    public void AttachHead(GameObject obj)
    {
        AttachHead(obj, Vector3.zero);
    }
    public void AttachHead(GameObject obj, Vector3 attachmentPoint)
    {
        if (obj == null || !isAlive)
            return;

        head.attachment = obj;
        head.attachmentPoint = attachmentPoint;

        head.point.position = head.attachment.transform.TransformPoint(head.attachmentPoint);
    }
    public void DetachHead()
    {
        head.attachment = null;
        head.attachmentPoint = Vector3.zero;
    }

    public void AttachTail(GameObject obj)
    {
        AttachTail(obj, Vector3.zero);
    }
    public void AttachTail(GameObject obj, Vector3 attachmentPoint)
    {
        if (obj == null || !isAlive)
            return;

        tail.attachment = obj;
        tail.attachmentPoint = attachmentPoint;

        tail.point.position = tail.attachment.transform.TransformPoint(tail.attachmentPoint);
    }
    public void DetachTail()
    {
        tail.attachment = null;
        tail.attachmentPoint = Vector3.zero;
    }

    //---------------------------------getters---------------------------------//

    public float GetLength()
    {
        return length;
    }

    //---------------------------------setters---------------------------------//

    public void SetLength(float length)
    {

    }

    //-----------------------------private classes-----------------------------//

    [System.Serializable]
    private class RopeEnd
    {
        public RopePoint point;
        public RopeSegment segment;
        public bool isLocked;
        public GameObject attachment;
        public Vector3 attachmentPoint;

        public bool IsStatic {
            get
            {
                if (isLocked)
                    return true;
                if (attachment)
                {
                    Rigidbody rb = attachment.GetComponent<Rigidbody>();
                    if (rb == null || rb.isKinematic)
                        return true;
                }
                return false;
            }
        }

        public RopeEnd(RopePoint point, RopeSegment segment, GameObject attachment = null, bool isLocked = false)
            : this(point, segment, Vector3.zero, attachment, isLocked)
        {

        }
        public RopeEnd(RopePoint point, RopeSegment segment, Vector3 attachmentPoint, GameObject attachment = null, bool isLocked = false)
        {
            this.point = point;
            this.segment = segment;
            this.isLocked = isLocked;
            this.attachment = attachment;
            this.attachmentPoint = attachmentPoint;
        }
    }
    private class RopeSegment
    {
        public RopePoint A;
        public RopePoint B;
        public float length;

        public RopeSegment(RopePoint A, RopePoint B, float length = segmentLength)
        {
            this.A = A;
            this.B = B;
            this.length = length;
        }
    }

    [System.Serializable]
    private class RopePoint
    {
        public Vector3 position;
        [System.NonSerialized]
        public Vector3 oldPosition;

        public RopePoint()
            : this(Vector3.zero, Vector3.zero)
        {

        }

        public RopePoint(Vector3 position, Vector3 velocity)
        {
            this.position = position;
            this.oldPosition = position - velocity;
        }
    }
}
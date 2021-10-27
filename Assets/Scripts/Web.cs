using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

namespace Spider
{
    [RequireComponent(typeof(LineRenderer))]
    public class Web : MonoBehaviour
    {
        [SerializeField]
        private float segmentLength = 0.1f;
        private const int contraintIterations = 50;

        //------------------------------------------members-----------------------------------//

        [SerializeField]
        private bool simulateOnStart;   // should the rope pre-simulate when it is created?    NOT IMPLEMENTED
        [SerializeField] [Range(0, 0.1f)]
        private float dampening;        // how much the rope swinging is dampened

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
        public Vector3 HeadPosition { get { if (!isAlive) return Vector3.zero; return head.point.position; } set { if (isAlive) head.point.position = value; } }
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

            isAlive = false;

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
            if (isAlive)
            {
                Simulate();
                Constraint();
                MoveAttachments();
            }
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
                if ((point == head.point && head.isLocked) || (point == tail.point && tail.isLocked))
                    continue;

                if (point == head.point && head.attachment)
                    continue;
                if (point == tail.point && tail.attachment)
                    continue;


                // velocity is assumed to be the same as the velocity in the previous frame
                Vector3 velocity = point.position - oldPos;

                point.position += velocity * (1 - dampening);
                point.position += Physics.gravity * Time.fixedDeltaTime * Time.fixedDeltaTime;
            }

            // move the head and tail so they are where the attachments are, but also length distance apart
            Vector3 newHeadPos = head.point.position;
            Vector3 newTailPos = tail.point.position;
            if (head.attachment)
            {
                Vector3 pos = head.attachment.transform.TransformPoint(head.attachmentPoint);   // the position of the attachment point in world space
                Vector3 dif = pos - tail.point.position;                                        // the vector from tail to the attachment point
                newHeadPos = tail.point.position + Vector3.ClampMagnitude(dif, length);    // set the head to be towards the attachment point, but clamped to be within length
            }
            if (tail.attachment)
            {
                Vector3 pos = tail.attachment.transform.TransformPoint(tail.attachmentPoint);   // the position of the attachment point in world space
                Vector3 dif = pos - head.point.position;                                        // the vector from tail to the attachment point
                newTailPos = head.point.position + Vector3.ClampMagnitude(dif, length);    // set the head to be towards the attachment point, but clamped to be within length
            }
            head.point.position = newHeadPos;
            tail.point.position = newTailPos;
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

                while (buffer.Count > 0)
                {
                    int index = Random.Range(0, buffer.Count);
                    RopeSegment segment = buffer[index];
                    buffer.RemoveAt(index);

                    Vector3 dir = (segment.A.position - segment.B.position).normalized;     // the direction vector from the tail side point to the head side point

                    if (segment.A == head.point && (head.isLocked || head.attachment))
                    {
                        segment.B.position = segment.A.position - (dir * segment.length);   // move the tail side point so it is segment length away from the head side point
                    }
                    else if (segment.B == tail.point && (tail.isLocked || tail.attachment))
                    {
                        segment.A.position = segment.B.position + (dir * segment.length);   // move the head side point so it is segment length away from the tail side point
                    }
                    else
                    {
                        Vector3 midpoint = (segment.A.position + segment.B.position) / 2.0f;   // find the midpoint between the two points
                        segment.A.position = midpoint + (dir * segment.length / 2.0f);         // move both points so they are a half length from the midpoint
                        segment.B.position = midpoint - (dir * segment.length / 2.0f);         // two half lengths make a full length
                    }
                }
            }
        }

        private void MoveAttachments()
        {
            if (IsHeadAttached)
            {
                Rigidbody rb = head.attachment.GetComponent<Rigidbody>();
                if (rb != null && rb.isKinematic == false && (tail.isLocked || IsTailAttached))
                {
                    Vector3 attachPoint = head.attachment.transform.TransformPoint(head.attachmentPoint);   // the attach point in world space

                    // set the position of the attachment so that the attach point is where the end point is
                    head.attachment.transform.position = head.attachment.transform.position - (attachPoint - head.point.position);

                    // check if the attach point has moved outside the length of the rope
                    if ((attachPoint - tail.point.position).magnitude > length)
                    {
                        Vector3 attachVelocity = rb.GetPointVelocity(attachPoint);  // the velocity of the point on the rigidbody
                        if (Vector3.Dot(attachVelocity, attachPoint - tail.point.position) > 0)     // if the velocity vector is pointing outside the circle formed by the rope
                        {
                            // Add a velocity change inwards towards the rope circle so that the new velocity is tangential to the circle
                            rb.AddForceAtPosition(-Vector3.Project(attachVelocity, attachPoint - tail.point.position), attachPoint, ForceMode.VelocityChange);
                        }
                    }
                }
            }

            if (IsTailAttached)
            {
                Rigidbody rb = tail.attachment.GetComponent<Rigidbody>();
                if (rb != null && rb.isKinematic == false && (head.isLocked || IsHeadAttached))
                {
                    Vector3 attachPoint = tail.attachment.transform.TransformPoint(tail.attachmentPoint);   // the attach point in world space

                    // set the position of the attachment so that the attach point is where the end point is
                    tail.attachment.transform.position = tail.attachment.transform.position - (attachPoint - tail.point.position);

                    // check if the attach point has moved outside the length of the rope
                    if ((attachPoint - head.point.position).magnitude > length)
                    {
                        Vector3 attachVelocity = rb.GetPointVelocity(attachPoint);  // the velocity of the point on the rigidbody
                        if (Vector3.Dot(attachVelocity, attachPoint - head.point.position) > 0)     // if the velocity vector is pointing outside the circle formed by the rope
                        {
                            // Add a velocity change inwards towards the rope circle so that the new velocity is tangential to the circle
                            rb.AddForceAtPosition(-Vector3.Project(attachVelocity, attachPoint - head.point.position), attachPoint, ForceMode.VelocityChange);
                        }
                    }
                }
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

            public bool IsStatic
            {
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

            public RopeSegment(RopePoint A, RopePoint B, float length)
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
}

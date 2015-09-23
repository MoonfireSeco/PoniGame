﻿using UnityEngine;
using System.Collections;
using InControl;

/*
 * Responsible for moving the player character
 */
public class TSMovement : MonoBehaviour
{
    public bool debugView = true;

    public LayerMask walkable;

    [Tooltip("How fast the character may walk (Units / Second)")]
    [Range(0.5f, 2.0f)]
    public float walkSpeed = 1.0f;

    [Tooltip("How fast the character may run (Units / Second)")]
    [Range(1.0f, 4.0f)]
    public float runSpeed = 2.0f;

    [Tooltip("How fast the character accelerates forward (Units / Second^2)")]
    [Range(1.0f, 10.0f)]
    public float acceleration = 5.0f;

    [Tooltip("How fast the character may rotate (Degrees / Second)")]
    [Range(60, 720)]
    public float rotSpeed = 120.0f;

    [Tooltip("How fast the character begins moving on jumping (Units / Second)")]
    [Range(0.5f, 3.0f)]
    public float jumpSpeed = 1.0f;

    [Tooltip("Fraction of the world's gravity is applied when in the air")]
    [Range(0.25f, 2.5f)]
    public float gravityFraction = 1.0f;

    [Tooltip("The number of raycasts done in a circle around the character to get an average ground normal")]
    [Range(0, 21)]
    public int normalSamples = 4;

    [Tooltip("The radius of the raycast circle around the character to get an average ground normal (Units)")]
    [Range(0.0f, 1.0f)]
    public float groundSmoothRadius = 0.1f;

    [Tooltip("The higher this value, the faster the character will align to the ground normal when on terrain")]
    [Range(1.0f, 24.0f)]
    public float groundAlignSpeed = 10.0f;

    [Tooltip("The higher this value, the faster the character will align upwards when midair")]
    [Range(0.25f, 24.0f)]
    public float airAlignSpeed = 1.5f;


    private CapsuleCollider m_collider;
    private float m_forwardVelocity = 0;
    private float m_angVelocity = 0;
    private float m_velocityY = 0;
    private bool m_run = false;
    private bool m_pullingCart = false;

    public float ForwardSpeed
    {
        get { return m_forwardVelocity; }
    }


    void Start ()
    {
        m_collider = GetComponent<CapsuleCollider>();
    }


    /*
     * Is there a collider beneath us?
     */
    public bool IsGrounded()
    {
        RaycastHit hitInfo;
        return Physics.SphereCast(transform.TransformPoint(m_collider.center - Vector3.up * (m_collider.height / 2 - m_collider.radius)), m_collider.radius * 0.95f, -transform.up, out hitInfo, 0.01f, walkable);
    }

    /*
     * Executes the player's or AI's commands
     */
    void FixedUpdate ()
    {
        MoveInputs inputs = new MoveInputs();
        InputDevice device = InputManager.ActiveDevice;

        m_run = Input.GetKeyDown(KeyCode.C) || device.LeftStickButton.WasPressed ? !m_run : m_run;

        if (tag == "Player")
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                SetCart(!m_pullingCart);
            }

            Vector3 move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            move += new Vector3(device.LeftStick.X, 0, device.LeftStick.Y);
            move = Vector3.ClampMagnitude(move, 1);

            if (move.magnitude > 0)
            {
                inputs.turn = GetBearing(transform.forward, Camera.main.transform.rotation * move);
                inputs.forward = move.magnitude;
            }
            inputs.run = Input.GetKey(KeyCode.LeftShift) || device.RightTrigger.State ? !m_run : m_run;
            inputs.jump = (Input.GetKey(KeyCode.Space) && !device.Action4.State) || device.Action3.State;

            ExecuteMovement(inputs);
        }
        else
        {
            ExecuteMovement(GetComponent<TSAI>().GetMovement());
        }
    }

    /*
     * Gets the bearing in degrees between two vectors as viewed from a certain direction
     */
    public float GetBearing(Vector3 dir1, Vector3 dir2)
    {
        float vec1 = Quaternion.LookRotation(Vector3.ProjectOnPlane(dir1, Vector3.up), Vector3.up).eulerAngles.y;
        float vec2 = Quaternion.LookRotation(Vector3.ProjectOnPlane(dir2, Vector3.up), Vector3.up).eulerAngles.y;
        return -Mathf.DeltaAngle(vec2, vec1);
    }


    /*
     * Moves the character based on the provided input
     */
    private void ExecuteMovement(MoveInputs inputs)
    {
        // cancel invalid actions
        if (m_pullingCart)
        {
            inputs.run = false;
            inputs.jump = false;
        }
        
        // linearly accelerate towards some target velocity
        m_forwardVelocity = Mathf.MoveTowards(m_forwardVelocity, inputs.forward * (inputs.run ? runSpeed : walkSpeed), acceleration * Time.deltaTime);
        Vector3 moveVelocity = transform.forward * m_forwardVelocity;
        
        if (IsGrounded())
        {
            // align the character to the ground being stood on
            Vector3 normal = GetGroundNormal(normalSamples, groundSmoothRadius);
            Quaternion targetRot = Quaternion.LookRotation(Vector3.Cross(transform.right, normal), normal);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * groundAlignSpeed);

            if (inputs.jump)
            {
                // jumping
                GetComponent<Rigidbody>().velocity = new Vector3(GetComponent<Rigidbody>().velocity.x, jumpSpeed, GetComponent<Rigidbody>().velocity.z);
            }
            else
            {
                // keeps the character on the ground by applying a small downwards velocity that increases with the slope the character is standing on
                float slopeFactor = (1 - Mathf.Clamp01(Vector3.Dot(normal, Vector3.up)));
                //m_velocityY = (-0.5f * slopeFactor + (1 - slopeFactor) * -0.01f) / Time.deltaTime;
            }
        }
        else
        {
            // slowly orient the character up in the air
            Vector3 normal = Vector3.up;
            Quaternion targetRot = Quaternion.LookRotation(Vector3.Cross(transform.right, normal), normal);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * airAlignSpeed);

            // apply downwards acceleration when the character is in the air
            //m_velocityY += Physics.gravity.y * Time.deltaTime * gravityFraction;
        }

        Vector3 move = new Vector3(moveVelocity.x, GetComponent<Rigidbody>().velocity.y, moveVelocity.z);// * Time.deltaTime;
        GetComponent<Rigidbody>().velocity = move;
        //m_CollisionFlags = m_controller.Move(move);

        float targetAngVelocity = Mathf.Clamp(inputs.turn, -rotSpeed * Time.deltaTime, rotSpeed * Time.deltaTime);
        m_angVelocity = Mathf.MoveTowards(m_angVelocity, targetAngVelocity, 20.0f * Time.deltaTime) * Mathf.Clamp01((Mathf.Abs(inputs.turn) + 25.0f) / 45.0f);
        bool willOvershoot = Mathf.Abs(inputs.turn) < Mathf.Abs(m_angVelocity);
        m_angVelocity = willOvershoot ? targetAngVelocity : m_angVelocity;
        transform.Rotate(0, m_angVelocity, 0, Space.Self);
    }


    /*
     * Samples the ground beneath the character in a circle and returns the average normal of the ground at those points
     */
    private Vector3 GetGroundNormal(int samples, float radius)
    {
        Vector3 normal = Vector3.zero;

        for (int i = 0; i < samples; i++)
        {
            Vector3 offset = Quaternion.Euler(0, i * (360.0f / samples), 0) * Vector3.forward * radius;
            Vector3 SamplePos = transform.TransformPoint(offset + m_collider.center);
            Vector3 SampleDir = transform.TransformPoint(offset + Vector3.down * 0.05f);
           
            RaycastHit hit;
            if (Physics.Linecast(SamplePos, SampleDir, out hit) && hit.transform.gameObject.layer == LayerMask.NameToLayer("Ground") && Vector3.Dot(hit.normal, Vector3.up) > 0.75f)
            {
                normal += hit.normal;

                if (debugView)
                {
                    Debug.DrawLine(SamplePos, hit.point, Color.cyan);
                    Debug.DrawLine(hit.point, hit.point + hit.normal * 0.25f, Color.yellow);
                }
            }
        }

        if (normal != Vector3.zero)
        {
            if (debugView)
            {
                Debug.DrawLine(transform.position, transform.position + normal.normalized * 0.35f, Color.red);
            }

            return normal.normalized;
        }
        else
        {
            return Vector3.up;
        }
    }


    /*
     * Tries to hitch the pony and the cart
     */
    private void SetCart(bool pullCart)
    {
        Cart cart = GameController.m_harness.GetComponent<Cart>();

        if (pullCart && Vector3.Distance(transform.position, cart.harnessCenter.position) < 0.4f)
        {
            m_pullingCart = pullCart;

            if (m_pullingCart)
            {
                transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.position - GameController.m_harness.position, Vector3.up), transform.up);
                cart.Harness(transform);
            }
        }
        else if (!pullCart)
        {
            m_pullingCart = pullCart;
            cart.RemoveHarness();
        }
    }
}

public class MoveInputs
{
    public float    turn        = 0;
    public float    forward     = 0;
    public bool     run         = false;
    public bool     jump        = false;
}

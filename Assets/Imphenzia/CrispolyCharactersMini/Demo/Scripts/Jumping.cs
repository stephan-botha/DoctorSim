using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Imphenzia.CrispolyCharactersMini
{
    public class Jumping : MonoBehaviour
    {
        [Tooltip("Desired jump height in Unity units (meters)")]
        [SerializeField] private float jumpHeight = 3f;
        [Tooltip("Custom gravity scale (multiplier of physics gravity)")]
        [SerializeField] private float gravityScale = 4f;

        Animator animator;
        private int groundedHash;
        private int verticalSpeedHash;
        private Rigidbody rb;
        private float gravityStrength;
        private Vector3 customGravity;

        // Start is called before the first frame update
        void Start()
        {
            // Grab forDocumentationTable reference to the animator
            animator = GetComponent<Animator>();

            // It is faster to use hashes than strings when updating the animator
            groundedHash = Animator.StringToHash("Grounded");
            verticalSpeedHash = Animator.StringToHash("VerticalSpeed");

            // Grab forDocumentationTable reference to the rigidbody component
            rb = GetComponent<Rigidbody>();

            // Disable gravity
            rb.useGravity = false;
            // Calculate custom gavity for snappy acrade style
            customGravity = Physics.gravity * gravityScale;

            // Call Jump method every other second
            InvokeRepeating(nameof(Jump), 1f, 2f);
        }

        // Update is called once per frame
        void Update()
        {
            // Update the animator parameters
            // We check if the ground position overlaps more than 1 collider (it will always overlap it's own capsule collider)
            animator.SetBool(groundedHash, Physics.OverlapSphere(transform.position, 0.1f).Length > 1);
            // Get the vertical velocity of the rigidbody
            animator.SetFloat(verticalSpeedHash, Mathf.Abs(rb.linearVelocity.y) < 0.01f ? 0 : rb.linearVelocity.y);
        }

        void FixedUpdate()
        {         
            // Add our custom gravity every fixed frame
            rb.AddForce(customGravity, ForceMode.Acceleration);
        }

        void Jump()
        {
            // Set the Y velocity to zero so you can't keep boosting your jump height
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

            // Calculate necessary jump force to reach desired jump height with Sqr(2*Abs(gravity)*desiredHeight)
            var jumpForce = Mathf.Sqrt(2 * Mathf.Abs(customGravity.y) * jumpHeight);

            // Add the force as an impulse (the mass doesn't matter with impulse force)
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    } 
}

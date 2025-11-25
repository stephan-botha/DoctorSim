using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Imphenzia.CrispolyCharactersMini
{
    public class Runner : MonoBehaviour
    {
        private Animator animator;
        private NavMeshAgent agent;
        private float maxSpeed = 5f;

        void Start()
        {
            animator = GetComponentInChildren<Animator>();
            agent = GetComponent<NavMeshAgent>();
            SetRandomDestination();
        }

        void Update()
        {
            if (agent.remainingDistance < 0.5f)
            {
                SetRandomDestination();
            }
            float speed = Vector3.ProjectOnPlane(agent.velocity, Vector3.up).magnitude / maxSpeed;
            animator.SetFloat("Speed", speed);
        }

        private void SetRandomDestination()
        {
            agent.SetDestination(new Vector3(Random.Range(-10, 10), 0, Random.Range(-10, 10)));
            agent.speed = Random.Range(2.5f, 5f);
        }
    }

}
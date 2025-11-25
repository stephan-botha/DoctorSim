using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Imphenzia.CrispolyCharactersMini
{
    public class Strafer : MonoBehaviour
    {
        [SerializeField] private GameObject character = null;
        [SerializeField] private Vector3 facing = Vector3.forward;

        private Animator animator;
        private NavMeshAgent agent;

        void Start()
        {
            animator = GetComponentInChildren<Animator>();
            agent = GetComponentInChildren<NavMeshAgent>();
            SetRandomDestination();
        }

        void Update()
        {
            if (agent.remainingDistance < 0.5f)
            {
                SetRandomDestination();
            }
            character.transform.forward = facing;
            character.transform.position = agent.transform.position;
            animator.SetFloat("SpeedX", character.transform.InverseTransformVector(agent.velocity).normalized.x);
            animator.SetFloat("SpeedZ", character.transform.InverseTransformVector(agent.velocity).normalized.z);

        }

        private void SetRandomDestination()
        {
            agent.SetDestination(new Vector3(Random.Range(-5, 5), 0, Random.Range(-5, 5)));
        }
    }

}
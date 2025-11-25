using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Imphenzia.CrispolyCharactersMini
{
    public class Blacksmith : MonoBehaviour
    {
        [SerializeField] private ParticleSystem sparks = null;
        [SerializeField] private AudioClip[] metalImpacts = null;

        void SparksEvent()
        {
            sparks.Play();
            AudioSource.PlayClipAtPoint(metalImpacts[Random.Range(0, metalImpacts.Length)], transform.position, Random.Range(0.2f, 0.4f));
        }
    } 
}

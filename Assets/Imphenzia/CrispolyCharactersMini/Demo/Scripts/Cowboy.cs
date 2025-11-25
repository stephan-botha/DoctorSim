using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Imphenzia.CrispolyCharactersMini
{
    public class Cowboy : MonoBehaviour
    {
        [SerializeField] AudioClip[] revolverFire = null;
        [SerializeField] GameObject revolver = null;

        void AttackEvent()
        {
            AudioSource.PlayClipAtPoint(revolverFire[Random.Range(0, revolverFire.Length)], transform.position);
        }

        void RevolverAppearEvent()
        {
            revolver.SetActive(!revolver.activeInHierarchy);
        }
    } 
}

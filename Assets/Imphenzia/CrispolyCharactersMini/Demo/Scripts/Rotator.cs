using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Imphenzia.CrispolyCharactersMini
{
    public class Rotator : MonoBehaviour
    {
        [SerializeField] Vector3 rotateBy = Vector3.zero;

        void Update()
        {
            transform.Rotate(rotateBy * Time.deltaTime);
        }
    }

}
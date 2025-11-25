using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Imphenzia.CrispolyCharactersMini
{
    public class Golfer : MonoBehaviour
    {
        [SerializeField] AudioClip golfSwingAudio = null;

        void GolfSwingEvent()
        {
            AudioSource.PlayClipAtPoint(golfSwingAudio, transform.position);
        }
    }

}
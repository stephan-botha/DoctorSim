using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Imphenzia.CrispolyCharactersMini
{
    public class Boxer : MonoBehaviour
    {
        [SerializeField] Transform rightHandHitter = null;
        [SerializeField] Transform leftHandHitter = null;
        [SerializeField] AudioClip[] punchAudioClips = null;
        [SerializeField] AudioClip[] wooshAudioClips = null;
        [SerializeField] AudioClip[] ouchAudioClips = null;

        private Animator animator;

        private string[] punches = new string[] { "PunchStraightLeft", "PunchStraightRight", "PunchBodyLeft", "PunchBodyRight", "PunchUppercutLeft", "PunchUppercutRight" };
        private string[] hurt = new string[] { "HurtBody", "HurtStraight", "HurtUppercut", "HurtUppercut2" };
        private Dictionary<Transform, AudioSource> audioSources = new Dictionary<Transform, AudioSource>();

        void Start()
        {
            animator = GetComponent<Animator>();
            Invoke(nameof(Punch), Random.Range(0.5f, 2f));
            audioSources.Add(rightHandHitter, rightHandHitter.GetComponent<AudioSource>());
            audioSources.Add(leftHandHitter, leftHandHitter.GetComponent<AudioSource>());
        }

        void Punch()
        {
            var punch = punches[Random.Range(0, punches.Length)];
            animator.SetTrigger(punch);
            Invoke(nameof(Punch), Random.Range(0.1f, 1f));

            var hitter = punch.Contains("Right") ? rightHandHitter : leftHandHitter;
            audioSources[hitter].clip = wooshAudioClips[Random.Range(0, wooshAudioClips.Length)];
            audioSources[hitter].volume = Random.Range(0.1f, 0.5f);
            audioSources[hitter].Play();

            //animator.ResetTrigger(punch);
        }

        void AttackEvent(string info)
        {
            var hitter = info.Contains("Right") ? rightHandHitter : leftHandHitter;

            var hitColliders = Physics.OverlapSphere(hitter.position, 0.5f);
            if (hitColliders.Length > 0)
            {
                for (int i = 0; i < hitColliders.Length; i++)
                {

                    if (hitColliders[i].GetComponentInParent<Boxer>().transform != transform)
                    {
                        var otherBoxer = hitColliders[i].GetComponentInParent<Boxer>();

                        if (otherBoxer != null)
                        {
                            // Other boxer hit
                            if (hitColliders[i].transform.parent.name.Contains("Head"))
                            {
                                // head Hit
                                if (info.Contains("Straight"))
                                {
                                    otherBoxer.Hurt("HurtStraight");
                                }
                                else
                                {
                                    otherBoxer.Hurt(Random.Range(0f, 1f) < 0.5f ? "HurtUppercut1" : "HurtUppercut2");
                                }


                            }
                            else
                            {
                                // body hit
                                otherBoxer.Hurt("HurtBody");
                            }
                            audioSources[hitter].clip = punchAudioClips[Random.Range(0, punchAudioClips.Length)];
                            audioSources[hitter].volume = 1;
                            audioSources[hitter].Play();
                        }
                    }
                }
            }

        }

        public void Hurt(string type)
        {
            animator.SetTrigger(type);
            if (Random.Range(0f, 1f) < 0.25f)
            {
                AudioSource.PlayClipAtPoint(ouchAudioClips[Random.Range(0, ouchAudioClips.Length)], transform.position, Random.Range(0f, 1f));
            }
        }

    }
}
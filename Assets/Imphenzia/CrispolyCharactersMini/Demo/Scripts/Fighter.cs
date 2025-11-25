using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Imphenzia.CrispolyCharactersMini
{
    public class Fighter : MonoBehaviour
    {
        [SerializeField] Transform rightHandHitter = null;
        [SerializeField] Transform leftHandHitter = null;
        [SerializeField] Transform rightFootHitter = null;
        [SerializeField] Transform leftFootHitter = null;

        [SerializeField] AudioClip[] attackAudioClips = null;
        [SerializeField] AudioClip[] wooshAudioClips = null;
        [SerializeField] AudioClip[] ouchAudioClips = null;

        private Animator animator;

        private string[] attacks = new string[] { "PunchStraightLeft", "PunchStraightRight", "KickFront", "KickSide" };
        private string[] hurt = new string[] { "HurtBody", "HurtStraight" };
        private Dictionary<Transform, AudioSource> audioSources = new Dictionary<Transform, AudioSource>();


        void Start()
        {
            animator = GetComponent<Animator>();
            Invoke(nameof(Attack), Random.Range(0.5f, 2f));
            audioSources.Add(rightHandHitter, rightHandHitter.GetComponent<AudioSource>());
            audioSources.Add(leftHandHitter, leftHandHitter.GetComponent<AudioSource>());
            audioSources.Add(rightFootHitter, rightFootHitter.GetComponent<AudioSource>());
            audioSources.Add(leftFootHitter, leftFootHitter.GetComponent<AudioSource>());
        }

        void Attack()
        {
            var attack = attacks[Random.Range(0, attacks.Length)];
            animator.SetTrigger(attack);
            Invoke(nameof(Attack), Random.Range(0.1f, 1f));


            var hitter = attack.Contains("Right") ? rightHandHitter : leftHandHitter;
            if (attack.Contains("Kick"))
            {
                hitter = attack.Contains("Right") ? rightFootHitter : leftFootHitter;
            }
            audioSources[hitter].clip = wooshAudioClips[Random.Range(0, wooshAudioClips.Length)];
            audioSources[hitter].volume = Random.Range(0.1f, 0.5f);
            audioSources[hitter].Play();
        }

        void AttackEvent(string info)
        {
            var hitter = info.Contains("Right") ? rightHandHitter : leftHandHitter;
            if (info.Contains("Kick"))
            {
                hitter = info.Contains("Right") ? rightFootHitter : leftFootHitter;
            }


            var hitColliders = Physics.OverlapSphere(hitter.position, 0.5f);
            if (hitColliders.Length > 0)
            {
                for (int i = 0; i < hitColliders.Length; i++)
                {

                    var otherFighter = hitColliders[i].GetComponentInParent<Fighter>();

                    if (otherFighter != null && otherFighter.transform != transform)
                    {
                        if (otherFighter != null)
                        {
                            // Other boxer hit
                            if (hitColliders[i].transform.parent.name.Contains("Head"))
                            {

                                // head Hit
                                otherFighter.Hurt("HurtStraight");
                            }
                            else
                            {
                                // body hit
                                otherFighter.Hurt("HurtBody");
                            }
                            audioSources[hitter].clip = attackAudioClips[Random.Range(0, attackAudioClips.Length)];
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
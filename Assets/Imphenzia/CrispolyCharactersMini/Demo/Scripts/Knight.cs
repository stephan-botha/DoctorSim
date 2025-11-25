using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Imphenzia.CrispolyCharactersMini
{
    public class Knight : MonoBehaviour
    {
        [SerializeField] Transform swordHitterTip = null;
        [SerializeField] Transform swordHitterMiddle = null;

        [SerializeField] AudioClip[] attackAudioClips = null;
        [SerializeField] AudioClip[] wooshAudioClips = null;
        [SerializeField] AudioClip[] ouchAudioClips = null;

        private Animator animator;

        private string[] attacks = new string[] { "SwordHit1", "SwordHit2", "SwordSlash1", "SwordSlash2", "SwordStab1" };
        private string[] hurt = new string[] { "HurtMid", "HurtSlash", "HurtHigh" };
        private Dictionary<Transform, AudioSource> audioSources = new Dictionary<Transform, AudioSource>();


        void Start()
        {
            animator = GetComponent<Animator>();
            Invoke(nameof(Attack), Random.Range(0.5f, 2f));
            audioSources.Add(swordHitterTip, swordHitterTip.GetComponent<AudioSource>());
            audioSources.Add(swordHitterMiddle, swordHitterMiddle.GetComponent<AudioSource>());
        }

        void Attack()
        {
            var attack = attacks[Random.Range(0, attacks.Length)];
            animator.SetTrigger(attack);
            Invoke(nameof(Attack), Random.Range(0.1f, 1f));
            AudioSource.PlayClipAtPoint(wooshAudioClips[Random.Range(0, wooshAudioClips.Length)], transform.position, Random.Range(0.1f, 0.5f));
        }

        void AttackEvent(string info)
        {
            Collider[] hitColliders = new Collider[0];

            if (info.Contains("Stab"))
            {
                hitColliders = Physics.OverlapSphere(swordHitterTip.position, 0.5f);
            }
            else if (info.Contains("Slash"))
            {
                hitColliders = Physics.OverlapSphere(swordHitterMiddle.position, 0.5f);
            }
            else if (info.Contains("Hit"))
            {
                hitColliders = Physics.OverlapSphere(swordHitterMiddle.position, 0.5f);
            }

            if (hitColliders.Length > 0)
            {
                for (int i = 0; i < hitColliders.Length; i++)
                {

                    var otherKnight = hitColliders[i].GetComponentInParent<Knight>();
                    if (otherKnight != null && otherKnight.transform != transform)
                    {
                        if (info.Contains("Stab"))
                        {
                            otherKnight.Hurt("HurtMid");
                        }
                        else if (info.Contains("Slash"))
                        {
                            otherKnight.Hurt("HurtSlash");
                        }
                        else if (info.Contains("Hit"))
                        {
                            otherKnight.Hurt("HurtHigh");
                        }

                        AudioSource.PlayClipAtPoint(attackAudioClips[Random.Range(0, attackAudioClips.Length)], transform.position, Random.Range(0.2f, 0.4f));
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
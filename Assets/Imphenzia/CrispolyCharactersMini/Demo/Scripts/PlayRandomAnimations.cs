using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Imphenzia.CrispolyCharactersMini
{
    public class PlayRandomAnimations : MonoBehaviour
    {

        [SerializeField] private AnimationClip[] animationClips = null;

        private Animator animator;

        void Start()
        {
            animator = GetComponent<Animator>();
            PlayRandomAnimation();
        }

        IEnumerator PlayAnimation(AnimationClip clip)
        {
            animator.CrossFade(clip.name, 0.025f);

            yield return new WaitForSeconds(clip.length - 0.025f);
            if (clip.name.ToLower().Contains("loop") || clip.name.ToLower().Contains("fall") || clip.name.ToLower().Contains("knockedout"))
            {
                yield return new WaitForSeconds(clip.length);
                yield return new WaitForSeconds(clip.length);
            }

            PlayRandomAnimation();
        }

        void PlayRandomAnimation()
        {
            StartCoroutine(PlayAnimation(animationClips[Random.Range(0, animationClips.Length)]));
        }
    }
}
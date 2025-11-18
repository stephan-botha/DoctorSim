using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DG.Tweening;
using MedMania.Core.Data.ScriptableObjects;
using MedMania.Presentation.Views.Hands;

public class HandAnimator_PlayModeTests
{
    [UnityTest]
    public IEnumerator Reach_and_release_align_target_to_anchor()
    {
        var created = new List<UnityEngine.Object>();
        try
        {
            DOTween.KillAll();

            var root = new GameObject("HandRoot");
            created.Add(root);

            var target = new GameObject("HandTarget");
            target.transform.SetParent(root.transform, false);
            created.Add(target);

            var anchor = new GameObject("Anchor");
            anchor.transform.position = new Vector3(0.5f, 1f, 0.25f);
            anchor.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
            created.Add(anchor);

            var animator = root.AddComponent<HandAnimator>();

            var reachPreset = ScriptableObject.CreateInstance<MotionPreset>();
            created.Add(reachPreset);
            SetPrivateField(reachPreset, "_duration", 0.1f);
            SetPrivateField(reachPreset, "_ease", Ease.Linear);

            var tapPreset = ScriptableObject.CreateInstance<MotionPreset>();
            created.Add(tapPreset);
            SetPrivateField(tapPreset, "_duration", 0.1f);
            SetPrivateField(tapPreset, "_ease", Ease.Linear);

            SetPrivateField(animator, "_handTarget", target.transform);
            SetPrivateField(animator, "_reachPreset", reachPreset);
            SetPrivateField(animator, "_tapPreset", tapPreset);
            animator.RefreshNeutralPose();

            var initialLocalPosition = target.transform.localPosition;
            var initialLocalRotation = target.transform.localRotation;

            var sequence = animator.Reach(anchor.transform, 0f);
            sequence?.Complete(true);

            Assert.Less(Vector3.Distance(target.transform.position, anchor.transform.position), 0.001f,
                "Reach should move the hand target to the anchor position.");
            Assert.Less(Quaternion.Angle(target.transform.rotation, anchor.transform.rotation), 0.1f,
                "Reach should align the hand target rotation with the anchor.");

            animator.Release(root.transform);
            DOTween.Complete(animator);

            Assert.Less(Vector3.Distance(target.transform.localPosition, initialLocalPosition), 0.001f,
                "Release should return the hand target to its neutral local position.");
            Assert.Less(Quaternion.Angle(target.transform.localRotation, initialLocalRotation), 0.1f,
                "Release should restore the hand target's neutral rotation.");
        }
        finally
        {
            DOTween.KillAll();
            for (int i = created.Count - 1; i >= 0; i--)
            {
                var obj = created[i];
                if (obj != null)
                {
                    UnityEngine.Object.DestroyImmediate(obj);
                }
            }
        }

        yield return null;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        if (target == null) throw new ArgumentNullException(nameof(target));

        var type = target.GetType();
        var field = default(System.Reflection.FieldInfo);
        while (type != null)
        {
            field = type.GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (field != null)
            {
                break;
            }

            type = type.BaseType;
        }

        if (field == null)
        {
            throw new InvalidOperationException($"Field '{fieldName}' not found on {target.GetType().FullName}.");
        }

        field.SetValue(target, value);
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using DG.Tweening;
using MedMania.Core.Data.ScriptableObjects;
using MedMania.Core.Domain.Patients;
using MedMania.Core.Domain.Procedures;
using MedMania.Core.Services.Timing;
using MedMania.Presentation.Input.Staff;
using MedMania.Presentation.Views.Carry;
using MedMania.Presentation.Views.Hands;
using MedMania.Presentation.Views.Inventory;
using MedMania.Presentation.Views.Patients;
using MedMania.Presentation.Views.Procedures;

public class ProcedureRun_Integration_PlayMode
{
    [UnityTest]
    public IEnumerator Procedure_only_advances_when_equipment_is_occupied()
    {
        var world = new ProcedureEquipmentTestWorld(0.1f);
        try
        {
            yield return null; // allow Awake/OnEnable wiring

            var patient = world.Patient.Domain;

            InvokePerform(world.Staff, world.Procedure);
            yield return null; // ensure runner processed the request
            Assert.AreEqual(PatientState.Waiting, patient.State, "Patient should remain waiting when equipment has no occupant.");

            world.PlacePatientInEquipment();
            yield return null;

            InvokePerform(world.Staff, world.Procedure);
            yield return null;
            Assert.AreEqual(PatientState.UnderTest, patient.State, "Patient should begin procedure once placed in equipment.");

            yield return new WaitForSeconds(0.2f);
            yield return null;
            Assert.AreEqual(PatientState.Diagnosed, patient.State, "Patient should complete the procedure after the scheduled duration.");
        }
        finally
        {
            world.Dispose();
        }
    }

    [UnityTest]
    public IEnumerator Procedure_cancels_when_patient_leaves_equipment()
    {
        var world = new ProcedureEquipmentTestWorld(1f);
        try
        {
            yield return null;
            world.PlacePatientInEquipment();
            yield return null;

            InvokePerform(world.Staff, world.Procedure);
            yield return null;
            Assert.AreEqual(PatientState.UnderTest, world.Patient.Domain.State, "Run should start when equipment is occupied.");

            world.RemovePatientFromEquipment();
            yield return null;

            Assert.AreEqual(PatientState.Waiting, world.Patient.Domain.State, "Removing the patient should cancel the run and reset their state.");
        }
        finally
        {
            world.Dispose();
        }
    }

    private static void InvokePerform(StaffAgent staff, IProcedureDef procedure)
    {
        staff.PerformRequested?.Invoke(procedure);
        var field = typeof(StaffAgent).GetField("_performRequestedHandlers", BindingFlags.Instance | BindingFlags.NonPublic);
        var handlers = (Action<IProcedureDef>)field?.GetValue(staff);
        handlers?.Invoke(procedure);
    }

    private sealed class ProcedureEquipmentTestWorld : IDisposable
    {
        private readonly List<UnityEngine.Object> _toDestroy = new();
        private readonly PatientCarryView _patientCarry;

        public StaffAgent Staff { get; }
        public ProcedureRunner Runner { get; }
        public EquipmentView Equipment { get; }
        public CarrySlot EquipmentSlot { get; }
        public PatientView Patient { get; }
        public TestSO Procedure { get; }

        public ProcedureEquipmentTestWorld(float durationSeconds)
        {
            var timerGO = new GameObject("TimerService");
            _toDestroy.Add(timerGO);
            timerGO.AddComponent<GameTimerService>();

            var equipmentAsset = ScriptableObject.CreateInstance<EquipmentSO>();
            SetPrivateField(equipmentAsset, "_name", "Scanner");
            _toDestroy.Add(equipmentAsset);

            Procedure = ScriptableObject.CreateInstance<TestSO>();
            SetPrivateField(Procedure, "_durationSeconds", durationSeconds);
            SetPrivateField(Procedure, "_requiredEquipment", equipmentAsset);
            _toDestroy.Add(Procedure);

            var disease = ScriptableObject.CreateInstance<DiseaseSO>();
            SetPrivateField(disease, "_tests", new[] { Procedure });
            SetPrivateField(disease, "_treatments", Array.Empty<TreatmentSO>());
            _toDestroy.Add(disease);

            var patientGO = new GameObject("Patient");
            _toDestroy.Add(patientGO);
            Patient = patientGO.AddComponent<PatientView>();
            Patient.Configure(disease, "Pat");
            _patientCarry = patientGO.AddComponent<PatientCarryView>();

            var equipmentGO = new GameObject("Equipment");
            _toDestroy.Add(equipmentGO);
            var anchorGO = new GameObject("Anchor");
            anchorGO.transform.SetParent(equipmentGO.transform, false);
            _toDestroy.Add(anchorGO);
            var slotGO = new GameObject("Slot");
            slotGO.transform.SetParent(equipmentGO.transform, false);
            EquipmentSlot = slotGO.AddComponent<CarrySlot>();
            SetPrivateField(EquipmentSlot, "_captureExistingChild", false);
            _toDestroy.Add(slotGO);

            Equipment = equipmentGO.AddComponent<EquipmentView>();
            SetPrivateField(Equipment, "_equipment", equipmentAsset);
            SetPrivateField(Equipment, "_procedure", Procedure);
            SetPrivateField(Equipment, "_patientSlot", EquipmentSlot);
            SetPrivateField(Equipment, "_interactionAnchor", anchorGO.transform);

            var staffGO = new GameObject("Staff");
            _toDestroy.Add(staffGO);
            var rb = staffGO.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;

            var handsGO = new GameObject("Hands");
            handsGO.transform.SetParent(staffGO.transform, false);
            _toDestroy.Add(handsGO);
            var hands = handsGO.AddComponent<CarrySlot>();
            SetPrivateField(hands, "_captureExistingChild", false);

            var reachPreset = ScriptableObject.CreateInstance<MotionPreset>();
            SetPrivateField(reachPreset, "_duration", 0.2f);
            SetPrivateField(reachPreset, "_ease", Ease.OutCubic);
            _toDestroy.Add(reachPreset);

            var hoverPreset = ScriptableObject.CreateInstance<MotionPreset>();
            SetPrivateField(hoverPreset, "_duration", 1.2f);
            SetPrivateField(hoverPreset, "_hoverRadius", 0.01f);
            SetPrivateField(hoverPreset, "_hoverPeriod", 1.4f);
            SetPrivateField(hoverPreset, "_hoverTiltDegrees", 2f);
            _toDestroy.Add(hoverPreset);

            var tapPreset = ScriptableObject.CreateInstance<MotionPreset>();
            SetPrivateField(tapPreset, "_duration", 0.2f);
            SetPrivateField(tapPreset, "_ease", Ease.InOutCubic);
            SetPrivateField(tapPreset, "_punchStrength", 6f);
            SetPrivateField(tapPreset, "_vibrato", 6);
            SetPrivateField(tapPreset, "_elasticity", 0.4f);
            _toDestroy.Add(tapPreset);

            var leftRig = new GameObject("LeftHandRig");
            leftRig.transform.SetParent(staffGO.transform, false);
            _toDestroy.Add(leftRig);
            var leftTarget = new GameObject("LeftHandTarget");
            leftTarget.transform.SetParent(leftRig.transform, false);
            _toDestroy.Add(leftTarget);
            var leftAnimator = leftRig.AddComponent<HandAnimator>();
            SetPrivateField(leftAnimator, "_handTarget", leftTarget.transform);
            SetPrivateField(leftAnimator, "_reachPreset", reachPreset);
            SetPrivateField(leftAnimator, "_hoverPreset", hoverPreset);
            SetPrivateField(leftAnimator, "_tapPreset", tapPreset);
            leftAnimator.RefreshNeutralPose();

            var rightRig = new GameObject("RightHandRig");
            rightRig.transform.SetParent(staffGO.transform, false);
            _toDestroy.Add(rightRig);
            var rightTarget = new GameObject("RightHandTarget");
            rightTarget.transform.SetParent(rightRig.transform, false);
            _toDestroy.Add(rightTarget);
            var rightAnimator = rightRig.AddComponent<HandAnimator>();
            SetPrivateField(rightAnimator, "_handTarget", rightTarget.transform);
            SetPrivateField(rightAnimator, "_reachPreset", reachPreset);
            SetPrivateField(rightAnimator, "_hoverPreset", hoverPreset);
            SetPrivateField(rightAnimator, "_tapPreset", tapPreset);
            rightAnimator.RefreshNeutralPose();

            Staff = staffGO.AddComponent<StaffAgent>();
            SetPrivateField(Staff, "_heldProcedure", Procedure);

            var runnerGO = new GameObject("Runner");
            runnerGO.transform.SetParent(staffGO.transform, false);
            runnerGO.transform.localPosition = Vector3.zero;
            _toDestroy.Add(runnerGO);
            Runner = runnerGO.AddComponent<ProcedureRunner>();

            var controllerGO = new GameObject("HandsController");
            controllerGO.transform.SetParent(staffGO.transform, false);
            controllerGO.SetActive(false);
            _toDestroy.Add(controllerGO);
            var controller = controllerGO.AddComponent<DisembodiedHandsController>();
            SetPrivateField(controller, "_leftHand", leftAnimator);
            SetPrivateField(controller, "_rightHand", rightAnimator);
            SetPrivateField(controller, "_staff", Staff);
            SetPrivateField(controller, "_procedureRunner", Runner);
            SetPrivateField(controller, "_fallbackAnchor", anchorGO.transform);
            controllerGO.SetActive(true);

            staffGO.transform.position = anchorGO.transform.position;
            runnerGO.transform.position = anchorGO.transform.position;
        }

        public void PlacePatientInEquipment()
        {
            if (EquipmentSlot.IsEmpty)
            {
                EquipmentSlot.TryPlace(_patientCarry);
            }
        }

        public void RemovePatientFromEquipment()
        {
            if (EquipmentSlot.TryTake(out var carryable) && carryable is PatientCarryView patient)
            {
                var transform = patient.transform;
                transform.SetParent(null, true);
                transform.position = EquipmentSlot.transform.position + Vector3.right;
            }
        }

        public void Dispose()
        {
            for (int i = _toDestroy.Count - 1; i >= 0; i--)
            {
                var obj = _toDestroy[i];
                if (obj != null)
                {
                    UnityEngine.Object.DestroyImmediate(obj);
                }
            }

            _toDestroy.Clear();
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        if (target == null) throw new ArgumentNullException(nameof(target));

        var type = target.GetType();
        FieldInfo field = null;
        while (type != null)
        {
            field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
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

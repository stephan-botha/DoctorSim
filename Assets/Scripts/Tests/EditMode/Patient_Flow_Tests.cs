using NUnit.Framework;
using System;
using MedMania.Core.Domain.Patients;
using MedMania.Core.Domain.Procedures;
using MedMania.Core.Domain.Diseases;

public class Patient_Flow_Tests
{
    [Test]
    public void Patient_Completes_All_Tests_And_Discharges()
    {
        var testA = new StubProcedure("Scan A", ProcedureKind.Test);
        var testB = new StubProcedure("Scan B", ProcedureKind.Test);
        var treatment = new StubProcedure("Med", ProcedureKind.Treatment);
        var disease = new StubDisease(new[] { testA, testB }, new[] { treatment });
        var patient = new Patient("Pat", disease);

        Assert.False(patient.DiagnosisKnown);
        Assert.AreEqual(0, patient.CompletedTestCount);
        Assert.AreEqual(2, patient.TotalTestCount);
        Assert.False(patient.AreAllTestsCompleted);

        Assert.IsTrue(patient.TryBeginProcedure(testA));
        patient.CompleteProcedure(testA);
        Assert.False(patient.DiagnosisKnown);
        Assert.AreEqual(1, patient.CompletedTestCount);
        Assert.AreEqual(PatientState.Waiting, patient.State);
        Assert.IsFalse(patient.TryBeginProcedure(treatment));

        Assert.IsTrue(patient.TryBeginProcedure(testB));
        patient.CompleteProcedure(testB);
        Assert.IsTrue(patient.DiagnosisKnown);
        Assert.AreEqual(2, patient.CompletedTestCount);
        Assert.AreEqual(PatientState.Diagnosed, patient.State);
        Assert.True(patient.AreAllTestsCompleted);

        Assert.IsTrue(patient.TryBeginProcedure(treatment));
        patient.CompleteProcedure(treatment);
        Assert.AreEqual(PatientState.ReadyForDischarge, patient.State);

        Assert.IsTrue(patient.TryDischarge());
        Assert.AreEqual(PatientState.Discharged, patient.State);
        Assert.IsFalse(patient.TryDischarge());
    }

    [Test]
    public void Patient_Blocks_Repeated_Treatments_After_Ready()
    {
        var test = new StubProcedure("Scan", ProcedureKind.Test);
        var treatment = new StubProcedure("Med", ProcedureKind.Treatment);
        var disease = new StubDisease(new[] { test }, new[] { treatment });
        var patient = new Patient("Pat", disease);

        Assert.IsTrue(patient.TryBeginProcedure(test));
        patient.CompleteProcedure(test);
        Assert.IsTrue(patient.TryBeginProcedure(treatment));
        patient.CompleteProcedure(treatment);
        Assert.AreEqual(PatientState.ReadyForDischarge, patient.State);

        Assert.IsFalse(patient.TryBeginProcedure(treatment));
        Assert.IsTrue(patient.TryDischarge());
        Assert.AreEqual(PatientState.Discharged, patient.State);
        Assert.IsFalse(patient.TryBeginProcedure(treatment));
    }

    [Test]
    public void Cancelling_Treatment_Reverts_To_Diagnosed()
    {
        var test = new StubProcedure("Scan", ProcedureKind.Test);
        var treatment = new StubProcedure("Med", ProcedureKind.Treatment);
        var disease = new StubDisease(new[] { test }, new[] { treatment });
        var patient = new Patient("Pat", disease);

        Assert.IsTrue(patient.TryBeginProcedure(test));
        patient.CompleteProcedure(test);
        Assert.AreEqual(PatientState.Diagnosed, patient.State);

        Assert.IsTrue(patient.TryBeginProcedure(treatment));
        patient.CancelActiveProcedure();
        Assert.AreEqual(PatientState.Diagnosed, patient.State);
        Assert.IsTrue(patient.TryBeginProcedure(treatment));
    }

    [Test]
    public void Cancelling_Handle_Reverts_State()
    {
        var test = new StubProcedure("Scan", ProcedureKind.Test);
        var treatment = new StubProcedure("Med", ProcedureKind.Treatment);
        var disease = new StubDisease(new[] { test }, new[] { treatment });
        var patient = new Patient("Pat", disease);
        var context = new ManualContext();

        Assert.IsTrue(patient.TryBeginProcedure(test));
        patient.CompleteProcedure(test);
        var handle = ProcedureRun.TryRun(patient, treatment, context);
        Assert.NotNull(handle);
        Assert.AreEqual(PatientState.UnderTreatment, patient.State);
        handle.Dispose();

        Assert.AreEqual(PatientState.Diagnosed, patient.State);
        Assert.IsTrue(context.HandleDisposed);
        Assert.IsFalse(context.CompletionInvoked);
        Assert.IsTrue(patient.TryBeginProcedure(treatment));
        patient.CancelActiveProcedure();
    }

    private sealed class StubProcedure : IProcedureDef
    {
        public StubProcedure(string name, ProcedureKind kind)
        {
            Name = name;
            Kind = kind;
        }

        public string Name { get; }
        public ProcedureKind Kind { get; }
        public float DurationSeconds => 1f;
        public MedMania.Core.Domain.Inventory.IToolDef RequiredTool => null;
        public MedMania.Core.Domain.Inventory.IEquipmentDef RequiredEquipment => null;
    }

    private sealed class StubDisease : IDiseaseDef
    {
        public StubDisease(IProcedureDef[] tests, IProcedureDef[] treatments)
        {
            Tests = tests;
            Treatments = treatments;
        }

        public StubDisease(IProcedureDef test, IProcedureDef treatment)
            : this(new[] { test }, new[] { treatment })
        {
        }

        public string Name => "D";
        public IProcedureDef[] Tests { get; }
        public IProcedureDef[] Treatments { get; }
        public float MaxWaitSeconds => 60f;
    }

    private sealed class ManualContext : IProcedureContext
    {
        public bool HandleDisposed { get; private set; }
        public bool CompletionInvoked { get; private set; }
        private Action _completion;

        public IDisposable Schedule(TimeSpan duration, Action onCompleted, System.Threading.CancellationToken token = default)
        {
            _completion = () =>
            {
                CompletionInvoked = true;
                onCompleted?.Invoke();
            };

            return new ManualHandle(() => HandleDisposed = true);
        }

        public void TriggerCompletion()
        {
            _completion?.Invoke();
        }

        public bool IsEquipmentAvailable(MedMania.Core.Domain.Inventory.IEquipmentDef equipment) => true;

        private sealed class ManualHandle : IDisposable
        {
            private readonly Action _onDispose;
            private bool _disposed;

            public ManualHandle(Action onDispose)
            {
                _onDispose = onDispose;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _onDispose?.Invoke();
            }
        }
    }
}

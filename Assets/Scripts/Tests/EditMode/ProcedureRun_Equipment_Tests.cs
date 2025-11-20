using System;
using System.Linq;
using NUnit.Framework;
using MedMania.Core.Domain.Procedures;
using MedMania.Core.Domain.Patients;
using MedMania.Core.Domain.Diseases;
using MedMania.Core.Domain.Inventory;

public class ProcedureRun_Equipment_Tests
{
    [Test]
    public void TryRun_Fails_When_Equipment_Unavailable()
    {
        var equipment = new StubEquipment("MRI");
        var procedure = new StubProcedure("Scan", ProcedureKind.Test, equipment);
        var disease = new StubDisease(procedure);
        var patient = new Patient("Pat", disease);
        var context = new EquipmentGateContext { Available = false };

        var run = ProcedureRun.TryRun(patient, procedure, context);

        Assert.IsNull(run);
        Assert.AreEqual(PatientState.Waiting, patient.State);
        Assert.AreEqual(0, context.ScheduleCalls);
    }

    [Test]
    public void TryRun_Succeeds_When_Equipment_Available()
    {
        var equipment = new StubEquipment("MRI");
        var procedure = new StubProcedure("Scan", ProcedureKind.Test, equipment);
        var disease = new StubDisease(procedure);
        var patient = new Patient("Pat", disease);
        var context = new EquipmentGateContext { Available = true };

        var run = ProcedureRun.TryRun(patient, procedure, context);

        Assert.IsNotNull(run);
        Assert.AreEqual(1, context.ScheduleCalls);
        Assert.AreEqual(PatientState.UnderTest, patient.State);
        run.Dispose();
    }

    [Test]
    public void TryRun_Checks_Equipment_Gate_Before_Asking_Patient()
    {
        var equipment = new StubEquipment("MRI");
        var procedure = new StubProcedure("Scan", ProcedureKind.Test, equipment);
        var disease = new StubDisease(procedure);
        var patient = new FakePatient(disease);
        var context = new EquipmentGateContext { Available = false };

        var firstAttempt = ProcedureRun.TryRun(patient, procedure, context);

        Assert.IsNull(firstAttempt);
        Assert.AreEqual(0, patient.BeginCalls, "Patient should not be queried when equipment is unavailable.");
        Assert.AreEqual(0, context.ScheduleCalls);

        context.Available = true;
        patient.BeginResult = true;

        var secondAttempt = ProcedureRun.TryRun(patient, procedure, context);

        Assert.IsNotNull(secondAttempt);
        Assert.AreEqual(1, patient.BeginCalls, "Patient should be queried once equipment becomes available.");
        Assert.AreEqual(1, context.ScheduleCalls);

        secondAttempt.Dispose();
        Assert.AreEqual(1, patient.CancelCalls);
    }

    private sealed class EquipmentGateContext : IProcedureContext
    {
        public bool Available { get; set; }
        public int ScheduleCalls { get; private set; }

        public IDisposable Schedule(TimeSpan duration, Action onCompleted, System.Threading.CancellationToken token = default)
        {
            ScheduleCalls++;
            return new Cancel(onCompleted);
        }

        public bool IsEquipmentAvailable(IEquipmentDef equipment) => Available;

        private sealed class Cancel : IDisposable
        {
            private readonly Action _onDisposed;

            public Cancel(Action onDisposed)
            {
                _onDisposed = onDisposed;
            }

            public void Dispose()
            {
                _onDisposed?.Invoke();
            }
        }
    }

    private sealed class StubEquipment : IEquipmentDef
    {
        public StubEquipment(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }

    private sealed class StubProcedure : IProcedureDef
    {
        private readonly IEquipmentDef _equipment;

        public StubProcedure(string name, ProcedureKind kind, IEquipmentDef equipment)
        {
            Name = name;
            Kind = kind;
            _equipment = equipment;
            InteractionText = name;
        }

        public string Name { get; }
        public string InteractionText { get; }
        public ProcedureKind Kind { get; }
        public float DurationSeconds => 1f;
        public IToolDef RequiredTool => null;
        public IEquipmentDef RequiredEquipment => _equipment;
    }

    private sealed class StubDisease : IDiseaseDef
    {
        public StubDisease(IProcedureDef procedure)
        {
            Tests = new[] { procedure };
            Treatments = Array.Empty<IProcedureDef>();
        }

        public string Name => "D";
        public IProcedureDef[] Tests { get; }
        public IProcedureDef[] Treatments { get; }
        public float MaxWaitSeconds => 120f;
    }

    private sealed class FakePatient : MedMania.Core.Domain.Patients.IPatient
    {
        private MedMania.Core.Domain.Patients.PatientState _state = MedMania.Core.Domain.Patients.PatientState.Waiting;
        private readonly IDiseaseDef _disease;

        public FakePatient(IDiseaseDef disease)
        {
            _disease = disease;
            TotalTestCount = disease?.Tests?.Count(t => t != null) ?? 0;
        }

        public Guid Id { get; } = Guid.NewGuid();
        public string DisplayName => "Fake";
        public MedMania.Core.Domain.Patients.PatientState State => _state;
        public IDiseaseDef Disease => _disease;
        public bool DiagnosisKnown { get; private set; }
        public int CompletedTestCount { get; private set; }
        public int TotalTestCount { get; }
        public bool AreAllTestsCompleted => CompletedTestCount >= TotalTestCount;
        public int BeginCalls { get; private set; }
        public bool BeginResult { get; set; }
        public int CancelCalls { get; private set; }

        public bool TryBeginProcedure(IProcedureDef procedure)
        {
            BeginCalls++;
            if (BeginResult)
            {
                _state = MedMania.Core.Domain.Patients.PatientState.UnderTest;
            }

            return BeginResult;
        }

        public void CompleteProcedure(IProcedureDef procedure)
        {
            DiagnosisKnown = true;
            CompletedTestCount = TotalTestCount;
            _state = MedMania.Core.Domain.Patients.PatientState.Diagnosed;
        }

        public void CancelActiveProcedure()
        {
            CancelCalls++;
            _state = DiagnosisKnown
                ? MedMania.Core.Domain.Patients.PatientState.Diagnosed
                : MedMania.Core.Domain.Patients.PatientState.Waiting;
        }

        public bool TryDischarge() => false;
    }
}

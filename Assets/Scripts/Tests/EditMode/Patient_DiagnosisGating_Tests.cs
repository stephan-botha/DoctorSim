// MedMania.Tests.EditMode
// Ensures treatments are blocked until tests reveal diagnosis.

using NUnit.Framework;
using MedMania.Core.Domain.Patients;
using MedMania.Core.Domain.Diseases;
using MedMania.Core.Domain.Procedures;

public class Patient_DiagnosisGating_Tests
{
    private class Proc : IProcedureDef
    {
        public string Name { get; set; }
        public string InteractionText { get; set; }
        public ProcedureKind Kind { get; set; }
        public float DurationSeconds { get; set; } = 1f;
        public MedMania.Core.Domain.Inventory.IToolDef RequiredTool => null;
        public MedMania.Core.Domain.Inventory.IEquipmentDef RequiredEquipment => null;
    }

    private class Disease : IDiseaseDef
    {
        public string Name => "X";
        public IProcedureDef[] Tests { get; set; }
        public IProcedureDef[] Treatments { get; set; }
        public float MaxWaitSeconds => 120f;
    }

    [Test]
    public void TreatmentsBlocked_UntilDiagnosisKnown()
    {
        var testA = new Proc { Name = "EKG", Kind = ProcedureKind.Test };
        var testB = new Proc { Name = "XRay", Kind = ProcedureKind.Test };
        var treat = new Proc { Name = "Aspirin", Kind = ProcedureKind.Treatment };
        var d = new Disease { Tests = new[] { testA, testB }, Treatments = new[] { treat } };

        var p = new Patient("P1", d);

        Assert.False(p.TryBeginProcedure(treat)); // blocked
        Assert.True(p.TryBeginProcedure(testA));
        p.CompleteProcedure(testA);
        Assert.False(p.DiagnosisKnown);
        Assert.False(p.AreAllTestsCompleted);
        Assert.False(p.TryBeginProcedure(treat));

        Assert.True(p.TryBeginProcedure(testB));
        p.CompleteProcedure(testB);
        Assert.True(p.DiagnosisKnown);
        Assert.True(p.AreAllTestsCompleted);
        Assert.True(p.TryBeginProcedure(treat)); // now allowed
    }

    [Test]
    public void NullEntriesIgnored_InDiseaseDefinitions()
    {
        var testA = new Proc { Name = "MRI", Kind = ProcedureKind.Test };
        var testB = new Proc { Name = "CAT", Kind = ProcedureKind.Test };
        var treat = new Proc { Name = "Steroids", Kind = ProcedureKind.Treatment };
        var disease = new Disease
        {
            Tests = new IProcedureDef[] { null, testA, null, testB },
            Treatments = new IProcedureDef[] { null, treat }
        };

        var patient = new Patient("P2", disease);

        Assert.False(patient.TryBeginProcedure(treat));
        Assert.True(patient.TryBeginProcedure(testA));

        patient.CompleteProcedure(testA);
        Assert.False(patient.DiagnosisKnown);
        Assert.False(patient.AreAllTestsCompleted);

        Assert.True(patient.TryBeginProcedure(testB));
        patient.CompleteProcedure(testB);
        Assert.True(patient.DiagnosisKnown);
        Assert.True(patient.AreAllTestsCompleted);

        Assert.True(patient.TryBeginProcedure(treat));
        patient.CompleteProcedure(treat);

        Assert.AreEqual(PatientState.ReadyForDischarge, patient.State);
    }
}

using NUnit.Framework;
using System;
using MedMania.Core.Domain.Procedures;
using MedMania.Core.Domain.Patients;

public class Procedure_Interruption_Tests
{
    private sealed class ImmediateContext : IProcedureContext
    {
        public IDisposable Schedule(TimeSpan duration, Action onCompleted, System.Threading.CancellationToken token = default)
        {
            return new Cancel(() => { });
        }

        public bool IsEquipmentAvailable(MedMania.Core.Domain.Inventory.IEquipmentDef equipment) => true;

        private sealed class Cancel : IDisposable { private readonly Action _a; public Cancel(Action a){_a=a;} public void Dispose()=>_a(); }
    }

    [Test]
    public void Cancel_RevertsState()
    {
        var test = new StubProc("EKG", ProcedureKind.Test);
        var d = new StubDisease(test);
        var p = new Patient("P", d);

        var handle = ProcedureRun.TryRun(p, test, new ImmediateContext());
        Assert.NotNull(handle);
        p.CancelActiveProcedure();
        Assert.AreEqual(PatientState.Waiting, p.State);
    }

    private sealed class StubProc : IProcedureDef
    {
        public StubProc(string name, ProcedureKind kind)
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
    private sealed class StubDisease : MedMania.Core.Domain.Diseases.IDiseaseDef
    {
        public string Name => "D";
        public IProcedureDef[] Tests { get; }
        public IProcedureDef[] Treatments { get; } = Array.Empty<IProcedureDef>();
        public float MaxWaitSeconds => 120f;
        public StubDisease(IProcedureDef t){ Tests = new[]{t}; }
    }
}

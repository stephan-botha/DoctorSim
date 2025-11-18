// MedMania.Core.Domain
// Patient.cs
// Responsibility: Patient state machine enforcing GDD rules.
// Notes: No Unity types. All timing handled by external IProcedureContext/Services.

using System;
using System.Collections.Generic;
using System.Linq;
using MedMania.Core.Domain.Diseases;
using MedMania.Core.Domain.Procedures;

namespace MedMania.Core.Domain.Patients
{
    public sealed class Patient : IPatient
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string DisplayName { get; }
        public PatientState State { get; private set; } = PatientState.Waiting;
        public IDiseaseDef Disease { get; }
        public bool DiagnosisKnown { get; private set; }
        public int CompletedTestCount => _completedTests.Count;
        public int TotalTestCount => _tests.Length;
        public bool AreAllTestsCompleted => CompletedTestCount >= TotalTestCount;

        private readonly IProcedureDef[] _tests;
        private readonly IProcedureDef[] _treatments;
        private readonly HashSet<IProcedureDef> _completedTests;
        private IProcedureDef _active = null;

        public Patient(string displayName, IDiseaseDef disease)
        {
            DisplayName = displayName ?? "Patient";
            Disease = disease ?? throw new ArgumentNullException(nameof(disease));

            _tests = FilterProcedures(Disease.Tests);
            _treatments = FilterProcedures(Disease.Treatments);
            _completedTests = new HashSet<IProcedureDef>();

            RefreshDiagnosisState();
        }

        public bool TryBeginProcedure(IProcedureDef procedure)
        {
            if (procedure == null) return false;
            if (_active != null) return false;
            if (State == PatientState.Discharged) return false;

            if (TryResolveCandidate(_tests, procedure, out var matchedTest))
            {
                if (_completedTests.Contains(matchedTest) || AreAllTestsCompleted)
                {
                    return false;
                }

                if (State == PatientState.Waiting || State == PatientState.UnderTest)
                {
                    _active = procedure;
                    State = PatientState.UnderTest;
                    return true;
                }

                return false;
            }

            if (TryResolveCandidate(_treatments, procedure, out _))
            {
                if (DiagnosisKnown && (State == PatientState.Diagnosed || State == PatientState.UnderTreatment))
                {
                    _active = procedure;
                    State = PatientState.UnderTreatment;
                    return true;
                }

                return false;
            }

            return false;
        }

        public void CompleteProcedure(IProcedureDef procedure)
        {
            if (_active != procedure) return;

            _active = null;

            if (TryResolveCandidate(_tests, procedure, out var completedTest))
            {
                _completedTests.Add(completedTest);
                RefreshDiagnosisState();
            }
            else if (TryResolveCandidate(_treatments, procedure, out _))
            {
                State = DiagnosisKnown ? PatientState.ReadyForDischarge : PatientState.Diagnosed;
            }
        }

        public void CancelActiveProcedure()
        {
            if (_active == null) return;

            _active = null;

            State = DiagnosisKnown ? PatientState.Diagnosed : PatientState.Waiting;

        }

        public bool TryDischarge()
        {
            if (State != PatientState.ReadyForDischarge) return false;

            State = PatientState.Discharged;
            return true;
        }

        private static bool TryResolveCandidate(IProcedureDef[] candidates, IProcedureDef procedure, out IProcedureDef match)
        {
            match = null;
            if (candidates == null || procedure == null) return false;

            foreach (var candidate in candidates)
            {
                if (candidate == null) continue;
                if (ReferenceEquals(candidate, procedure) || candidate.Equals(procedure))
                {
                    match = candidate;
                    return true;
                }
            }

            return false;
        }

        private static IProcedureDef[] FilterProcedures(IProcedureDef[] source)
        {
            if (source == null || source.Length == 0)
            {
                return Array.Empty<IProcedureDef>();
            }

            return source
                .Where(p => !ReferenceEquals(p, null))
                .Distinct()
                .ToArray();
        }

        private void RefreshDiagnosisState()
        {
            DiagnosisKnown = AreAllTestsCompleted;
            State = DiagnosisKnown ? PatientState.Diagnosed : PatientState.Waiting;
        }
    }
}

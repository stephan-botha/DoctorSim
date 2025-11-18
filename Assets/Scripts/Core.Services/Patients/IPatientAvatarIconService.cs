// MedMania.Core.Services
// IPatientAvatarIconService.cs
// Responsibility: Provide icon sprites for patient avatars rendered from avatar prefabs.

using UnityEngine;
using MedMania.Core.Domain.Patients;
using MedMania.Core.Services.Procedures;

namespace MedMania.Core.Services.Patients
{
    public interface IPatientAvatarIconService
    {
        void RegisterAvatar(IPatient patient, GameObject avatarPrefab);

        void ClearAvatar(IPatient patient);

        ProcedureIconView GetAvatarIcon(IPatient patient);
    }
}

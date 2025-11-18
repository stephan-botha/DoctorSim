// MedMania.Presentation.Input
// ProcedureStationRegistry.cs
// Responsibility: Central registry for procedure stations discoverable by input agents.

using System.Collections.Generic;

namespace MedMania.Presentation.Input.Staff
{
    /// <summary>
    /// Maintains a scene-level registry of procedure stations. View components register themselves,
    /// allowing input agents to remain unaware of concrete view implementations.
    /// </summary>
    public static class ProcedureStationRegistry
    {
        private static readonly List<IProcedureStation> s_Stations = new();

        public static IReadOnlyList<IProcedureStation> Stations => s_Stations;

        public static void Register(IProcedureStation station)
        {
            if (station == null || s_Stations.Contains(station))
            {
                return;
            }

            s_Stations.Add(station);
        }

        public static void Unregister(IProcedureStation station)
        {
            if (station == null)
            {
                return;
            }

            s_Stations.Remove(station);
        }
    }
}

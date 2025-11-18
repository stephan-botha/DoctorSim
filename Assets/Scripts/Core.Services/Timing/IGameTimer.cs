// MedMania.Core.Services
// IGameTimer.cs
using System;

namespace MedMania.Core.Services.Timing
{
    public interface IGameTimer
    {
        IDisposable Schedule(float seconds, Action onComplete);
    }
}

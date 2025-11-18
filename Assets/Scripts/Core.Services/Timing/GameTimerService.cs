// MedMania.Core.Services
// GameTimerService.cs
// Responsibility: Simple MonoBehaviour-backed timer implementation.

using System;
using UnityEngine;

namespace MedMania.Core.Services.Timing
{
    public sealed class GameTimerService : MonoBehaviour, IGameTimer
    {
        private sealed class TimerHandle : IDisposable
        {
            public float remaining;
            public Action onDone;
            public bool cancelled;
            public void Dispose() => cancelled = true;
        }

        private readonly System.Collections.Generic.List<TimerHandle> _handles = new();

        public IDisposable Schedule(float seconds, Action onComplete)
        {
            var h = new TimerHandle { remaining = Mathf.Max(0, seconds), onDone = onComplete };
            _handles.Add(h);
            return h;
        }

        private void Update()
        {
            for (int i = _handles.Count - 1; i >= 0; i--)
            {
                var h = _handles[i];
                if (h.cancelled) { _handles.RemoveAt(i); continue; }
                h.remaining -= Time.deltaTime;
                if (h.remaining <= 0f)
                {
                    _handles.RemoveAt(i);
                    h.onDone?.Invoke();
                }
            }
        }
    }
}

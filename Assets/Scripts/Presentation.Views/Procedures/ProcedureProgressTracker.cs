// MedMania.Presentation.Views
// ProcedureProgressTracker.cs

using UnityEngine;

namespace MedMania.Presentation.Views.Procedures
{
    public sealed class ProcedureProgressTracker
    {
        private float _duration;
        private float _startTime;

        public bool IsRunning { get; private set; }
        public bool HasDuration => _duration > 0f;

        public float InitialProgress => _duration <= 0f ? 1f : 0f;

        public void Begin(float durationSeconds)
        {
            _duration = Mathf.Max(0f, durationSeconds);
            _startTime = Time.time;
            IsRunning = true;
        }

        public float GetProgress()
        {
            if (!IsRunning)
            {
                return 0f;
            }

            if (_duration <= 0f)
            {
                return 1f;
            }

            var elapsed = Mathf.Max(0f, Time.time - _startTime);
            return Mathf.Clamp01(elapsed / _duration);
        }

        public void Stop()
        {
            IsRunning = false;
            _duration = 0f;
            _startTime = 0f;
        }
    }
}

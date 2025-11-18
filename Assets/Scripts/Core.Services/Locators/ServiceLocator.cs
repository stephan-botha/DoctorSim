// MedMania.Core.Services
// ServiceLocator.cs
using System;
using UnityEngine;

namespace MedMania.Core.Services
{
    public static class ServiceLocator
    {
        public static T Find<T>() where T : class
        {
            if (typeof(T).IsSubclassOf(typeof(Component)))
                return UnityEngine.Object.FindFirstObjectByType(typeof(T)) as T;

            var behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var behaviour in behaviours)
            {
                if (behaviour is T match)
                    return match;
            }

            throw new InvalidOperationException($"ServiceLocator could not find an active service implementing {typeof(T).FullName}.");
        }
    }
}

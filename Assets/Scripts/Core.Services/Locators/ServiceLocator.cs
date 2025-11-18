// MedMania.Core.Services
// ServiceLocator.cs
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MedMania.Core.Services
{
    public static class ServiceLocator
    {
        private static readonly Dictionary<System.Type, object> s_ServiceCache = new();
        private static readonly object s_CacheLock = new();

        public static void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                throw new System.ArgumentNullException(nameof(service));
            }

            lock (s_CacheLock)
            {
                s_ServiceCache[typeof(T)] = service;
            }
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            lock (s_CacheLock)
            {
                if (s_ServiceCache.TryGetValue(typeof(T), out var cached) && cached is T typedCached)
                {
                    service = typedCached;
                    return true;
                }
            }

            service = ResolveFromScene<T>();
            if (service == null)
            {
                return false;
            }

            lock (s_CacheLock)
            {
                s_ServiceCache[typeof(T)] = service;
            }

            return true;
        }

        public static T Find<T>() where T : class
        {
            if (TryGet(out T service))
            {
                return service;
            }

            throw new InvalidOperationException($"ServiceLocator could not find an active service implementing {typeof(T).FullName}.");
        }

        private static T ResolveFromScene<T>() where T : class
        {
            if (typeof(T).IsSubclassOf(typeof(Component)))
            {
                return UnityEngine.Object.FindFirstObjectByType(typeof(T)) as T;
            }

            var behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var behaviour in behaviours)
            {
                if (behaviour is T match)
                {
                    return match;
                }
            }

            return null;
        }
    }
}

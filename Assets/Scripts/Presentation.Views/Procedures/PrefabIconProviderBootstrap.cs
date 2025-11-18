// MedMania.Presentation.Views
// PrefabIconProviderBootstrap.cs
// Responsibility: Ensure a PrefabIconProvider is present at runtime.

using UnityEngine;

namespace MedMania.Presentation.Views.Procedures
{
    internal static class PrefabIconProviderBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureProvider()
        {
            if (Object.FindFirstObjectByType<PrefabIconProvider>() != null)
            {
                return;
            }

            var go = new GameObject(nameof(PrefabIconProvider));
            go.AddComponent<PrefabIconProvider>();
            Object.DontDestroyOnLoad(go);
        }
    }
}

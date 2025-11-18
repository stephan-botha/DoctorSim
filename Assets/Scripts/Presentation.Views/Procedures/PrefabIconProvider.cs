// MedMania.Presentation.Views
// PrefabIconProvider.cs
// Responsibility: Resolve sprites for procedures using ScriptableObject data and runtime renders.

using System.Collections.Generic;
using UnityEngine;
using MedMania.Core.Domain.Procedures;
using MedMania.Core.Services.Procedures;
using MedMania.Core.Data.ScriptableObjects;

namespace MedMania.Presentation.Views.Procedures
{
    [DefaultExecutionOrder(-500)]
    public sealed class PrefabIconProvider : MonoBehaviour, IProcedureIconProvider
    {
        private const int PreviewLayer = 30;

        [SerializeField] private Vector3 _cameraAngles = new(15f, -30f, 0f);
        [SerializeField] private Color _defaultBackground = Color.clear;
        [SerializeField] private Vector2Int _defaultRenderSize = new(256, 256);

        private readonly Dictionary<IProcedureDef, ProcedureIconView> _cache = new();
        private readonly Dictionary<PrefabIconCacheKey, ProcedureIconView> _prefabCache = new();
        private Transform _previewRoot;

        private void Awake()
        {
            EnsurePreviewRoot();
        }

        public ProcedureIconView GetIcon(IProcedureDef procedure)
        {
            if (procedure == null)
            {
                return ProcedureIconView.Empty;
            }

            if (_cache.TryGetValue(procedure, out var cached))
            {
                return cached;
            }

            var created = CreateIconView(procedure);
            _cache[procedure] = created;
            return created;
        }

        private ProcedureIconView CreateIconView(IProcedureDef procedure)
        {
            if (procedure is ProcedureSOBase procedureAsset)
            {
                if (procedureAsset.IconSprite != null)
                {
                    return new ProcedureIconView(procedureAsset.IconSprite, null);
                }

                if (procedureAsset.IconPrefab != null)
                {
                    var background = procedureAsset.IconRenderBackground;
                    if (Mathf.Approximately(background.a, 0f))
                    {
                        background = _defaultBackground;
                    }

                    var sprite = RenderPrefabIcon(
                        procedureAsset.IconPrefab,
                        procedureAsset.IconRenderTextureSize,
                        background,
                        procedureAsset.name);
                    if (sprite != null)
                    {
                        return new ProcedureIconView(sprite, null);
                    }
                }
            }

            return ProcedureIconView.Empty;
        }

        public ProcedureIconView GetPrefabIcon(GameObject prefab, Vector2Int size = default, Color? background = null, string labelOverride = null)
        {
            if (prefab == null)
            {
                return ProcedureIconView.Empty;
            }

            var resolvedSize = size;
            if (resolvedSize.x <= 0 || resolvedSize.y <= 0)
            {
                resolvedSize = _defaultRenderSize;
            }

            var resolvedBackground = background ?? _defaultBackground;
            var cacheKey = new PrefabIconCacheKey(prefab, resolvedSize, resolvedBackground);

            if (_prefabCache.TryGetValue(cacheKey, out var cached))
            {
                if (!string.IsNullOrEmpty(labelOverride) && labelOverride != cached.LabelOverride)
                {
                    return new ProcedureIconView(cached.Sprite, labelOverride);
                }

                return cached;
            }

            var sprite = RenderPrefabIcon(prefab, resolvedSize, resolvedBackground, prefab.name);
            if (sprite == null)
            {
                return ProcedureIconView.Empty;
            }

            var view = new ProcedureIconView(sprite, labelOverride);
            _prefabCache[cacheKey] = view;
            return view;
        }

        private Sprite RenderPrefabIcon(GameObject prefab, Vector2Int size, Color background, string spriteName)
        {
            if (prefab == null)
            {
                return null;
            }

            var previewRoot = EnsurePreviewRoot();
            var instance = Instantiate(prefab, previewRoot);
            instance.hideFlags = HideFlags.HideAndDontSave;
            SetLayerRecursively(instance, PreviewLayer);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            instance.SetActive(true);

            var bounds = CalculateRendererBounds(instance);
            var localCenter = previewRoot.InverseTransformPoint(bounds.center);
            var width = Mathf.Max(1, size.x);
            var height = Mathf.Max(1, size.y);

            var cameraGo = new GameObject("ProcedureIconCamera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            cameraGo.transform.SetParent(previewRoot, false);
            SetLayerRecursively(cameraGo, PreviewLayer);

            var cam = cameraGo.AddComponent<Camera>();
            cam.enabled = false;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = background;
            cam.cullingMask = 1 << PreviewLayer;
            cam.orthographic = true;

            var maxExtent = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
            if (maxExtent <= 0f)
            {
                maxExtent = 0.5f;
            }
            cam.orthographicSize = maxExtent;
            var cameraDirection = Quaternion.Euler(_cameraAngles) * Vector3.back;
            var distance = Mathf.Max(maxExtent * 2f, 1f);
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = Mathf.Max(distance * 4f, 10f);
            var cameraPosition = localCenter - cameraDirection * distance;
            cameraGo.transform.localPosition = cameraPosition;
            cameraGo.transform.localRotation = Quaternion.LookRotation(localCenter - cameraPosition);

            var lightGo = new GameObject("ProcedureIconLight")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            lightGo.transform.SetParent(previewRoot, false);
            var lightDirection = Quaternion.Euler(_cameraAngles + new Vector3(-15f, 20f, 0f)) * Vector3.back;
            var lightDistance = Mathf.Max(maxExtent * 2.5f, 1.5f);
            var lightPosition = localCenter - lightDirection * lightDistance;
            lightGo.transform.localPosition = lightPosition;
            lightGo.transform.localRotation = Quaternion.LookRotation(localCenter - lightPosition);
            SetLayerRecursively(lightGo, PreviewLayer);

            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.cullingMask = 1 << PreviewLayer;

            var renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            cam.targetTexture = renderTexture;

            var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = renderTexture;
                cam.Render();
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                texture.Apply();

                var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
                sprite.name = $"{spriteName}_Icon";
                return sprite;
            }
            finally
            {
                RenderTexture.active = previous;
                cam.targetTexture = null;
                ReleaseTemporary(renderTexture);
                DestroyTemporary(cameraGo);
                DestroyTemporary(lightGo);
                DestroyTemporary(instance);
            }
        }

        private Transform EnsurePreviewRoot()
        {
            if (_previewRoot != null)
            {
                return _previewRoot;
            }

            var root = new GameObject("ProcedureIconPreviewRoot")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(10000f, 10000f, 10000f);
            root.layer = PreviewLayer;
            _previewRoot = root.transform;
            return _previewRoot;
        }

        private static Bounds CalculateRendererBounds(GameObject instance)
        {
            var renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                return new Bounds(instance.transform.position, Vector3.one);
            }

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }

            return bounds;
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            if (root == null)
            {
                return;
            }

            root.layer = layer;
            var transform = root.transform;
            for (int i = 0; i < transform.childCount; i++)
            {
                SetLayerRecursively(transform.GetChild(i).gameObject, layer);
            }
        }

        private static void DestroyTemporary(Object obj)
        {
            if (obj == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                if (obj is GameObject go)
                {
                    go.SetActive(false);
                }
                else if (obj is Component component && component != null)
                {
                    component.gameObject.SetActive(false);
                }
                Destroy(obj);
            }
            else
            {
                DestroyImmediate(obj);
            }
        }

        private static void ReleaseTemporary(RenderTexture renderTexture)
        {
            if (renderTexture == null)
            {
                return;
            }

            renderTexture.Release();
            DestroyTemporary(renderTexture);
        }

        private readonly struct PrefabIconCacheKey : System.IEquatable<PrefabIconCacheKey>
        {
            public PrefabIconCacheKey(GameObject prefab, Vector2Int size, Color background)
            {
                Prefab = prefab;
                Size = size;
                Background = background;
            }

            public GameObject Prefab { get; }
            public Vector2Int Size { get; }
            public Color Background { get; }

            public bool Equals(PrefabIconCacheKey other)
            {
                return ReferenceEquals(Prefab, other.Prefab)
                       && Size.Equals(other.Size)
                       && Background.Equals(other.Background);
            }

            public override bool Equals(object obj)
            {
                return obj is PrefabIconCacheKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = Prefab != null ? Prefab.GetHashCode() : 0;
                    hashCode = (hashCode * 397) ^ Size.GetHashCode();
                    hashCode = (hashCode * 397) ^ Background.GetHashCode();
                    return hashCode;
                }
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;

namespace Imphenzia.CrispolyCharactersMini
{
    public class Spawner : MonoBehaviour
    {
        [SerializeField] private GameObject characterPrefab = null;
        [SerializeField] private Mesh[] characterMeshes = null;

        private float spacing = 2f;

        void Start()
        {
            var meshes = characterMeshes.ToList();
            meshes = meshes.OrderBy(i => System.Guid.NewGuid()).ToList();

            int count = characterMeshes.Length;
            int sqrt = Mathf.FloorToInt(Mathf.Sqrt(count));
            int x = 0;
            int z = 0;

            for (int i = 0; i < count; i++)
            {
                var go = Instantiate(characterPrefab, transform);
                go.transform.localPosition = new Vector3((x - sqrt / 2) * spacing + (i % 2 == 0 ? 0 : 1f), 0, (z - sqrt / 2) * spacing);
                go.transform.rotation = Quaternion.identity;
                var skinnedMeshRenderer = go.GetComponentInChildren<SkinnedMeshRenderer>();
                skinnedMeshRenderer.sharedMesh = meshes[i];
                z++;
                if (z > sqrt)
                {
                    z = 0;
                    x++;
                }

            }
        }
    }

}
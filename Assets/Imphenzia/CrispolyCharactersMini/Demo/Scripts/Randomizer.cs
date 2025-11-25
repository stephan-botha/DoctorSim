using UnityEngine;

namespace Imphenzia.CrispolyCharactersMini
{
    public class Randomizer : MonoBehaviour
    {
        [SerializeField] SkinnedMeshRenderer[] renderers = null;
        [SerializeField] Mesh[] meshes = null;

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                Randomize();
            }
        }

        public void Randomize()
        {
            foreach (var renderer in renderers)
            {
                renderer.sharedMesh = meshes[Random.Range(0, meshes.Length)];
            }
        }
    }
}
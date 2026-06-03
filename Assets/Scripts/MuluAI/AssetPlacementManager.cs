using System.Collections.Generic;
using UnityEngine;

namespace MuluAI
{
    public class AssetPlacementManager : MonoBehaviour
    {
        [Header("Hierarchy")]
        [SerializeField] private Transform spawnedAssetRoot;
        [SerializeField] private string spawnedTag = "MuluSpawned";

        [Header("Materials")]
        [SerializeField] private Material defaultMaterial;
        [SerializeField] private bool addMeshCollider;

        private readonly Dictionary<string, Mesh> downloadedMeshesByFileId = new Dictionary<string, Mesh>();
        private readonly Dictionary<string, GameObject> registeredPrefabsByFileId = new Dictionary<string, GameObject>();
        private readonly List<GameObject> spawnedObjects = new List<GameObject>();

        public void RegisterDownloadedModel(DownloadedModel downloadedModel)
        {
            if (downloadedModel == null || downloadedModel.Item == null || downloadedModel.RuntimeMesh == null)
            {
                return;
            }

            downloadedMeshesByFileId[downloadedModel.Item.file_id] = downloadedModel.RuntimeMesh;
        }

        public void RegisterPrefab(string fileId, GameObject prefab)
        {
            if (string.IsNullOrWhiteSpace(fileId) || prefab == null)
            {
                return;
            }

            registeredPrefabsByFileId[fileId] = prefab;
        }

        public void ClearLevel()
        {
            for (int i = spawnedObjects.Count - 1; i >= 0; i--)
            {
                DestroyIfExists(spawnedObjects[i]);
            }

            spawnedObjects.Clear();

            Transform root = ResolveSpawnRoot();
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                DestroyIfExists(root.GetChild(i).gameObject);
            }

            if (!string.IsNullOrWhiteSpace(spawnedTag))
            {
                try
                {
                    GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(spawnedTag);
                    for (int i = 0; i < taggedObjects.Length; i++)
                    {
                        DestroyIfExists(taggedObjects[i]);
                    }
                }
                catch
                {
                    Debug.LogWarning("Tag '" + spawnedTag + "' is not defined in Project Settings. ClearLevel skipped tag lookup.");
                }
            }
        }

        public GameObject SpawnAsset(SpawnItem item)
        {
            if (item == null)
            {
                Debug.LogWarning("Cannot spawn a null SpawnItem.");
                return null;
            }

            GameObject spawnedObject = CreateObjectForItem(item);
            if (spawnedObject == null)
            {
                Debug.LogWarning("No downloaded mesh or registered prefab found for file_id: " + item.file_id);
                return null;
            }

            spawnedObject.name = string.IsNullOrWhiteSpace(item.name) ? item.instance_id : item.name;
            spawnedObject.transform.SetParent(ResolveSpawnRoot(), true);
            spawnedObject.transform.position = item.position != null ? item.position.ToVector3() : Vector3.zero;
            spawnedObject.transform.rotation = Quaternion.Euler(item.rotation != null ? item.rotation.ToVector3() : Vector3.zero);
            spawnedObject.transform.localScale = item.scale != null ? item.scale.ToVector3() : Vector3.one;

            ApplyTagIfAvailable(spawnedObject);
            spawnedObjects.Add(spawnedObject);
            return spawnedObject;
        }

        public void ApplyTexture(GameObject spawnedObject, Texture2D texture)
        {
            if (spawnedObject == null || texture == null)
            {
                return;
            }

            Renderer[] renderers = spawnedObject.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Material material = renderers[i].material;
                if (material == null)
                {
                    material = defaultMaterial != null ? new Material(defaultMaterial) : new Material(Shader.Find("Standard"));
                    renderers[i].material = material;
                }

                material.mainTexture = texture;
            }
        }

        private GameObject CreateObjectForItem(SpawnItem item)
        {
            GameObject prefab;
            if (!string.IsNullOrWhiteSpace(item.file_id) && registeredPrefabsByFileId.TryGetValue(item.file_id, out prefab) && prefab != null)
            {
                return Instantiate(prefab);
            }

            Mesh mesh;
            if (string.IsNullOrWhiteSpace(item.file_id) || !downloadedMeshesByFileId.TryGetValue(item.file_id, out mesh) || mesh == null)
            {
                return null;
            }

            GameObject spawnedObject = new GameObject(string.IsNullOrWhiteSpace(item.name) ? "Mulu Spawned Asset" : item.name);
            MeshFilter meshFilter = spawnedObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = spawnedObject.AddComponent<MeshRenderer>();
            meshFilter.sharedMesh = mesh;
            meshRenderer.sharedMaterial = defaultMaterial != null ? new Material(defaultMaterial) : new Material(Shader.Find("Standard"));

            if (addMeshCollider)
            {
                MeshCollider meshCollider = spawnedObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh;
            }

            return spawnedObject;
        }

        private Transform ResolveSpawnRoot()
        {
            if (spawnedAssetRoot == null)
            {
                spawnedAssetRoot = transform;
            }

            return spawnedAssetRoot;
        }

        private void ApplyTagIfAvailable(GameObject spawnedObject)
        {
            if (string.IsNullOrWhiteSpace(spawnedTag))
            {
                return;
            }

            try
            {
                spawnedObject.tag = spawnedTag;
            }
            catch
            {
                Debug.LogWarning("Tag '" + spawnedTag + "' is not defined in Project Settings. Spawn tracking still works through the manager list.");
            }
        }

        private static void DestroyIfExists(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}

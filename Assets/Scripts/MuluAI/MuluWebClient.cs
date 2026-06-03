using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace MuluAI
{
    [Serializable]
    public class Vec3Data
    {
        public float x;
        public float y;
        public float z;

        public Vec3Data() { }

        public Vec3Data(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(x, y, z);
        }

        public static Vec3Data FromVector3(Vector3 value)
        {
            return new Vec3Data(value.x, value.y, value.z);
        }
    }

    [Serializable]
    public class PlayerStatus
    {
        public float health;
        public Vec3Data position;
    }

    [Serializable]
    public class BoundaryData
    {
        public float min_x;
        public float min_y;
        public float min_z;
        public float max_x;
        public float max_y;
        public float max_z;

        public BoundaryData() { }

        public BoundaryData(Vector3 min, Vector3 max)
        {
            min_x = min.x;
            min_y = min.y;
            min_z = min.z;
            max_x = max.x;
            max_y = max.y;
            max_z = max.z;
        }
    }

    [Serializable]
    public class UnityBuildRequest
    {
        public string world_id;
        public string user_id;
        public string prompt;
        public PlayerStatus player_status;
        public BoundaryData boundary;
    }

    [Serializable]
    public class JoystickButton
    {
        public string id;
        public string label;
        public string action;
    }

    [Serializable]
    public class ControlsConfig
    {
        public string scheme_type;
        public bool joystick_enabled;
        public List<JoystickButton> buttons = new List<JoystickButton>();
    }

    [Serializable]
    public class TextureRef
    {
        public string file_id;
        public string bucket_id;
        public string filename;
    }

    [Serializable]
    public class MetadataEntry
    {
        public string key;
        public string value;
    }

    [Serializable]
    public class SpawnItem
    {
        public string instance_id;
        public string name;
        public string file_id;
        public string bucket_id;
        public string filename;
        public Vec3Data position;
        public Vec3Data rotation;
        public Vec3Data scale;
        public TextureRef texture;
        public List<MetadataEntry> metadata = new List<MetadataEntry>();
    }

    [Serializable]
    public class EnvironmentConfig
    {
        public string time_of_day;
        public string weather;
        public TextureRef terrain_texture;
    }

    [Serializable]
    public class UnityBuildResponse
    {
        public string status;
        public string world_id;
        public string prompt;
        public string narrative;
        public ControlsConfig controls;
        public List<SpawnItem> spawns = new List<SpawnItem>();
        public EnvironmentConfig environment;
    }

    [Serializable]
    public class BackendConfigResponse
    {
        public string status;
        public BackendConfig config;
    }

    [Serializable]
    public class BackendConfig
    {
        public string APPWRITE_ENDPOINT;
        public string APPWRITE_PROJECT_ID;
    }

    public sealed class DownloadedModel
    {
        public SpawnItem Item { get; private set; }
        public string LocalPath { get; private set; }
        public byte[] Bytes { get; private set; }
        public Mesh RuntimeMesh { get; private set; }

        public DownloadedModel(SpawnItem item, string localPath, byte[] bytes, Mesh runtimeMesh)
        {
            Item = item;
            LocalPath = localPath;
            Bytes = bytes;
            RuntimeMesh = runtimeMesh;
        }
    }

    public class MuluWebClient : MonoBehaviour
    {
        [Header("FastAPI")]
        [SerializeField] private string unityBuildEndpoint = "http://localhost:8000/api/agent/unity-build";
        [SerializeField] private string backendBaseUrl = "http://localhost:8000";
        [SerializeField] private int requestTimeoutSeconds = 30;

        [Header("Appwrite Storage")]
        [SerializeField] private string appwriteEndpoint = "https://sfo.cloud.appwrite.io/v1";
        [SerializeField] private string appwriteProjectId = "6a1b314900276031f68d";
        [SerializeField] private string appwriteApiKey = "";
        [SerializeField] private bool usePreviewEndpointForTextures;

        public string UnityBuildEndpoint
        {
            get { return unityBuildEndpoint; }
            set { unityBuildEndpoint = value; }
        }

        public IEnumerator FetchBackendConfig(Action<BackendConfigResponse> onCompleted, Action<string> onFailed = null)
        {
            string url = backendBaseUrl.TrimEnd('/') + "/api/admin/config";
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                webRequest.timeout = requestTimeoutSeconds;
                webRequest.SetRequestHeader("Accept", "application/json");

                yield return webRequest.SendWebRequest();

                if (HasRequestError(webRequest))
                {
                    onFailed.SafeInvoke(BuildErrorMessage(webRequest));
                    yield break;
                }

                BackendConfigResponse response;
                try
                {
                    response = JsonUtility.FromJson<BackendConfigResponse>(webRequest.downloadHandler.text);
                }
                catch (Exception exception)
                {
                    onFailed.SafeInvoke("Failed to parse backend config response: " + exception.Message);
                    yield break;
                }

                if (response != null && response.config != null)
                {
                    if (!string.IsNullOrWhiteSpace(response.config.APPWRITE_ENDPOINT))
                    {
                        appwriteEndpoint = response.config.APPWRITE_ENDPOINT;
                    }

                    if (!string.IsNullOrWhiteSpace(response.config.APPWRITE_PROJECT_ID))
                    {
                        appwriteProjectId = response.config.APPWRITE_PROJECT_ID;
                    }
                }

                onCompleted.SafeInvoke(response);
            }
        }

        public IEnumerator SendUnityBuildRequest(
            UnityBuildRequest request,
            Action<UnityBuildResponse> onCompleted,
            Action<string> onFailed = null)
        {
            if (request == null)
            {
                onFailed.SafeInvoke("UnityBuildRequest is null.");
                yield break;
            }

            string json = JsonUtility.ToJson(request);
            using (UnityWebRequest webRequest = new UnityWebRequest(unityBuildEndpoint, UnityWebRequest.kHttpVerbPOST))
            {
                byte[] body = System.Text.Encoding.UTF8.GetBytes(json);
                webRequest.uploadHandler = new UploadHandlerRaw(body);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.timeout = requestTimeoutSeconds;
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.SetRequestHeader("Accept", "application/json");

                yield return webRequest.SendWebRequest();

                if (HasRequestError(webRequest))
                {
                    onFailed.SafeInvoke(BuildErrorMessage(webRequest));
                    yield break;
                }

                UnityBuildResponse response;
                try
                {
                    response = JsonUtility.FromJson<UnityBuildResponse>(webRequest.downloadHandler.text);
                }
                catch (Exception exception)
                {
                    onFailed.SafeInvoke("Failed to parse Unity build response: " + exception.Message);
                    yield break;
                }

                onCompleted.SafeInvoke(response);
            }
        }

        public IEnumerator DownloadModelFile(
            SpawnItem item,
            Action<DownloadedModel> onCompleted,
            Action<string> onFailed = null)
        {
            if (item == null)
            {
                onFailed.SafeInvoke("SpawnItem is null.");
                yield break;
            }

            if (string.IsNullOrWhiteSpace(item.file_id) || string.IsNullOrWhiteSpace(item.bucket_id))
            {
                onFailed.SafeInvoke("SpawnItem is missing file_id or bucket_id.");
                yield break;
            }

            string url = BuildStorageFileUrl(item.bucket_id, item.file_id, false);
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                ApplyAppwriteHeaders(webRequest);
                webRequest.timeout = requestTimeoutSeconds;

                yield return webRequest.SendWebRequest();

                if (HasRequestError(webRequest))
                {
                    onFailed.SafeInvoke(BuildErrorMessage(webRequest));
                    yield break;
                }

                byte[] bytes = webRequest.downloadHandler.data;
                string localPath = SaveDownloadedBytes(item.bucket_id, item.file_id, item.filename, bytes);
                Mesh mesh = TryCreateObjMesh(item.filename, bytes);

                onCompleted.SafeInvoke(new DownloadedModel(item, localPath, bytes, mesh));
            }
        }

        public IEnumerator DownloadTextureFile(
            TextureRef textureRef,
            Action<Texture2D> onCompleted,
            Action<string> onFailed = null)
        {
            if (textureRef == null)
            {
                onFailed.SafeInvoke("TextureRef is null.");
                yield break;
            }

            if (string.IsNullOrWhiteSpace(textureRef.file_id) || string.IsNullOrWhiteSpace(textureRef.bucket_id))
            {
                onFailed.SafeInvoke("TextureRef is missing file_id or bucket_id.");
                yield break;
            }

            string url = BuildStorageFileUrl(textureRef.bucket_id, textureRef.file_id, usePreviewEndpointForTextures);
            using (UnityWebRequest webRequest = UnityWebRequestTexture.GetTexture(url, true))
            {
                ApplyAppwriteHeaders(webRequest);
                webRequest.timeout = requestTimeoutSeconds;

                yield return webRequest.SendWebRequest();

                if (HasRequestError(webRequest))
                {
                    onFailed.SafeInvoke(BuildErrorMessage(webRequest));
                    yield break;
                }

                Texture2D texture = DownloadHandlerTexture.GetContent(webRequest);
                texture.name = string.IsNullOrWhiteSpace(textureRef.filename) ? textureRef.file_id : textureRef.filename;
                onCompleted.SafeInvoke(texture);
            }
        }

        private string BuildStorageFileUrl(string bucketId, string fileId, bool preview)
        {
            string normalizedEndpoint = appwriteEndpoint.TrimEnd('/');
            string mode = preview ? "preview" : "view";
            string url = string.Format("{0}/storage/buckets/{1}/files/{2}/{3}", normalizedEndpoint, bucketId, fileId, mode);

            if (!string.IsNullOrWhiteSpace(appwriteProjectId))
            {
                url += "?project=" + UnityWebRequest.EscapeURL(appwriteProjectId);
            }

            return url;
        }

        private void ApplyAppwriteHeaders(UnityWebRequest webRequest)
        {
            if (!string.IsNullOrWhiteSpace(appwriteProjectId))
            {
                webRequest.SetRequestHeader("X-Appwrite-Project", appwriteProjectId);
            }

            if (!string.IsNullOrWhiteSpace(appwriteApiKey))
            {
                webRequest.SetRequestHeader("X-Appwrite-Key", appwriteApiKey);
            }
        }

        private string SaveDownloadedBytes(string bucketId, string fileId, string filename, byte[] bytes)
        {
            string safeName = string.IsNullOrWhiteSpace(filename) ? fileId : filename;
            foreach (char invalidChar in Path.GetInvalidFileNameChars())
            {
                safeName = safeName.Replace(invalidChar, '_');
            }

            string directory = Path.Combine(Application.persistentDataPath, "MuluAI", bucketId);
            Directory.CreateDirectory(directory);

            string path = Path.Combine(directory, safeName);
            File.WriteAllBytes(path, bytes);
            return path;
        }

        private static bool HasRequestError(UnityWebRequest request)
        {
#if UNITY_2020_2_OR_NEWER
            return request.result == UnityWebRequest.Result.ConnectionError ||
                   request.result == UnityWebRequest.Result.ProtocolError ||
                   request.result == UnityWebRequest.Result.DataProcessingError;
#else
            return request.isNetworkError || request.isHttpError;
#endif
        }

        private static string BuildErrorMessage(UnityWebRequest request)
        {
            return string.Format(
                "HTTP {0} {1}: {2}",
                request.responseCode,
                request.url,
                string.IsNullOrEmpty(request.error) ? request.downloadHandler.text : request.error);
        }

        private static Mesh TryCreateObjMesh(string filename, byte[] bytes)
        {
            if (string.IsNullOrWhiteSpace(filename) ||
                !filename.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string objText = System.Text.Encoding.UTF8.GetString(bytes);
            return ObjMeshParser.Parse(objText, Path.GetFileNameWithoutExtension(filename));
        }
    }

    internal static class ActionExtensions
    {
        public static void SafeInvoke<T>(this Action<T> action, T value)
        {
            if (action != null)
            {
                action(value);
            }
        }
    }

    internal static class ObjMeshParser
    {
        public static Mesh Parse(string objText, string meshName)
        {
            List<Vector3> sourceVertices = new List<Vector3>();
            List<Vector2> sourceUvs = new List<Vector2>();
            List<Vector3> sourceNormals = new List<Vector3>();
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vector3> normals = new List<Vector3>();
            List<int> triangles = new List<int>();
            Dictionary<string, int> indexLookup = new Dictionary<string, int>();

            string[] lines = objText.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0)
                {
                    continue;
                }

                switch (parts[0])
                {
                    case "v":
                        if (parts.Length >= 4)
                        {
                            sourceVertices.Add(new Vector3(ParseFloat(parts[1]), ParseFloat(parts[2]), ParseFloat(parts[3])));
                        }
                        break;
                    case "vt":
                        if (parts.Length >= 3)
                        {
                            sourceUvs.Add(new Vector2(ParseFloat(parts[1]), ParseFloat(parts[2])));
                        }
                        break;
                    case "vn":
                        if (parts.Length >= 4)
                        {
                            sourceNormals.Add(new Vector3(ParseFloat(parts[1]), ParseFloat(parts[2]), ParseFloat(parts[3])));
                        }
                        break;
                    case "f":
                        AddFace(parts, sourceVertices, sourceUvs, sourceNormals, vertices, uvs, normals, triangles, indexLookup);
                        break;
                }
            }

            Mesh mesh = new Mesh();
            mesh.name = string.IsNullOrWhiteSpace(meshName) ? "Runtime OBJ Mesh" : meshName;

            if (vertices.Count > 65535)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);

            if (uvs.Count == vertices.Count)
            {
                mesh.SetUVs(0, uvs);
            }

            if (normals.Count == vertices.Count)
            {
                mesh.SetNormals(normals);
            }
            else
            {
                mesh.RecalculateNormals();
            }

            mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddFace(
            string[] parts,
            List<Vector3> sourceVertices,
            List<Vector2> sourceUvs,
            List<Vector3> sourceNormals,
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<Vector3> normals,
            List<int> triangles,
            Dictionary<string, int> indexLookup)
        {
            if (parts.Length < 4)
            {
                return;
            }

            int[] faceIndices = new int[parts.Length - 1];
            for (int i = 1; i < parts.Length; i++)
            {
                faceIndices[i - 1] = GetOrCreateIndex(parts[i], sourceVertices, sourceUvs, sourceNormals, vertices, uvs, normals, indexLookup);
            }

            for (int i = 1; i < faceIndices.Length - 1; i++)
            {
                triangles.Add(faceIndices[0]);
                triangles.Add(faceIndices[i]);
                triangles.Add(faceIndices[i + 1]);
            }
        }

        private static int GetOrCreateIndex(
            string token,
            List<Vector3> sourceVertices,
            List<Vector2> sourceUvs,
            List<Vector3> sourceNormals,
            List<Vector3> vertices,
            List<Vector2> uvs,
            List<Vector3> normals,
            Dictionary<string, int> indexLookup)
        {
            int existingIndex;
            if (indexLookup.TryGetValue(token, out existingIndex))
            {
                return existingIndex;
            }

            string[] indices = token.Split('/');
            int vertexIndex = ResolveObjIndex(indices[0], sourceVertices.Count);
            Vector3 vertex = vertexIndex >= 0 && vertexIndex < sourceVertices.Count ? sourceVertices[vertexIndex] : Vector3.zero;

            vertices.Add(vertex);

            if (indices.Length > 1 && !string.IsNullOrEmpty(indices[1]))
            {
                int uvIndex = ResolveObjIndex(indices[1], sourceUvs.Count);
                uvs.Add(uvIndex >= 0 && uvIndex < sourceUvs.Count ? sourceUvs[uvIndex] : Vector2.zero);
            }
            else
            {
                uvs.Add(Vector2.zero);
            }

            if (indices.Length > 2 && !string.IsNullOrEmpty(indices[2]))
            {
                int normalIndex = ResolveObjIndex(indices[2], sourceNormals.Count);
                normals.Add(normalIndex >= 0 && normalIndex < sourceNormals.Count ? sourceNormals[normalIndex] : Vector3.up);
            }
            else
            {
                normals.Add(Vector3.up);
            }

            int newIndex = vertices.Count - 1;
            indexLookup[token] = newIndex;
            return newIndex;
        }

        private static int ResolveObjIndex(string token, int count)
        {
            int index;
            if (!int.TryParse(token, out index))
            {
                return -1;
            }

            return index < 0 ? count + index : index - 1;
        }

        private static float ParseFloat(string value)
        {
            return float.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}

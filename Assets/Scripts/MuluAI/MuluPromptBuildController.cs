using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MuluAI
{
    public class MuluPromptBuildController : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private MuluWebClient webClient;
        [SerializeField] private AssetPlacementManager assetPlacementManager;
        [SerializeField] private DynamicHUDController hudController;
        [SerializeField] private EnvironmentController environmentController;
        [SerializeField] private Transform playerTransform;

        [Header("Prompt UI")]
        [SerializeField] private InputField promptInputField;
        [SerializeField] private Button submitButton;
        [SerializeField] private Text narrativeText;
        [SerializeField] private Text statusText;
        [SerializeField] private MuluChatLogView chatLogView;

        [Header("Request Defaults")]
        [SerializeField] private string worldId = "default-world";
        [SerializeField] private string userId = "local-player";
        [SerializeField] private float playerHealth = 100f;
        [SerializeField] private Vector3 boundaryMin = new Vector3(-100f, 0f, -100f);
        [SerializeField] private Vector3 boundaryMax = new Vector3(100f, 100f, 100f);
        [SerializeField] private bool clearLevelBeforeBuild = true;
        [SerializeField] private bool fetchBackendConfigBeforeBuild = true;

        [Header("Events")]
        public UnityEvent<UnityBuildResponse> BuildCompleted = new UnityEvent<UnityBuildResponse>();
        public UnityEvent<string> BuildFailed = new UnityEvent<string>();

        private bool isBuilding;

        private void Awake()
        {
            if (webClient == null)
            {
                webClient = FindAnyObjectByType<MuluWebClient>();
            }

            if (assetPlacementManager == null)
            {
                assetPlacementManager = FindAnyObjectByType<AssetPlacementManager>();
            }

            if (hudController == null)
            {
                hudController = FindAnyObjectByType<DynamicHUDController>();
            }

            if (environmentController == null)
            {
                environmentController = FindAnyObjectByType<EnvironmentController>();
            }

            if (submitButton != null)
            {
                submitButton.onClick.RemoveListener(SubmitPromptFromInput);
                submitButton.onClick.AddListener(SubmitPromptFromInput);
            }
        }

        public void SubmitPromptFromInput()
        {
            string prompt = promptInputField != null ? promptInputField.text : string.Empty;
            SubmitPrompt(prompt);
        }

        public void SubmitPrompt(string prompt)
        {
            if (isBuilding)
            {
                SetStatus("Build already running.");
                return;
            }

            if (string.IsNullOrWhiteSpace(prompt))
            {
                SetStatus("Enter a prompt first.");
                return;
            }

            if (chatLogView != null)
            {
                chatLogView.AddUserMessage(prompt);
            }

            if (promptInputField != null)
            {
                promptInputField.text = string.Empty;
            }

            StartCoroutine(BuildFromPrompt(prompt));
        }

        public IEnumerator BuildFromPrompt(string prompt)
        {
            isBuilding = true;
            SetSubmitInteractable(false);
            SetStatus("Sending prompt to AI backend...");

            if (webClient == null)
            {
                FinishWithFailure("MuluWebClient is not assigned in the scene.");
                yield break;
            }

            if (fetchBackendConfigBeforeBuild)
            {
                string configFailure = null;
                yield return webClient.FetchBackendConfig(
                    _ => SetStatus("Backend config loaded. Building scene..."),
                    error => configFailure = error);

                if (!string.IsNullOrEmpty(configFailure))
                {
                    Debug.LogWarning("Could not fetch backend config. Continuing with Inspector settings. " + configFailure);
                }
            }

            UnityBuildRequest request = CreateRequest(prompt);
            UnityBuildResponse response = null;
            string failure = null;

            yield return webClient.SendUnityBuildRequest(
                request,
                value => response = value,
                error => failure = error);

            if (!string.IsNullOrEmpty(failure))
            {
                if (chatLogView != null)
                {
                    chatLogView.AddSystemMessage("Backend error: " + failure);
                }

                FinishWithFailure(failure);
                yield break;
            }

            if (response == null)
            {
                FinishWithFailure("Backend returned an empty response.");
                yield break;
            }

            yield return ApplyBuildResponse(response);

            SetStatus("Build complete.");
            if (chatLogView != null && !string.IsNullOrWhiteSpace(response.narrative))
            {
                chatLogView.AddAiMessage(response.narrative);
            }

            BuildCompleted.Invoke(response);
            isBuilding = false;
            SetSubmitInteractable(true);
        }

        private UnityBuildRequest CreateRequest(string prompt)
        {
            Vector3 playerPosition = playerTransform != null ? playerTransform.position : Vector3.zero;

            return new UnityBuildRequest
            {
                world_id = worldId,
                user_id = userId,
                prompt = prompt,
                player_status = new PlayerStatus
                {
                    health = playerHealth,
                    position = Vec3Data.FromVector3(playerPosition)
                },
                boundary = new BoundaryData(boundaryMin, boundaryMax)
            };
        }

        private IEnumerator ApplyBuildResponse(UnityBuildResponse response)
        {
            if (clearLevelBeforeBuild && assetPlacementManager != null)
            {
                assetPlacementManager.ClearLevel();
            }

            if (hudController != null)
            {
                hudController.ConfigureGamepad(response.controls);
            }

            if (environmentController != null)
            {
                environmentController.ApplyEnvironment(response.environment);
            }

            if (narrativeText != null)
            {
                narrativeText.text = response.narrative;
            }

            if (response.spawns == null || assetPlacementManager == null)
            {
                yield break;
            }

            for (int i = 0; i < response.spawns.Count; i++)
            {
                SpawnItem item = response.spawns[i];
                SetStatus("Downloading asset " + (i + 1) + " of " + response.spawns.Count + "...");

                DownloadedModel downloadedModel = null;
                string modelFailure = null;

                yield return webClient.DownloadModelFile(
                    item,
                    value => downloadedModel = value,
                    error => modelFailure = error);

                if (!string.IsNullOrEmpty(modelFailure))
                {
                    Debug.LogWarning(modelFailure);
                    continue;
                }

                assetPlacementManager.RegisterDownloadedModel(downloadedModel);
                GameObject spawnedObject = assetPlacementManager.SpawnAsset(item);

                if (spawnedObject != null && item.texture != null && !string.IsNullOrWhiteSpace(item.texture.file_id))
                {
                    Texture2D texture = null;
                    string textureFailure = null;

                    yield return webClient.DownloadTextureFile(
                        item.texture,
                        value => texture = value,
                        error => textureFailure = error);

                    if (!string.IsNullOrEmpty(textureFailure))
                    {
                        Debug.LogWarning(textureFailure);
                    }
                    else
                    {
                        assetPlacementManager.ApplyTexture(spawnedObject, texture);
                    }
                }
            }
        }

        private void FinishWithFailure(string message)
        {
            Debug.LogError(message);
            SetStatus(message);
            BuildFailed.Invoke(message);
            isBuilding = false;
            SetSubmitInteractable(true);
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }

            Debug.Log("Mulu build: " + message);
        }

        private void SetSubmitInteractable(bool interactable)
        {
            if (submitButton != null)
            {
                submitButton.interactable = interactable;
            }
        }
    }
}

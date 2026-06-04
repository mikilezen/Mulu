using UnityEngine;

namespace MuluAI
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("Mulu AI/Character Configurator")]
    public class MuluCharacterConfigurator : MonoBehaviour
    {
        [Header("Character Model Settings")]
        public Transform characterTransform;
        public float characterScale = 1f;
        public Vector3 characterPositionOffset = Vector3.zero;
        public bool autoFitCharacterScale = true;
        public float targetCharacterHeight = 1.45f;
        public float floorClearance = 0.03f;

        private bool cachedBaseCharacterScale;
        private Vector3 baseCharacterLocalScale = Vector3.one;

        [Header("Platform Settings")]
        public Renderer platformRenderer;
        public float platformDiameter = 6f;
        public Color platformColor = new Color(0.08f, 0.1f, 0.15f, 1f);
        [Range(0f, 1f)] public float platformMetallic = 0.8f;
        [Range(0f, 1f)] public float platformSmoothness = 0.8f;

        [Header("Camera & Centering Settings")]
        public Camera mainCamera;
        public MuluCameraSwipeControl cameraSwipeControl;
        public float cameraDistance = 3.5f;
        public Vector3 cameraTargetOffset = new Vector3(0f, 0.7f, 0f);
        public float cameraMinPitch = -15f;
        public float cameraMaxPitch = 70f;
        public Color backgroundColor = new Color(0.04f, 0.05f, 0.07f, 1f);

        [Header("Key Light Settings")]
        public Light keyLight;
        public Color keyLightColor = new Color(1f, 0.96f, 0.90f);
        public float keyLightIntensity = 1.5f;
        public Vector3 keyLightRotation = new Vector3(30f, -30f, 0f);
        public LightShadows keyLightShadows = LightShadows.Soft;

        [Header("Fill Light Settings")]
        public Light fillLight;
        public Color fillLightColor = new Color(0.80f, 0.85f, 1f);
        public float fillLightIntensity = 0.6f;
        public Vector3 fillLightRotation = new Vector3(20f, 150f, 0f);

        [Header("Rim Light Settings")]
        public Light rimLight;
        public Color rimLightColor = new Color(1f, 1f, 1f);
        public float rimLightIntensity = 1.2f;
        public Vector3 rimLightRotation = new Vector3(135f, 45f, 0f);

        private void Start()
        {
            AutoFindReferences();
            ApplySettings();
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                AutoFindReferences();
                ApplySettings();
            }
        }

        private void OnValidate()
        {
            ApplySettings();
        }

        public void AutoFindReferences()
        {
            if (characterTransform == null)
            {
                characterTransform = transform.Find("CoolPirate_EditableCharacter")
                    ?? (transform.childCount > 0 ? transform.GetChild(0) : null);
            }

            if (platformRenderer == null)
            {
                var plat = transform.Find("CharacterStagePlatform");
                if (plat != null) platformRenderer = plat.GetComponent<Renderer>();
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (cameraSwipeControl == null)
            {
                cameraSwipeControl = FindAnyObjectByType<MuluCameraSwipeControl>();
            }

            if (keyLight == null)
            {
                var l = transform.Find("CharacterKeyLight");
                if (l != null) keyLight = l.GetComponent<Light>();
            }

            if (fillLight == null)
            {
                var l = transform.Find("CharacterFillLight");
                if (l != null) fillLight = l.GetComponent<Light>();
            }

            if (rimLight == null)
            {
                var l = transform.Find("CharacterRimLight");
                if (l != null) rimLight = l.GetComponent<Light>();
            }
        }

        public void ApplySettings()
        {
            // Apply character transform settings
            try
            {
                if (characterTransform != null)
                {
                    CacheBaseCharacterScale();

                    if (autoFitCharacterScale)
                    {
                        FitCharacterToBounds();
                    }
                    else
                    {
                        characterTransform.localScale = baseCharacterLocalScale * Mathf.Max(characterScale, 0.01f);
                    }

                    CenterCharacterFromBounds();
                    FrameCharacterFromBounds();
                }
            }
            catch (System.Exception e) { Debug.LogWarning("Configurator characterTransform error: " + e.Message); }

            // Apply platform settings
            try
            {
                if (platformRenderer != null)
                {
                    platformRenderer.transform.localScale = new Vector3(platformDiameter, 0.05f, platformDiameter);
                    platformRenderer.transform.localPosition = new Vector3(0f, -0.05f, 0f);
                    
                    Material mat = platformRenderer.sharedMaterial;
                    if (mat == null || mat.name.StartsWith("Default") || !mat.shader.name.Contains("Universal Render Pipeline"))
                    {
                        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                        if (shader != null)
                        {
                            mat = new Material(shader);
                            mat.name = "MuluPlatformMaterial_Instance";
                            platformRenderer.sharedMaterial = mat;
                        }
                    }
                    if (mat != null)
                    {
                        mat.color = platformColor;
                        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", platformMetallic);
                        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", platformSmoothness);
                        if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", platformSmoothness);
                    }
                }
            }
            catch (System.Exception e) { Debug.LogWarning("Configurator platformRenderer error: " + e.Message); }

            // Apply light settings
            try
            {
                if (keyLight != null)
                {
                    keyLight.type = LightType.Directional;
                    keyLight.color = keyLightColor;
                    keyLight.intensity = keyLightIntensity;
                    keyLight.transform.localRotation = Quaternion.Euler(keyLightRotation);
                    keyLight.shadows = keyLightShadows;
                }
            }
            catch (System.Exception e) { Debug.LogWarning("Configurator keyLight error: " + e.Message); }

            try
            {
                if (fillLight != null)
                {
                    fillLight.type = LightType.Directional;
                    fillLight.color = fillLightColor;
                    fillLight.intensity = fillLightIntensity;
                    fillLight.transform.localRotation = Quaternion.Euler(fillLightRotation);
                    fillLight.shadows = LightShadows.None;
                }
            }
            catch (System.Exception e) { Debug.LogWarning("Configurator fillLight error: " + e.Message); }

            try
            {
                if (rimLight != null)
                {
                    rimLight.type = LightType.Directional;
                    rimLight.color = rimLightColor;
                    rimLight.intensity = rimLightIntensity;
                    rimLight.transform.localRotation = Quaternion.Euler(rimLightRotation);
                    rimLight.shadows = LightShadows.None;
                }
            }
            catch (System.Exception e) { Debug.LogWarning("Configurator rimLight error: " + e.Message); }

            // Apply camera & SwipeControl settings
            try
            {
                if (mainCamera != null)
                {
                    mainCamera.clearFlags = CameraClearFlags.SolidColor;
                    mainCamera.backgroundColor = backgroundColor;
                }
            }
            catch (System.Exception e) { Debug.LogWarning("Configurator mainCamera error: " + e.Message); }

            try
            {
                if (cameraSwipeControl != null)
                {
                    // Safe auto-wires if they were lost
                    if (cameraSwipeControl.cameraTransform == null && mainCamera != null)
                    {
                        cameraSwipeControl.cameraTransform = mainCamera.transform;
                    }
                    if (cameraSwipeControl.targetCharacter == null && characterTransform != null)
                    {
                        cameraSwipeControl.targetCharacter = characterTransform;
                    }

                    cameraSwipeControl.distance = cameraDistance;
                    cameraSwipeControl.targetOffset = cameraTargetOffset;
                    cameraSwipeControl.minPitch = cameraMinPitch;
                    cameraSwipeControl.maxPitch = cameraMaxPitch;
                    // Force update swipe control camera position
                    cameraSwipeControl.UpdateCameraPosition();
                }
            }
            catch (System.Exception e) { Debug.LogWarning("Configurator cameraSwipeControl error: " + e.Message); }
        }

        private void FitCharacterToBounds()
        {
            if (characterTransform == null)
            {
                return;
            }

            characterTransform.localScale = baseCharacterLocalScale;

            Renderer[] renderers = characterTransform.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                characterTransform.localScale = baseCharacterLocalScale * Mathf.Max(characterScale, 0.01f);
                return;
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            float baseHeight = Mathf.Max(bounds.size.y, 0.0001f);
            float fitMultiplier = targetCharacterHeight / baseHeight;
            float finalMultiplier = fitMultiplier * Mathf.Max(characterScale, 0.01f);

            characterTransform.localScale = baseCharacterLocalScale * finalMultiplier;
        }

        private void CacheBaseCharacterScale()
        {
            if (cachedBaseCharacterScale || characterTransform == null)
            {
                return;
            }

            baseCharacterLocalScale = characterTransform.localScale;
            cachedBaseCharacterScale = true;
        }

        private void CenterCharacterFromBounds()
        {
            if (characterTransform == null)
            {
                return;
            }

            Renderer[] renderers = characterTransform.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                characterTransform.localPosition = characterPositionOffset;
                return;
            }

            Bounds bounds = CalculateBounds(characterTransform);

            Vector3 pivotOffset = bounds.center - characterTransform.position;
            Vector3 bottomOffset = bounds.min - characterTransform.position;
            float floorY = GetPlatformTopY();
            Vector3 desiredBottom = transform.position + characterPositionOffset;
            desiredBottom.y = floorY + floorClearance;
            Vector3 desiredCenter = desiredBottom - new Vector3(0f, bottomOffset.y, 0f);
            desiredCenter.x = transform.position.x + characterPositionOffset.x;
            desiredCenter.z = transform.position.z + characterPositionOffset.z;
            characterTransform.position += desiredCenter - (characterTransform.position + new Vector3(pivotOffset.x, 0f, pivotOffset.z));
            Bounds adjustedBounds = CalculateBounds(characterTransform);
            if (adjustedBounds.size.sqrMagnitude > 0.0001f)
            {
                characterTransform.position += Vector3.up * ((floorY + floorClearance) - adjustedBounds.min.y);
            }
        }

        private float GetPlatformTopY()
        {
            if (platformRenderer != null)
            {
                return platformRenderer.bounds.max.y;
            }

            Transform platform = transform.Find("CharacterStagePlatform");
            if (platform != null)
            {
                Renderer renderer = platform.GetComponent<Renderer>();
                return renderer != null ? renderer.bounds.max.y : platform.position.y;
            }

            return transform.position.y;
        }

        private static Bounds CalculateBounds(Transform root)
        {
            Renderer[] renderers = root != null ? root.GetComponentsInChildren<Renderer>(true) : null;
            if (renderers == null || renderers.Length == 0)
            {
                return new Bounds(root != null ? root.position : Vector3.zero, Vector3.zero);
            }

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    bounds.Encapsulate(renderers[i].bounds);
                }
            }

            return bounds;
        }

        private void FrameCharacterFromBounds()
        {
            if (characterTransform == null)
            {
                return;
            }

            Renderer[] renderers = characterTransform.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                return;
            }

            Bounds bounds = CalculateBounds(characterTransform);

            Vector3 pivotOffset = bounds.center - characterTransform.position;
            cameraTargetOffset = new Vector3(pivotOffset.x, Mathf.Max(pivotOffset.y, 0.7f), pivotOffset.z);

            float frameDistance = Mathf.Max(bounds.extents.magnitude * 2.25f, 3.5f);
            cameraDistance = frameDistance;
        }
    }
}

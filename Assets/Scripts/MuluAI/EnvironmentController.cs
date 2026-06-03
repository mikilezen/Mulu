using UnityEngine;

namespace MuluAI
{
    public class EnvironmentController : MonoBehaviour
    {
        [Header("Lighting")]
        [SerializeField] private Light directionalLight;
        [SerializeField] private Material skyboxMaterial;
        [SerializeField] private Color daySkyColor = new Color(0.52f, 0.75f, 1f);
        [SerializeField] private Color nightSkyColor = new Color(0.02f, 0.03f, 0.08f);
        [SerializeField] private Color sunsetSkyColor = new Color(1f, 0.45f, 0.22f);
        [SerializeField] private Color dayLightColor = new Color(1f, 0.95f, 0.84f);
        [SerializeField] private Color nightLightColor = new Color(0.32f, 0.42f, 0.75f);
        [SerializeField] private Color sunsetLightColor = new Color(1f, 0.58f, 0.34f);
        [SerializeField] private float dayLightIntensity = 1.15f;
        [SerializeField] private float nightLightIntensity = 0.28f;
        [SerializeField] private float sunsetLightIntensity = 0.75f;

        [Header("Weather Effects")]
        [SerializeField] private ParticleSystem rainParticles;
        [SerializeField] private ParticleSystem fogParticles;
        [SerializeField] private bool useRenderSettingsFog = true;
        [SerializeField] private Color fogColor = new Color(0.62f, 0.68f, 0.72f);
        [SerializeField] private float fogDensity = 0.035f;

        public void ApplyEnvironment(EnvironmentConfig config)
        {
            if (config == null)
            {
                return;
            }

            ApplyTimeOfDay(config.time_of_day);
            ApplyWeather(config.weather);
        }

        private void ApplyTimeOfDay(string timeOfDay)
        {
            string normalized = string.IsNullOrWhiteSpace(timeOfDay) ? "day" : timeOfDay.Trim().ToLowerInvariant();

            Color skyColor;
            Color lightColor;
            float lightIntensity;
            Vector3 lightRotation;

            switch (normalized)
            {
                case "night":
                    skyColor = nightSkyColor;
                    lightColor = nightLightColor;
                    lightIntensity = nightLightIntensity;
                    lightRotation = new Vector3(325f, 30f, 0f);
                    break;
                case "sunset":
                    skyColor = sunsetSkyColor;
                    lightColor = sunsetLightColor;
                    lightIntensity = sunsetLightIntensity;
                    lightRotation = new Vector3(18f, 210f, 0f);
                    break;
                default:
                    skyColor = daySkyColor;
                    lightColor = dayLightColor;
                    lightIntensity = dayLightIntensity;
                    lightRotation = new Vector3(50f, -30f, 0f);
                    break;
            }

            if (directionalLight != null)
            {
                directionalLight.color = lightColor;
                directionalLight.intensity = lightIntensity;
                directionalLight.transform.rotation = Quaternion.Euler(lightRotation);
            }

            if (skyboxMaterial != null)
            {
                RenderSettings.skybox = skyboxMaterial;
                if (skyboxMaterial.HasProperty("_Tint"))
                {
                    skyboxMaterial.SetColor("_Tint", skyColor);
                }
                else if (skyboxMaterial.HasProperty("_SkyTint"))
                {
                    skyboxMaterial.SetColor("_SkyTint", skyColor);
                }
                else if (skyboxMaterial.HasProperty("_Color"))
                {
                    skyboxMaterial.SetColor("_Color", skyColor);
                }
            }

            RenderSettings.ambientLight = Color.Lerp(skyColor, lightColor, 0.35f);
            DynamicGI.UpdateEnvironment();
        }

        private void ApplyWeather(string weather)
        {
            string normalized = string.IsNullOrWhiteSpace(weather) ? "clear" : weather.Trim().ToLowerInvariant();

            SetParticleActive(rainParticles, normalized == "rainy" || normalized == "rain");
            SetParticleActive(fogParticles, normalized == "foggy" || normalized == "fog");

            if (!useRenderSettingsFog)
            {
                return;
            }

            bool fogEnabled = normalized == "foggy" || normalized == "fog" || normalized == "rainy" || normalized == "rain";
            RenderSettings.fog = fogEnabled;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = normalized == "rainy" || normalized == "rain" ? fogDensity * 0.65f : fogDensity;
        }

        private static void SetParticleActive(ParticleSystem particleSystem, bool active)
        {
            if (particleSystem == null)
            {
                return;
            }

            GameObject particleObject = particleSystem.gameObject;
            if (!particleObject.activeSelf && active)
            {
                particleObject.SetActive(true);
            }

            if (active)
            {
                if (!particleSystem.isPlaying)
                {
                    particleSystem.Play(true);
                }
            }
            else
            {
                if (particleSystem.isPlaying)
                {
                    particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }

                particleObject.SetActive(false);
            }
        }
    }
}

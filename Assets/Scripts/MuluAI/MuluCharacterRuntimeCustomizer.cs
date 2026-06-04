using UnityEngine;

namespace MuluAI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mulu AI/Character Runtime Customizer")]
    public class MuluCharacterRuntimeCustomizer : MonoBehaviour
    {
        public Transform currentCharacter;
        public string generatedCharacterName = "GeneratedPlayableCharacter";

        public Transform ReplaceCharacter(GameObject characterPrefab)
        {
            if (characterPrefab == null)
            {
                return currentCharacter;
            }

            Transform oldCharacter = ResolveCurrentCharacter();
            Vector3 localPosition = oldCharacter != null ? oldCharacter.localPosition : Vector3.zero;
            Quaternion localRotation = oldCharacter != null ? oldCharacter.localRotation : Quaternion.identity;
            Vector3 localScale = oldCharacter != null ? oldCharacter.localScale : Vector3.one;

            if (oldCharacter != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(oldCharacter.gameObject);
                }
                else
                {
                    DestroyImmediate(oldCharacter.gameObject);
                }
            }

            GameObject instance = Instantiate(characterPrefab, transform);
            instance.name = generatedCharacterName;
            Transform instanceTransform = instance.transform;
            instanceTransform.localPosition = localPosition;
            instanceTransform.localRotation = localRotation;
            instanceTransform.localScale = localScale;
            currentCharacter = instanceTransform;

            if (instance.GetComponent<MuluCharacterMotor>() == null)
            {
                instance.AddComponent<MuluCharacterMotor>();
            }

            RefreshStageReferences();
            return currentCharacter;
        }

        public void ApplyTexture(Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            Transform character = ResolveCurrentCharacter();
            if (character == null)
            {
                return;
            }

            Renderer[] renderers = character.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Material material = renderer.material;
                if (material == null)
                {
                    continue;
                }

                if (material.HasProperty("_BaseMap"))
                {
                    material.SetTexture("_BaseMap", texture);
                }
                else if (material.HasProperty("_MainTex"))
                {
                    material.SetTexture("_MainTex", texture);
                }
            }
        }

        public void ApplyMaterial(Material material)
        {
            if (material == null)
            {
                return;
            }

            Transform character = ResolveCurrentCharacter();
            if (character == null)
            {
                return;
            }

            Renderer[] renderers = character.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].sharedMaterial = material;
                }
            }
        }

        private Transform ResolveCurrentCharacter()
        {
            if (currentCharacter != null)
            {
                return currentCharacter;
            }

            currentCharacter = transform.Find("CoolPirate_EditableCharacter")
                ?? transform.Find("GeneratedPlayableCharacter")
                ?? transform.Find("CharacterFallback")
                ?? (transform.childCount > 0 ? transform.GetChild(0) : null);
            return currentCharacter;
        }

        private void RefreshStageReferences()
        {
            MuluCharacterConfigurator configurator = GetComponent<MuluCharacterConfigurator>();
            if (configurator != null)
            {
                configurator.characterTransform = currentCharacter;
                configurator.ApplySettings();
            }

            MuluMobileNavigation navigation = FindAnyObjectByType<MuluMobileNavigation>();
            if (navigation != null)
            {
                navigation.characterTransform = currentCharacter;
            }

            MuluCameraSwipeControl swipeControl = FindAnyObjectByType<MuluCameraSwipeControl>();
            if (swipeControl != null)
            {
                swipeControl.targetCharacter = currentCharacter;
            }
        }
    }
}

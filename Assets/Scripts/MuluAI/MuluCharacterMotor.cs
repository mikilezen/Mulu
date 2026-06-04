using UnityEngine;

namespace MuluAI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Mulu AI/Character Motor")]
    public class MuluCharacterMotor : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 3.4f;
        public float acceleration = 18f;
        public float rotationSmoothing = 14f;
        public float stageRadius = 2.65f;

        [Header("Jump")]
        public float jumpHeight = 0.85f;
        public float gravity = -18f;
        public float floorClearance = 0.03f;

        [Header("Animation")]
        public Animator animator;
        public string speedParameter = "Speed";
        public string groundedParameter = "Grounded";
        public string jumpTrigger = "Jump";

        private Vector3 planarVelocity;
        private float verticalVelocity;
        private bool isGrounded = true;
        private int speedHash;
        private int groundedHash;
        private int jumpHash;

        public bool IsGrounded => isGrounded;

        private void Awake()
        {
            CacheAnimator();
            CacheAnimatorHashes();
            SnapToFloor();
        }

        private void OnEnable()
        {
            CacheAnimator();
            CacheAnimatorHashes();
            SnapToFloor();
        }

        public void Move(Vector3 worldDirection, float analogStrength, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return;
            }

            worldDirection.y = 0f;
            if (worldDirection.sqrMagnitude > 1f)
            {
                worldDirection.Normalize();
            }

            Vector3 targetVelocity = worldDirection * (moveSpeed * Mathf.Clamp01(analogStrength));
            planarVelocity = Vector3.Lerp(planarVelocity, targetVelocity, 1f - Mathf.Exp(-acceleration * deltaTime));

            ApplyGravity(deltaTime);

            Vector3 nextPosition = transform.position + (planarVelocity + Vector3.up * verticalVelocity) * deltaTime;
            nextPosition = ClampToStage(nextPosition);
            transform.position = nextPosition;
            SnapToFloorIfNeeded();

            Vector3 faceDirection = planarVelocity;
            faceDirection.y = 0f;
            if (faceDirection.sqrMagnitude > 0.0025f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(faceDirection.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothing * deltaTime);
            }

            UpdateAnimator();
        }

        public void Stop(float deltaTime)
        {
            planarVelocity = Vector3.Lerp(planarVelocity, Vector3.zero, 1f - Mathf.Exp(-acceleration * deltaTime));
            ApplyGravity(deltaTime);

            if (planarVelocity.sqrMagnitude > 0.0001f || Mathf.Abs(verticalVelocity) > 0.0001f)
            {
                Vector3 nextPosition = transform.position + (planarVelocity + Vector3.up * verticalVelocity) * deltaTime;
                nextPosition = ClampToStage(nextPosition);
                transform.position = nextPosition;
                SnapToFloorIfNeeded();
            }

            UpdateAnimator();
        }

        public void Jump()
        {
            SnapToFloorIfNeeded();
            if (!isGrounded)
            {
                return;
            }

            verticalVelocity = Mathf.Sqrt(Mathf.Max(0.01f, jumpHeight) * -2f * gravity);
            isGrounded = false;

            if (animator != null && HasParameter(animator, jumpHash, AnimatorControllerParameterType.Trigger))
            {
                animator.SetTrigger(jumpHash);
            }
        }

        public void SnapToFloor()
        {
            Bounds bounds = CalculateRendererBounds();
            float floorY = GetFloorY();
            if (bounds.size.sqrMagnitude <= 0.0001f)
            {
                Vector3 pos = transform.position;
                pos.y = floorY + floorClearance;
                transform.position = pos;
                isGrounded = true;
                verticalVelocity = 0f;
                return;
            }

            float lift = (floorY + floorClearance) - bounds.min.y;
            if (Mathf.Abs(lift) > 0.0001f)
            {
                transform.position += Vector3.up * lift;
            }

            isGrounded = true;
            verticalVelocity = 0f;
        }

        private void ApplyGravity(float deltaTime)
        {
            if (isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -0.5f;
            }

            verticalVelocity += gravity * deltaTime;
        }

        private void SnapToFloorIfNeeded()
        {
            Bounds bounds = CalculateRendererBounds();
            float floorY = GetFloorY();
            if (bounds.size.sqrMagnitude <= 0.0001f)
            {
                if (transform.position.y <= floorY + floorClearance)
                {
                    Vector3 pos = transform.position;
                    pos.y = floorY + floorClearance;
                    transform.position = pos;
                    verticalVelocity = 0f;
                    isGrounded = true;
                }
                else
                {
                    isGrounded = false;
                }

                return;
            }

            if (bounds.min.y <= floorY + floorClearance && verticalVelocity <= 0f)
            {
                transform.position += Vector3.up * ((floorY + floorClearance) - bounds.min.y);
                verticalVelocity = 0f;
                isGrounded = true;
            }
            else
            {
                isGrounded = false;
            }
        }

        private Vector3 ClampToStage(Vector3 position)
        {
            if (stageRadius <= 0f)
            {
                return position;
            }

            Vector3 stageCenter = transform.parent != null ? transform.parent.position : Vector3.zero;
            Vector2 offset = new Vector2(position.x - stageCenter.x, position.z - stageCenter.z);
            if (offset.sqrMagnitude > stageRadius * stageRadius)
            {
                offset = offset.normalized * stageRadius;
                position.x = stageCenter.x + offset.x;
                position.z = stageCenter.z + offset.y;
            }

            return position;
        }

        private float GetFloorY()
        {
            Transform stage = transform.parent;
            if (stage != null)
            {
                Transform platform = stage.Find("CharacterStagePlatform");
                if (platform != null)
                {
                    Renderer platformRenderer = platform.GetComponent<Renderer>();
                    if (platformRenderer != null)
                    {
                        return platformRenderer.bounds.max.y;
                    }

                    return platform.position.y;
                }

                return stage.position.y;
            }

            return 0f;
        }

        private Bounds CalculateRendererBounds()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                return new Bounds(transform.position, Vector3.zero);
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

        private void CacheAnimator()
        {
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>(true);
            }
        }

        private void CacheAnimatorHashes()
        {
            speedHash = Animator.StringToHash(speedParameter);
            groundedHash = Animator.StringToHash(groundedParameter);
            jumpHash = Animator.StringToHash(jumpTrigger);
        }

        private void UpdateAnimator()
        {
            if (animator == null)
            {
                return;
            }

            if (HasParameter(animator, speedHash, AnimatorControllerParameterType.Float))
            {
                animator.SetFloat(speedHash, planarVelocity.magnitude);
            }

            if (HasParameter(animator, groundedHash, AnimatorControllerParameterType.Bool))
            {
                animator.SetBool(groundedHash, isGrounded);
            }
        }

        private static bool HasParameter(Animator targetAnimator, int hash, AnimatorControllerParameterType type)
        {
            if (targetAnimator == null || targetAnimator.runtimeAnimatorController == null)
            {
                return false;
            }

            AnimatorControllerParameter[] parameters = targetAnimator.parameters;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].nameHash == hash && parameters[i].type == type)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

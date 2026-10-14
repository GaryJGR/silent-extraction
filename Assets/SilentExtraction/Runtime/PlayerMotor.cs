using UnityEngine;
using UnityEngine.InputSystem;

namespace SilentExtraction
{
    /// <summary>Camera-relative locomotion, crouching, and audible movement.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        public Transform visual;
        public float walkSpeed = 3.6f;
        public float sprintSpeed = 5.8f;
        public float crouchSpeed = 1.9f;
        public bool IsCrouching { get; private set; }
        public bool IsSprinting { get; private set; }
        public float NoiseRadius { get; private set; }
        public Vector3 EyePosition => transform.position + Vector3.up * (IsCrouching ? .65f : 1.35f);
        CharacterController controller;
        float verticalSpeed;
        float noiseTimer;

        void Awake() => controller = GetComponent<CharacterController>();

        void Update()
        {
            var mission = MissionController.Instance;
            if (mission == null || mission.Phase != MissionPhase.Playing)
            {
                NoiseRadius = 0;
                return;
            }
            var keyboard = Keyboard.current;
            if (keyboard == null) return;
            IsCrouching = keyboard.cKey.isPressed || keyboard.leftCtrlKey.isPressed;
            Vector2 input = new Vector2(
                (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0) -
                (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0),
                (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1 : 0) -
                (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1 : 0));
            input = Vector2.ClampMagnitude(input, 1f);
            IsSprinting = keyboard.leftShiftKey.isPressed && !IsCrouching && input.sqrMagnitude > .01f;
            float speed = IsCrouching ? crouchSpeed : IsSprinting ? sprintSpeed : walkSpeed;
            Vector3 forward = Camera.main.transform.forward;
            forward.y = 0;
            forward.Normalize();
            Vector3 right = Camera.main.transform.right;
            right.y = 0;
            right.Normalize();
            Vector3 movement = right * input.x + forward * input.y;
            if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            verticalSpeed += Physics.gravity.y * Time.deltaTime;
            controller.Move((movement * speed + Vector3.up * verticalSpeed) * Time.deltaTime);
            if (movement.sqrMagnitude > .01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(movement), 14 * Time.deltaTime);

            float height = IsCrouching ? 1f : 1.8f;
            controller.height = height;
            controller.center = Vector3.up * height * .5f;
            if (visual != null)
                visual.localScale = Vector3.Lerp(visual.localScale, new Vector3(1, IsCrouching ? .56f : 1, 1), 12 * Time.deltaTime);

            NoiseRadius = input.sqrMagnitude < .01f ? 0 : IsCrouching ? .6f : IsSprinting ? 7 : 2;
            noiseTimer -= Time.deltaTime;
            if (NoiseRadius > 0 && noiseTimer <= 0)
            {
                mission.EmitNoise(transform.position, NoiseRadius);
                noiseTimer = .45f;
            }
            if (keyboard.eKey.wasPressedThisFrame) mission.Interact();
            if (keyboard.qKey.wasPressedThisFrame) mission.ThrowDecoy();
        }
    }
}

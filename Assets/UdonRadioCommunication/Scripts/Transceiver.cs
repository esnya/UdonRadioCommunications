
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace UdonRadioCommunication
{
    [DefaultExecutionOrder(1000)]
    public class Transceiver : UdonSharpBehaviour
    {
        [UdonSynced] public bool active, talking;
        public bool exclusive = true;
        public TextMeshPro frequencyText;
        public Receiver receiver;
        public Transmitter transmitter;
        [UdonSynced] public float frequency = 1.0f;

        public float frequencyStep = 1.0f, minFrequency = 1.0f, maxFrequency = 8.0f;
        public string frequencyPrefix = "", frequencySuffix = " <size=75%>Ch</size>", frequencyFormat="";
        [Tooltip("Drives bool parameters \"PowerOn\" and \"Talking\"")] public Animator[] animators = {};

        private void SetBool(string name, bool value)
        {
            foreach (var animator in animators)
            {
                if (animator == null) continue;
                animator.SetBool(name, value);
            }
        }

        private void Start()
        {
            var pickup = (VRCPickup)GetComponent(typeof(VRCPickup));
            if (pickup != null) pickup.AutoHold = VRC_Pickup.AutoHoldMode.Yes;

            if (Networking.IsMaster) ApplyState();
            else UpdateVisual();
        }

        public override void OnOwnershipTransferred(VRCPlayerApi player)
        {
            if (player.isLocal)
            {
                ApplyState();
            }
        }
        public override void OnPickupUseDown() => SetTalking(true);

        public override void OnPickupUseUp() => SetTalking(false);

        public void TakeOwnership()
        {
            if (Networking.IsOwner(gameObject)) return;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        public void Respawn()
        {
            TakeOwnership();
            var sync = (VRCObjectSync)GetComponent(typeof(VRCObjectSync));
            if (sync == null) return;
            sync.Respawn();
        }

        public void SetTalking(bool value)
        {
            TakeOwnership();
            talking = value;
            ApplyState();
        }
        public bool GetTalking() => talking;
        public void StartTalking() => SetTalking(true);
        public void StopTalking() => SetTalking(false);
        public void ToggleTalking() => SetTalking(!talking);

        private void SetActive(bool value)
        {
            TakeOwnership();
            active = value;
            ApplyState();
        }
        public bool GetActive() => active;
        public void Activate() => SetActive(true);
        public void Deactivate() => SetActive(false);
        public void ToggleActive() => SetActive(!active);

        public void SetFrequency(float f)
        {
            TakeOwnership();
            frequency = Mathf.Clamp(f, minFrequency, maxFrequency);
            ApplyState();
        }
        public void IncrementFrequency() => SetFrequency(frequency + frequencyStep);
        public void DecrementFrequency() => SetFrequency(frequency - frequencyStep);

        public override void OnDeserialization()
        {
            UpdateVisual();
        }

        public void ApplyState()
        {
            TakeOwnership();

            receiver.SetActive(active && !(exclusive && talking));
            transmitter.SetActive(active && talking);
            receiver.SetFrequency(frequency);
            transmitter.SetFrequency(frequency);

            RequestSerialization();

            UpdateVisual();
        }

        public void UpdateVisual()
        {
            frequencyText.text = $"{frequencyPrefix}{frequency.ToString(frequencyFormat)}{frequencySuffix}";

            SetBool("Talking", talking && active);
            SetBool("PowerOn", active);
        }
    }
}

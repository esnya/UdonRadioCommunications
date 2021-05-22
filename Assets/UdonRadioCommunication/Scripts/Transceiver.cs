
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace UdonRadioCommunication
{
    [
        RequireComponent(typeof(VRCPickup)),
        RequireComponent(typeof(VRCObjectSync)),
        UdonBehaviourSyncMode(BehaviourSyncMode.Continuous),
    ]
    public class Transceiver : UdonSharpBehaviour
    {
        [UdonSynced] public bool active;
        public bool talking, exclusive = true;
        public TextMeshPro frequencyText;
        public Receiver receiver;
        public Transmitter transmitter;
        [UdonSynced] public float frequency = 122.6f;
        public float frequencyStep = 0.025f, minFrequency = 118.0f, maxFrequency = 136.975f;
        public string frequencyPrefix = "", frequencySuffix = " <size=75%>MHz</size>", frequencyFormat="f3";
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
            pickup.AutoHold = VRC_Pickup.AutoHoldMode.Yes;

            SetFrequency(frequency);
            SetActive(active);
            SetTalking(talking);
        }

        public override void OnPickup()
        {
            Networking.SetOwner(Networking.LocalPlayer, transmitter.gameObject);
            Networking.SetOwner(Networking.LocalPlayer, receiver.gameObject);
        }

        private void SetTalking(bool b)
        {
            talking = b;
            transmitter.frequency = frequency;
            if (active && exclusive)
            {
                if (b) receiver.Deactivate();
                else receiver.Activate();
            }
            if (b && active) transmitter.Activate();
            else transmitter.Deactivate();

            UpdateVisual();
        }

        public override void OnPickupUseDown() => SetTalking(true);

        public override void OnPickupUseUp() => SetTalking(false);
        public void StartTalking() => SetTalking(true);
        public void StopTalking() => SetTalking(false);
        public void ToggleTalking() => SetTalking(!talking);

        private void SetActive(bool b)
        {
            active = b;
            if (active) Networking.SetOwner(Networking.LocalPlayer, gameObject);
            RequestSerialization();
            if (b) receiver.Activate();
            else receiver.Deactivate();
            SetTalking(talking);
            UpdateVisual();
        }
        public void Activate() => SetActive(true);
        public void Deactivate() => SetActive(false);
        public void ToggleActive() => SetActive(!active);

        private void SetFrequency(float newFrequency)
        {
            frequency = Mathf.Clamp(newFrequency, minFrequency, maxFrequency);
            RequestSerialization();

            receiver.frequency = frequency;
            receiver.RequestSerialization();

            transmitter.frequency = frequency;
            transmitter.RequestSerialization();

            UpdateVisual();
        }
        public void IncrementFrequency() => SetFrequency(frequency + frequencyStep);
        public void DecrementFrequency() => SetFrequency(frequency - frequencyStep);

        public override void OnDeserialization()
        {
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

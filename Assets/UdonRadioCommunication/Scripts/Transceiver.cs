
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace UdonRadioCommunication
{
    [RequireComponent(typeof(VRCPickup))]
    public class Transceiver : UdonSharpBehaviour
    {
        public bool active, talking, exclusive = true;
        public TextMeshPro frequencyText;
        public Receiver receiver;
        public Transmitter transmitter;
        public float frequency = 122.6f, frequencyStep = 0.025f, minFrequency = 118.0f, maxFrequency = 136.975f;
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
        }

        private void SetTalking(bool b)
        {
            talking = b;
            SetBool("Talking", b && active);
            transmitter.frequency = frequency;
            if (active && exclusive)
            {
                if (b) receiver.Deactivate();
                else receiver.Activate();
            }
            if (b && active) transmitter.Activate();
            else transmitter.Deactivate();
        }

        public override void OnPickupUseDown() => SetTalking(true);

        public override void OnPickupUseUp() => SetTalking(false);
        public void StartTalking() => SetTalking(true);
        public void StopTalking() => SetTalking(false);
        public void ToggleTalking() => SetTalking(!talking);

        private void SetActive(bool b)
        {
            active = b;
            SetBool("PowerOn", b);
            if (b) receiver.Activate();
            else receiver.Deactivate();
            SetTalking(talking);
        }
        public void Activate() => SetActive(true);
        public void Deactivate() => SetActive(false);
        public void ToggleActive() => SetActive(!active);

        private void SetFrequency(float newFrequency)
        {
            frequency = Mathf.Clamp(newFrequency, minFrequency, maxFrequency);
            receiver.frequency = frequency;
            transmitter.frequency = frequency;
            frequencyText.text = $"{frequencyPrefix}{frequency.ToString(frequencyFormat)}{frequencySuffix}";
        }
        public void IncrementFrequency() => SetFrequency(frequency + frequencyStep);
        public void DecrementFrequency() => SetFrequency(frequency - frequencyStep);
    }
}

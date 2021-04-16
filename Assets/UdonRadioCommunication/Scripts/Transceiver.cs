
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
        public TextMeshPro frequencyText;
        public Receiver receiver;
        public Transmitter transmitter;
        public float frequency = 122.6f, frequencyStep = 0.025f, minFrequency = 118.0f, maxFrequency = 136.975f;
        public string frequencySuffix = " <size=75%>MHz</size>", frequencyFormat="f3";
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

            _SetFrequency(frequency);
            SetTalking(false);
            SetPower(false);
        }

        public override void OnPickup()
        {
            Networking.SetOwner(Networking.LocalPlayer, transmitter.gameObject);
        }

        private void SetTalking(bool b)
        {
            SetBool("Talking", b);
            transmitter.frequency = frequency;
            if (powerOn)
            {
                if (b) receiver._Deactivate();
                else receiver._Activate();
            }
            if (b) transmitter._Activate();
            else transmitter._Deactivate();
        }

        public override void OnPickupUseDown() => SetTalking(true);

        public override void OnPickupUseUp() => SetTalking(false);

        private bool powerOn;
        private void SetPower(bool b)
        {
            powerOn = b;
            SetBool("PowerOn", b);
            if (b) receiver._Activate();
            else receiver._Deactivate();
        }
        public void _PowerOn() => SetPower(true);
        public void _PowerOff() => SetPower(false);

        private void _SetFrequency(float newFrequency)
        {
            frequency = Mathf.Clamp(newFrequency, minFrequency, maxFrequency);
            receiver.frequency = frequency;
            transmitter.frequency = frequency;
            frequencyText.text = $"{frequency.ToString(frequencyFormat)}{frequencySuffix}";
        }
        public void _IncrementFrequency() => _SetFrequency(frequency + frequencyStep);
        public void _DecrementFrequency() => _SetFrequency(frequency - frequencyStep);
    }
}

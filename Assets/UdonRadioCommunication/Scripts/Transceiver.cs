
#pragma warning disable IDE0051,IDE1006
using JetBrains.Annotations;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace UdonRadioCommunication
{
    [DefaultExecutionOrder(1000), UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Transceiver : UdonSharpBehaviour
    {

        public bool exclusive = true;
        [NotNull] public Receiver receiver;
        [NotNull] public Transmitter transmitter;
        [UdonSynced, FieldChangeCallback(nameof(Frequency))] public float frequency = 1.0f;
        public float frequencyStep = 1.0f, minFrequency = 1.0f, maxFrequency = 8.0f;

        [Header("Optional")]
        public TextMeshPro frequencyText;
        [Tooltip("Drives bool parameters \"PowerOn\" and \"Talking\"")] public Animator[] animators = {};

        private string frequencyFormat;
        private float Frequency {
            set {
                if (Networking.IsOwner(gameObject))
                {
                    receiver._SetFrequency(value);
                    transmitter._SetFrequency(value);
                }
                if (frequencyText != null) frequencyText.text = string.Format(frequencyFormat, value);
                frequency = value;
            }
            get => frequency;
        }

        [UdonSynced, FieldChangeCallback(nameof(Receive))] private bool _receive;
        private bool Receive {
            set {
                if (Networking.IsOwner(gameObject)) receiver._SetActive(value);
                _receive = value;
                SetBool("PowerOn", value);
            }
            get => _receive;
        }

        [UdonSynced, FieldChangeCallback(nameof(Transmit))] private bool _transmit;
        private bool Transmit {
            set {
                if (Networking.IsOwner(gameObject))
                {
                    transmitter._SetActive(value);
                    if (exclusive && Receive) receiver._SetActive(!value);
                }
                SetBool("Talking", value);
                _transmit = value;
            }
            get => _transmit;
        }

        private void SetBool(string name, bool value)
        {
            if (animators == null) return;
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

            if (frequencyText) frequencyFormat = frequencyText.text;

            Frequency = frequency;
            Receive = false;
            Transmit = false;
        }

        public override void OnPickupUseDown() => _StartTransmit();
        public override void OnPickupUseUp() => _StopTransmit();

        public void _TakeOwnership()
        {
            if (Networking.IsOwner(gameObject)) return;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        public void _SetTransmit(bool value)
        {
            _TakeOwnership();
            Transmit = value;
            RequestSerialization();
        }
        public bool _GetTransmit() => Transmit;
        public void _StartTransmit() => _SetTransmit(true);
        public void _StopTransmit() => _SetTransmit(false);
        public void _ToggleTransmit() => _SetTransmit(!Transmit);

        public void _SetReceive(bool value)
        {
            _TakeOwnership();
            Receive = value;
            RequestSerialization();
        }
        public bool _GetReceive() => Receive;
        public void _StartReceive() => _SetReceive(true);
        public void _StopReceive() => _SetReceive(false);
        public void _ToggleReceive() => _SetReceive(!Receive);

        public void _SetFrequency(float f)
        {
            _TakeOwnership();
            Frequency = f > maxFrequency ? minFrequency : (f < minFrequency ? maxFrequency : f);
            RequestSerialization();
        }
        public void _IncrementFrequency() => _SetFrequency(Frequency + frequencyStep);
        public void _DecrementFrequency() => _SetFrequency(Frequency - frequencyStep);

        public void _SetActive(bool value)
        {
            _TakeOwnership();
            Transmit = value;
            Receive = value;
            RequestSerialization();
        }
        public void _Activate() => _SetActive(true);
        public void _Deactivate() => _SetActive(false);
        public void _ToggleActive() => _SetActive(!(Transmit || Receive));
    }
}

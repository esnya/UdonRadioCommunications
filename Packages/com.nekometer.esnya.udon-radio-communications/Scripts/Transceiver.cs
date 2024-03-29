
using System;
using JetBrains.Annotations;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace URC
{
    [DefaultExecutionOrder(1000)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Transceiver : UdonSharpBehaviour
    {
        public bool exclusive = true;
        [NotNull] public Receiver receiver;
        [NotNull] public Transmitter transmitter;

        [Header("Optional")]
        public TextMeshPro frequencyText;
        public string frequencyTextFormat = "000.000";
        [Tooltip("Drives bool parameters \"PowerOn\" and \"Talking\"")] public Animator[] animators = { };

        [NonSerialized] public UdonRadioCommunication urc;
        [NonSerialized] public float minFrequency;
        [NonSerialized] public float maxFrequency;
        [NonSerialized] public float frequencyStep;
        [NonSerialized] public float fastFrequencyStep;

        [UdonSynced][FieldChangeCallback(nameof(Frequency))] private float _frequency;
        public float Frequency
        {
            set
            {
                if (Networking.IsOwner(gameObject))
                {
                    transmitter._SetFrequency(value);
                }
                receiver.Frequency = value;

                _frequency = value;
                _UpdateFrequencyText();
            }
            get => _frequency;
        }

        [UdonSynced]
        [FieldChangeCallback(nameof(Receive))]
        private bool _receive;
        private bool Receive
        {
            set
            {
                if (Networking.IsOwner(gameObject)) receiver.Active = value;
                _receive = value;
                SetBool("PowerOn", value);
            }
            get => _receive;
        }

        [UdonSynced][FieldChangeCallback(nameof(Transmit))] private bool _transmit;
        private bool Transmit
        {
            set
            {
                if (Networking.IsOwner(gameObject))
                {
                    transmitter._SetActive(value);
                    if (exclusive && Receive) receiver.Active = !value;
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

            Receive = false;
            Transmit = false;
        }

        public void _Initialize(UdonRadioCommunication urc)
        {
            this.urc = urc;

            minFrequency = urc.minFrequency;
            maxFrequency = urc.maxFrequency;
            frequencyStep = urc.frequencyStep;
            fastFrequencyStep = urc.fastFrequencyStep;

            if (Networking.IsOwner(gameObject))
            {
                Frequency = urc.defaultFrequency;
                RequestSerialization();
            }
        }

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
        public void _FastIncrementFrequency() => _SetFrequency(Frequency + fastFrequencyStep);
        public void _FastDecrementFrequency() => _SetFrequency(Frequency - fastFrequencyStep);

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

        public void _UpdateFrequencyText()
        {
            if (frequencyText != null) frequencyText.text = Frequency.ToString(frequencyTextFormat);
        }
    }
}

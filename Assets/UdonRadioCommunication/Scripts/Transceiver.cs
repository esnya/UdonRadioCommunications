
#pragma warning disable IDE0051,IDE1006
using JetBrains.Annotations;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.Components;
using VRC.SDKBase;

namespace UdonRadioCommunication
{
    [DefaultExecutionOrder(1000)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Transceiver : UdonSharpBehaviour
    {
        const int MAX_SUBSCRIBERS = 32;
        const string EVENT_FREQUENCY_CHANGED = "_Transceiver_Frequency_Changed";
        const string EVENT_RECEIVER_ON = "_Transceiver_Receiver_On";
        const string EVENT_RECEIVER_OFF = "_Transceiver_Receiver_Off";
        const string EVENT_TRANSMITTER_ON = "_Transceiver_Transmitter_On";
        const string EVENT_TRANSMITTER_OFF = "_Transceiver_Transmitter_Off";

        public bool exclusive = true;
        [NotNull] public Receiver receiver;
        [NotNull] public Transmitter transmitter;
        [UdonSynced, FieldChangeCallback(nameof(Frequency))] public float frequency = 1.0f;
        public float frequencyStep = 1.0f, fastFrequencyStep = 10.0f, minFrequency = 1.0f, maxFrequency = 8.0f;

        public bool overrideFrequencyFormat = false;
        public string frequencyFormat = "{0:#00.00#}";

        [Header("Optional")]
        public TextMeshPro frequencyText;
        [Tooltip("Drives bool parameters \"PowerOn\" and \"Talking\"")] public Animator[] animators = { };

        private UdonSharpBehaviour[] subscribers;
        private float Frequency
        {
            set
            {
                var isOwner = Networking.IsOwner(gameObject);
                if (!receiver.sync || isOwner) receiver._SetFrequency(value);
                if (isOwner) transmitter._SetFrequency(value);

                frequency = value;
                _UpdateFrequencyText();

                _Dispatch(EVENT_FREQUENCY_CHANGED);
            }
            get => frequency;
        }

        [UdonSynced, FieldChangeCallback(nameof(Receive))] private bool _receive;
        private bool Receive
        {
            set
            {
                if (Networking.IsOwner(gameObject)) receiver._SetActive(value);
                _receive = value;
                SetBool("PowerOn", value);

                _Dispatch(value ? EVENT_RECEIVER_ON : EVENT_RECEIVER_OFF);
            }
            get => _receive;
        }

        [UdonSynced, FieldChangeCallback(nameof(Transmit))] private bool _transmit;
        private bool Transmit
        {
            set
            {
                if (Networking.IsOwner(gameObject))
                {
                    transmitter._SetActive(value);
                    if (exclusive && Receive) receiver._SetActive(!value);
                }
                SetBool("Talking", value);
                _transmit = value;

                _Dispatch(value ? EVENT_TRANSMITTER_ON : EVENT_TRANSMITTER_OFF);
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
            subscribers = new UdonSharpBehaviour[MAX_SUBSCRIBERS];

            var pickup = (VRCPickup)GetComponent(typeof(VRCPickup));
            if (pickup != null) pickup.AutoHold = VRC_Pickup.AutoHoldMode.Yes;

            if (frequencyText && !overrideFrequencyFormat) frequencyFormat = frequencyText.text;

            Frequency = frequency;
            Receive = false;
            Transmit = false;
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
            if (frequencyText != null) frequencyText.text = string.Format(frequencyFormat, frequency);
        }

        public void _Subscribe(UdonSharpBehaviour subscriber)
        {
            for (var i = 0; i < MAX_SUBSCRIBERS; i++)
            {
                if (!subscribers[i])
                {
                    subscribers[i] = subscriber;
                    return;
                }
            }
        }

        public void _Dispatch(string eventName)
        {
            foreach (var subscriber in subscribers)
            {
                if (!subscriber) return;
                subscriber.SendCustomEvent(eventName);
            }
        }
    }
}

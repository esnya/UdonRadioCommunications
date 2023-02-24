using SaccFlightAndVehicles;
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace URC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DFUNC_URC_Frequency : UdonSharpBehaviour
    {
        public GameObject Dial_Funcon;
        public KeyCode desktopDecrementKey = KeyCode.Comma;
        public KeyCode desktopIncrementKey = KeyCode.Period;
        public float controllerSensitivity = 100;
        public TextMeshPro frequencyText;
        public string frequencyTextFormat = "COM 000.000 '[,.]'";
        private float minFrequency, maxFrequency, frequencyStep;

        public Transform debugTransform;

        private float Frequency
        {
            get => transmitter.Frequency;
            set
            {
                transmitter._SetFrequency(value);
                receiver.Frequency = value;
                _UpdateFrequencyText();
                if (switchFunctionSound) switchFunctionSound.Play();
            }
        }

        private string triggerAxis;
        private VRCPlayerApi.TrackingDataType trackingTarget;
        public void DFUNC_LeftDial()
        {
            triggerAxis = "Oculus_CrossPlatform_PrimaryIndexTrigger";
            trackingTarget = VRCPlayerApi.TrackingDataType.LeftHand;
        }
        public void DFUNC_RightDial()
        {
            triggerAxis = "Oculus_CrossPlatform_SecondaryIndexTrigger";
            trackingTarget = VRCPlayerApi.TrackingDataType.RightHand;
        }

        private AudioSource switchFunctionSound;
        private Transform controlsRoot;
        private Receiver receiver;
        private Transmitter transmitter;
        public void SFEXT_L_EntityStart()
        {
            var entity = GetComponentInParent<SaccEntity>();
            switchFunctionSound = entity.SwitchFunctionSound;

            var airVehicle = entity.GetExtention(GetUdonTypeName<SaccAirVehicle>());

            controlsRoot = (Transform)airVehicle.GetProgramVariable("ControlsRoot") ?? entity.transform;

            var receiverDFunc = transform.parent.GetComponentInChildren<DFUNC_URC_RX>(true);
            receiver = receiverDFunc.receiver;

            var transmitterDFunc = transform.parent.GetComponentInChildren<DFUNC_URC_PTT>(true);
            transmitter = transmitterDFunc.transmitter;

            if (!frequencyText && Dial_Funcon)
            {
                frequencyText = Dial_Funcon.transform.parent.GetComponent<TextMeshPro>();
            }
        }
        public void SFEXTP_L_EntityStart() => SFEXT_L_EntityStart();

        public void SFEXT_O_PilotEnter()
        {
            var urc = transmitter.urc;
            minFrequency = urc.minFrequency;
            maxFrequency = urc.maxFrequency;
            frequencyStep = urc.frequencyStep;

            Frequency = transmitter.Frequency;
            if (!Networking.LocalPlayer.IsUserInVR()) DFUNC_Selected();
        }
        public void SFEXT_O_PilotExit()
        {
            DFUNC_Deselected();
        }
        public void SFEXTP_O_UserEnter() => SFEXT_O_PilotEnter();
        public void SFEXTP_O_UserExit() => SFEXT_O_PilotExit();

        public void DFUNC_Selected()
        {
            gameObject.SetActive(true);
            prevTriggered = false;
        }
        public void DFUNC_Deselected()
        {
            gameObject.SetActive(false);
        }

        private float inputOrigin;
        private bool prevTriggered;
        private void LateUpdate()
        {
            if (Input.GetKeyDown(desktopDecrementKey)) Frequency = Mathf.Max(Frequency - frequencyStep, minFrequency);
            else if (Input.GetKeyDown(desktopIncrementKey)) Frequency = Mathf.Min(Frequency + frequencyStep, maxFrequency);
            else
            {
                var trigger = Input.GetAxisRaw(triggerAxis) > 0.75f || debugTransform != null;
                if (trigger)
                {
                    var inputPos = controlsRoot.InverseTransformPoint(debugTransform ? debugTransform.position : Networking.LocalPlayer.GetTrackingData(trackingTarget).position).z;
                    if (!prevTriggered)
                    {
                        inputOrigin = inputPos;
                    }
                    else
                    {
                        var diff = (inputPos - inputOrigin) * controllerSensitivity * frequencyStep;
                        if (Mathf.Abs(diff) > frequencyStep)
                        {
                            Frequency = Mathf.Clamp(Mathf.Floor((Frequency + diff) / frequencyStep) * frequencyStep, minFrequency, maxFrequency);
                            inputOrigin = inputPos;
                        }
                    }
                }
                prevTriggered = trigger;
            }
        }

        public void _UpdateFrequencyText()
        {
            frequencyText.text = Frequency.ToString(frequencyTextFormat);
        }
    }
}

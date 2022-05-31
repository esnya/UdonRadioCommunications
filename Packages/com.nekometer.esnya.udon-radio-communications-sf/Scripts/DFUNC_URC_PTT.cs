using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace URC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DFUNC_URC_PTT : UdonSharpBehaviour
    {
        public GameObject Dial_Funcon;
        public KeyCode desktopKey = KeyCode.P;
        public Transmitter transmitter;

#if URC_SF
        private string triggerAxis;
        public void DFUNC_LeftDial()
        {
            triggerAxis = "Oculus_CrossPlatform_PrimaryIndexTrigger";
            // trackingTarget = VRCPlayerApi.TrackingDataType.LeftHand;
        }
        public void DFUNC_RightDial()
        {
            triggerAxis = "Oculus_CrossPlatform_SecondaryIndexTrigger";
            // trackingTarget = VRCPlayerApi.TrackingDataType.RightHand;
        }

        private SaccEntity CommonInit()
        {
            var entity = GetComponentInParent<SaccEntity>();

            transmitter.indicator = Dial_Funcon;
            transmitter.statusIndicator = Dial_Funcon;

            return entity;
        }

        public void SFEXT_L_EntityStart()
        {
            var entity = CommonInit();
            foreach (var seat in entity.gameObject.GetComponentsInChildren<SaccVehicleSeat>())
            {
                if (seat.IsPilotSeat)
                {
                    transmitter.transform.SetParent(entity.transform, true);
                    break;
                }
            }
        }
        public void SFEXTP_L_EntityStart()
        {
            var entity = CommonInit();
            var seat = gameObject.GetComponentInParent<SaccVehicleSeat>();
            transmitter.transform.SetParent(seat ? seat.transform : entity.transform, true);
        }

        public void SFEXT_O_PilotEnter()
        {
            transmitter._Deactivate();
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
        }
        public void DFUNC_Deselected()
        {
            transmitter._Deactivate();
            gameObject.SetActive(false);
        }

        private bool prevInput;
        private void Update()
        {
            var input = GetInput();
            if (input != prevInput)
            {
                transmitter._SetActive(input);
            }
            prevInput = input;
        }

        private bool GetInput()
        {
            return Input.GetKey(desktopKey) || Input.GetAxisRaw(triggerAxis) > 0.75f;
        }
#endif
    }
}

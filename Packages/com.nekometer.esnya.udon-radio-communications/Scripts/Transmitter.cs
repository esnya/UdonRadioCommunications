using System;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDKBase;

namespace URC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class Transmitter : UdonSharpBehaviour
    {
        public float deactivateDelay = 1.0f;
        public float minDistance = 5.0f;
        public GameObject indicator;
        public GameObject statusIndicator;
        public Material statusInactive, statusActive, statusDeactivating;
        public bool indicatorAsLocal = false;

        [NonSerialized][UdonSynced] public float frequency;
        [NonSerialized] public UdonRadioCommunication urc;

        private float lastActivatedTime;
        [UdonSynced][FieldChangeCallback(nameof(Active))] private bool _active;
        public bool Active
        {
            get => _active;
            set {
                if (value) lastActivatedTime = Time.time;
                _active = value;
                if (indicator != null) indicator.SetActive((!indicatorAsLocal || Networking.IsOwner(gameObject)) && value);
                SetIndicatorMateial(value ? statusActive : statusInactive);
            }
        }

        private void Start()
        {
            Active = false;
        }

        public void _Initialize(UdonRadioCommunication urc)
        {
            this.urc = urc;
            frequency = urc.defaultFrequency;
        }

        public void _TakeOwnership()
        {
            if (Networking.IsOwner(gameObject)) return;
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }

        public void _SetActive(bool value)
        {
            if (value) _Activate();
            else _Deactivate();
        }
        public void _Activate()
        {
            _TakeOwnership();
            Active = true;
            RequestSerialization();
        }
        public void _Deactivate()
        {
            _TakeOwnership();
            if (Active) SetIndicatorMateial(statusDeactivating);
            SendCustomEventDelayedSeconds(nameof(_DelayedDeactivate), deactivateDelay);
        }
        public void _DelayedDeactivate()
        {
            if (Time.time - lastActivatedTime < deactivateDelay) return;
            Active = false;
            RequestSerialization();
        }
        public void _ToggleActive() => _SetActive(!Active);

        public void _SetFrequency(float f)
        {
            _TakeOwnership();
            frequency = f;
            RequestSerialization();
        }

        public float _GetFrequency() => frequency;

        private void SetIndicatorMateial(Material material)
        {
            if (!statusIndicator || !material) return;
            foreach (var renderer in statusIndicator.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer) continue;
                 renderer.sharedMaterial = material;
            }
            foreach (var image in statusIndicator.GetComponentsInChildren<Image>(true))
            {
                if (!image) continue;
                 image.material = material;
            }
        }
    }
}

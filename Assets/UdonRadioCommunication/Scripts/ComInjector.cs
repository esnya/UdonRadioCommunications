using System;
using UnityEngine;
using UdonSharp;

namespace UdonRadioCommunication
{

    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class ComInjector : UdonSharpBehaviour
    {
        public Transform[] targets = {}, parents = {};

        private void Start()
        {
            var length = Mathf.Min(targets.Length, parents.Length);
            for (var i = 0; i < length; i++)
            {
                var target = targets[i];
                var parent = parents[i];
                if (target == null || parent == null) continue;

                target.SetParent(parent, true);
            }
        }
    }
}

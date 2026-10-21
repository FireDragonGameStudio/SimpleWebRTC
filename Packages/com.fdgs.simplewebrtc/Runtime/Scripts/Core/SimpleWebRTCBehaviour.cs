using System;
using UnityEngine;

namespace SimpleWebRTC {
    /// <summary>
    /// Marks a parameterless public method to be shown as a button in the inspector while in Play mode.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class InspectorButtonAttribute : Attribute {
        public string Label { get; }

        public InspectorButtonAttribute(string label = null) {
            Label = label;
        }
    }

    /// <summary>
    /// Base class for SimpleWebRTC components. Only used to share the inspector (Play mode buttons).
    /// </summary>
    public abstract class SimpleWebRTCBehaviour : MonoBehaviour {
    }
}

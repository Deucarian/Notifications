using System;
using Deucarian.Editor;
using UnityEditor;

namespace Deucarian.Notifications.Editor
{
    [CustomPropertyDrawer(typeof(NotificationKey), true)]
    public sealed class NotificationKeyDrawer : DeucarianKeyDrawer
    {
        public override Type KeyType => typeof(NotificationKey);
        public override Type DefinitionSetAttribute => typeof(NotificationKeySetAttribute);
        public override string SetupHint => "Select an existing NotificationKey; declare reusable keys once in a [NotificationKeySet] class.";
    }
}

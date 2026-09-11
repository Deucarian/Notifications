using System;

namespace Deucarian.Notifications
{
    /// <summary>Marks an authoritative set of named NotificationKey fields or properties for the Inspector.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class NotificationKeySetAttribute : Attribute { }
}

using System;
using UnityEngine;
using Deucarian.Notifications.Unity;
namespace Deucarian.Notifications.Samples.DefinitionWorkflow
{
    /// <summary>Small caller example. The configured scene hosts own services and resource lifetimes.</summary>
    public sealed class NotificationsWorkflow : MonoBehaviour
    {
        [SerializeField] private NotificationKey notification;
        [SerializeField] private NotificationTrigger trigger;
        private string status = "Ready. Choose an action below.";
        public string Status => status;
        public UnityEngine.Events.UnityEvent showEvent = new UnityEngine.Events.UnityEvent();
        public UnityEngine.Events.UnityEvent resolveEvent = new UnityEngine.Events.UnityEvent();
        public void Show() { NotificationManager.Show(notification); status = "Warning active. Repeated calls update the same warning."; }
        public void Resolve() { NotificationManager.Resolve(notification); status = "Warning resolved."; }
        public void ShowComponent() { trigger.Show(); status = "Shown through NotificationTrigger using the same definition."; }
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(24, 24, Math.Min(540, Screen.width - 48), Screen.height - 48), GUI.skin.box);
            GUILayout.Label("Notifications — definition workflow");
            GUILayout.Label("Create or edit the notification in Definitions or the Notification Lab. Its title, message and audio policy are reused here.");
            GUILayout.Space(12);
            if (GUILayout.Button("Show with C#", GUILayout.Height(32))) { try { Show(); } catch (Exception error) { status = error.Message; } }
            if (GUILayout.Button("Resolve with C#", GUILayout.Height(32))) { try { Resolve(); } catch (Exception error) { status = error.Message; } }
            if (GUILayout.Button("Show with component", GUILayout.Height(32))) { try { ShowComponent(); } catch (Exception error) { status = error.Message; } }
            GUILayout.Space(12);
            if (GUILayout.Button("Show via Inspector event", GUILayout.Height(32))) showEvent.Invoke();
            if (GUILayout.Button("Resolve via Inspector event", GUILayout.Height(32))) resolveEvent.Invoke();
            GUILayout.Label(status);
            GUILayout.EndArea();
        }
    }
}

using System;
using System.Collections.Generic;
using Deucarian.Editor;
using Deucarian.Notifications.Unity;
using UnityEditor;
using UnityEngine;

namespace Deucarian.Notifications.Editor
{
    public sealed partial class DeucarianNotificationLabWindow
    {
        [SerializeField] private NotificationLifetimeKind lifetimeKind;
        [SerializeField] private float lifetimeSeconds = 5f;
        [SerializeField] private NotificationPresentationSettings presentationSettings = NotificationPresentationSettings.Default;

        private NotificationLifetime Lifetime() => lifetimeKind == NotificationLifetimeKind.Timed
            ? NotificationLifetime.Timed(Math.Max(0.1, lifetimeSeconds)) : NotificationLifetime.UntilResolved;

        private void DrawPresentation()
        {
            DeucarianEditorCards.BeginCard("Presentation", subtitle: "Five visible by default; overflow remains active. Motion never resolves a message.");
            EditorGUI.BeginChangeCheck();
            presentationSettings.maxVisible = EditorGUILayout.IntSlider("Maximum visible", presentationSettings.Sanitized().maxVisible, 1, 20);
            presentationSettings.show = (NotificationTransition)EditorGUILayout.EnumPopup("Show transition", presentationSettings.show);
            presentationSettings.hide = (NotificationTransition)EditorGUILayout.EnumPopup("Hide transition", presentationSettings.hide);
            presentationSettings.showSeconds = EditorGUILayout.Slider("Show duration", presentationSettings.showSeconds, 0, 2);
            presentationSettings.hideSeconds = EditorGUILayout.Slider("Hide duration", presentationSettings.hideSeconds, 0, 2);
            if (EditorGUI.EndChangeCheck()) runtimeConnection?.ConfigurePresentation(presentationSettings);
            EditorGUILayout.LabelField(runtimeConnection == null
                    ? "Choose a runtime destination to preview motion and the application's visual theme. None disables motion."
                    : runtimeConnection.SupportsPresentation
                        ? "Live overrides affect this list only and are restored on disconnect. Colors and typography follow its Deucarian theme."
                        : "This custom view does not expose presentation settings. Its host controls layout and motion.",
                EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("Timed messages expire from activation, even in overflow. Until Resolved messages never expire automatically.",
                EditorStyles.wordWrappedLabel);
            DeucarianEditorCards.EndCard();
        }

        private void ShowMixed()
        {
            var definitions = new List<NotificationDefinition>();
            for (int i = 0; i < 10; i++)
            {
                int number = ++nextCustomId;
                var kind = (NotificationSeverity)(i % 4);
                bool timed = i % 2 == 0;
                definitions.Add(new NotificationDefinition("lab.mixed." + number, kind,
                    "Test " + number + " · " + kind, timed ? "This notice expires automatically." : "Resolve this notice to simulate recovery.",
                    (int)kind * 10, NotificationLabSession.FeedbackRole(kind),
                    timed ? NotificationLifetime.Timed(Math.Max(0.1, lifetimeSeconds)) : NotificationLifetime.UntilResolved));
            }
            session.ShowBatch(definitions, Timing());
        }

        private bool HasTimedMessages()
        {
            foreach (NotificationItem item in snapshot.Items)
                if (item.Definition.Lifetime.Kind == NotificationLifetimeKind.Timed) return true;
            return false;
        }

        private static string LifetimeLabel(NotificationItem item) => item.Definition.Lifetime.Kind == NotificationLifetimeKind.Timed
            ? "Timed · " + Math.Max(0, item.Definition.Lifetime.Seconds - (EditorApplication.timeSinceStartup - item.ActivatedAtSeconds)).ToString("0.0") + "s left"
            : "Until resolved";
    }
}

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

        internal void SaveAppearance()
        {
            NotificationPrefabSelection.SavePresentation(presentationSettings);
            runtimeConnection?.ConfigurePresentation(presentationSettings);
            NotificationLabRecipeStorage.SaveDraft(CaptureDraft());
            workspace?.Refresh();
        }

        internal bool HasSavedAppearance => NotificationViewSettings.Load()?.HasPresentation == true &&
            NotificationViewSettings.Load().Presentation.Equals(presentationSettings.Sanitized());

        private NotificationLifetime Lifetime() => lifetimeKind == NotificationLifetimeKind.Timed
            ? NotificationLifetime.Timed(Math.Max(0.1, lifetimeSeconds)) : NotificationLifetime.UntilResolved;

        internal void ShowMixed()
        {
            if (session == null) return;
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

    }
}

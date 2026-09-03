using System;
using Deucarian.Theming;

namespace Deucarian.Notifications.Theming
{
    /// <summary>Resolves notification feedback through the active audio theme.</summary>
    public sealed class ThemedNotificationFeedbackSink : INotificationFeedbackSink
    {
        private readonly DeucarianThemeAudioPlayer player;

        public ThemedNotificationFeedbackSink(DeucarianThemeAudioPlayer player)
        {
            this.player = player ?? throw new ArgumentNullException(nameof(player));
        }

        public bool TryRequestFeedback(NotificationFeedbackRequest request)
        {
            return player.PlayRoleById(request.RoleId);
        }
    }
}


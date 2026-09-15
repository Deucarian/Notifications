using System.Collections.Generic;

namespace Deucarian.Notifications.Tests
{
    /// <summary>Explicit fixture registrations; unknown IDs are never synthesized by lookup.</summary>
    internal sealed class RegisteredTestDefinitions : INotificationDefinitions
    {
        private readonly Dictionary<string, NotificationDefinition> definitions = new Dictionary<string, NotificationDefinition>();
        internal RegisteredTestDefinitions(params string[] ids)
        {
            foreach (string id in ids) definitions.Add(id, new NotificationDefinition(id,
                NotificationSeverity.Warning, id, "Registered fixture", feedbackRoleId: "deucarian.feedback.audio.warning"));
        }
        public bool TryGet(NotificationKey key, out NotificationDefinition definition) => definitions.TryGetValue(key.Id, out definition);
    }
}

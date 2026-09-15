using System.Collections.Generic;

namespace Deucarian.Notifications.PlayModeTests
{
    internal sealed class RegisteredTestDefinitions : INotificationDefinitions
    {
        private readonly Dictionary<string, NotificationDefinition> definitions = new Dictionary<string, NotificationDefinition>();
        internal RegisteredTestDefinitions(params string[] ids)
        {
            foreach (string id in ids) definitions.Add(id, new NotificationDefinition(id, NotificationSeverity.Warning, id, "Registered fixture"));
        }
        public bool TryGet(NotificationKey key, out NotificationDefinition definition) => definitions.TryGetValue(key.Id, out definition);
    }
}

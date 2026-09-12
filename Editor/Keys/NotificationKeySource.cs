using System;
using Deucarian.Editor;
using Deucarian.Notifications.Unity;

namespace Deucarian.Notifications.Editor
{
    public sealed class NotificationKeySource : DeucarianAssetKeySource<NotificationDefinitionAsset>
    {
        public override Type KeyType => typeof(NotificationKey);
        public override Type DefinitionSetAttribute => typeof(NotificationKeySetAttribute);
        public override string GeneratedClassName => "ProjectNotifications";
        protected override DeucarianKeyChoice ReadDefinition(NotificationDefinitionAsset asset) => new DeucarianKeyChoice(asset.Id, asset.DisplayName);
    }
}

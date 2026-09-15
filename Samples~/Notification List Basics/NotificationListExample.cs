using UnityEngine;
using Deucarian.Notifications.Unity;

namespace Deucarian.Notifications.Samples.Basic
{
    public sealed class NotificationListExample : MonoBehaviour
    {
        [SerializeField] private NotificationListView listView;
        private NotificationDefinition ExampleWarning => NotificationDefinitions.Require(catalog, ExampleKeys.Connection);
        private NotificationDefinition TimedNotice => NotificationDefinitions.Require(catalog, ExampleKeys.Saved);
        private NotificationCatalogAsset catalog;

        private NotificationStore store;
        private NotificationPresenter presenter;
        private NotificationEpisodeController episodes;


        public NotificationStore Store => store;

        public void ShowWarning()
        {
            episodes?.EvaluateBatch(new[] { new NotificationConditionSample(ExampleWarning, true, new NotificationTimingPolicy(0, 0)) });
        }

        public void ResolveWarning()
        {
            episodes?.EvaluateBatch(new[] { new NotificationConditionSample(ExampleWarning, false, new NotificationTimingPolicy(0, 0)) });
        }

        public void ShowTimedNotice() => episodes?.EvaluateBatch(
            new[] { new NotificationConditionSample(TimedNotice, true, new NotificationTimingPolicy(0, 0)) });

        private void Update() => episodes?.Tick();

        private void OnEnable()
        {
            catalog = Resources.Load<NotificationCatalogAsset>(NotificationCatalogAsset.DefaultResourcePath);
            store = new NotificationStore(definitions: catalog);
            episodes = new NotificationEpisodeController(store, new UnityUnscaledNotificationClock());
            if (listView != null)
            {
                presenter = new NotificationPresenter(store, listView);
                presenter.Activate();
            }
        }

        private void OnDisable()
        {
            presenter?.Dispose();
            presenter = null;
            episodes?.Dispose();
            episodes = null;
            store?.Dispose();
            store = null;
        }
    }
}

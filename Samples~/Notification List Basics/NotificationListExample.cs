using UnityEngine;
using Deucarian.Notifications.Unity;

namespace Deucarian.Notifications.Samples.Basic
{
    public sealed class NotificationListExample : MonoBehaviour
    {
        [SerializeField] private NotificationListView listView;
        private static readonly NotificationDefinition ExampleWarning =
            new NotificationDefinition(
                "sample.connection.lost",
                NotificationSeverity.Warning,
                "Connection lost",
                "Please reconnect",
                100,
                "deucarian.feedback.audio.warning");

        private NotificationStore store;
        private NotificationPresenter presenter;

        public NotificationStore Store => store;

        public void ShowWarning()
        {
            store?.ApplyBatch(
                new[] { NotificationCommand.Activate(ExampleWarning) },
                Time.realtimeSinceStartupAsDouble);
        }

        public void ResolveWarning()
        {
            store?.ApplyBatch(
                new[] { NotificationCommand.Resolve(ExampleWarning.Id) },
                Time.realtimeSinceStartupAsDouble);
        }

        private void OnEnable()
        {
            store = new NotificationStore();
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
            store?.Dispose();
            store = null;
        }
    }
}

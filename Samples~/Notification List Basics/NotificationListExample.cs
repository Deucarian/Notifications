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
        private NotificationEpisodeController episodes;
        private static readonly NotificationDefinition TimedNotice = new NotificationDefinition(
            "sample.saved", NotificationSeverity.Success, "Saved", "This notice expires after five seconds.",
            10, "deucarian.feedback.audio.success", NotificationLifetime.Timed(5));

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
            store = new NotificationStore();
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

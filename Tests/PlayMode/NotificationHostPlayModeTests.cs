using System.Collections;
using Deucarian.Notifications.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Deucarian.Notifications.PlayModeTests
{
    public sealed class NotificationHostPlayModeTests
    {
        [UnityTest]
        public IEnumerator ConfiguredHostOwnsTheOneLineWarningLifecycle()
        {
            var canvas = new GameObject("notifications", typeof(Canvas));
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var definition = ScriptableObject.CreateInstance<NotificationDefinitionAsset>();
            var catalog = ScriptableObject.CreateInstance<NotificationCatalogAsset>();
            try
            {
                JsonUtility.FromJsonOverwrite("{\"id\":\"connection.lost\"}", definition);
                typeof(NotificationCatalogAsset).GetField("definitions", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .SetValue(catalog, new[] { definition });
                var host = NotificationHost.Create((RectTransform)canvas.transform, catalog: catalog);
                var instance = host.gameObject;
                Assert.Throws<System.InvalidOperationException>(() => NotificationHost.Create((RectTransform)canvas.transform, catalog: catalog));
                Assert.That(canvas.GetComponentsInChildren<NotificationHost>(), Has.Length.EqualTo(1));
                NotificationManager.Warn(new NotificationHostPlayModeTestsKey("connection.lost"), "Connection lost", "Please reconnect your device.");
                yield return null;
                Assert.That(instance.GetComponent<NotificationListView>().VisibleCount, Is.EqualTo(1));
                NotificationManager.Resolve(new NotificationHostPlayModeTestsKey("connection.lost"));
                Assert.That(NotificationManager.Snapshot.Count, Is.Zero);
                instance.SetActive(false);
                Assert.That(NotificationManager.IsConfigured, Is.False);
                instance.SetActive(true);
                NotificationManager.EvaluateBatch(new[] { new NotificationCondition(new NotificationHostPlayModeTestsKey("connection.lost"), true) });
                Assert.That(NotificationManager.Snapshot.Count, Is.EqualTo(1));
            }
            finally { Object.DestroyImmediate(canvas); Object.DestroyImmediate(catalog); Object.DestroyImmediate(definition); }
        }
    }
}

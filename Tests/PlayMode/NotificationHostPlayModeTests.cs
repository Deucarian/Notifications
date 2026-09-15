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
            var instance = Object.Instantiate(Resources.Load<GameObject>(
                "Deucarian/Notifications/Defaults/DefaultNotificationList"), canvas.transform, false);
            var definition = ScriptableObject.CreateInstance<NotificationDefinitionAsset>();
            var catalog = ScriptableObject.CreateInstance<NotificationCatalogAsset>();
            try
            {
                instance.SetActive(false);
                JsonUtility.FromJsonOverwrite("{\"id\":\"connection.lost\"}", definition);
                typeof(NotificationCatalogAsset).GetField("definitions", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    .SetValue(catalog, new[] { definition });
                var host = instance.AddComponent<NotificationHost>();
                typeof(NotificationHost).GetField("catalog", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).SetValue(host, catalog);
                instance.SetActive(true);
                NotificationManager.Warn(new NotificationHostPlayModeTestsKey("connection.lost"), "Connection lost", "Please reconnect your device.");
                yield return null;
                Assert.That(instance.GetComponent<NotificationListView>().VisibleCount, Is.EqualTo(1));
                NotificationManager.Resolve(new NotificationHostPlayModeTestsKey("connection.lost"));
                Assert.That(NotificationManager.Snapshot.Count, Is.Zero);
                instance.SetActive(false);
                Assert.That(NotificationManager.IsConfigured, Is.False);
            }
            finally { Object.DestroyImmediate(canvas); Object.DestroyImmediate(catalog); Object.DestroyImmediate(definition); }
        }
    }
}

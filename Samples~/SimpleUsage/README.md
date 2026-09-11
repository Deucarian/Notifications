# Simple usage

Import TMP Essential Resources once when Unity prompts for text setup. The bundled list uses the shared Theming font.

Place the package's DefaultNotificationList prefab under your Canvas and add NotificationHost to the list. It owns the store, presenter, and unscaled scheduler while enabled. Add ThemeAudioHost once for shared themed feedback, or assign an existing DeucarianThemeAudioPlayer to NotificationHost. Configure the palette in the existing Theming settings. Ordinary callers only use the two methods below.

For separate notification areas, create a NotificationService per area and pass its view at construction. Only the application's default host registers with NotificationManager. Duplicate default registrations throw. A registration borrows its service; it never disposes that service. Repeating an ID updates the existing episode; Resolve removes it and cancels pending activation. Show(NotificationDefinition) remains available for reusable definitions and timed notices.

Import the **Simple Usage** sample from Unity Package Manager. Its caller script is:

```csharp
using UnityEngine;

namespace Deucarian.Notifications.Samples.SimpleUsage
{
    public sealed class SimpleUsageExample : MonoBehaviour
    {
        public void ShowWarning() => NotificationManager.Warn("connection.lost", "Connection lost", "Please reconnect your device.");
        public void ResolveWarning() => NotificationManager.Resolve("connection.lost");
    }
}
```

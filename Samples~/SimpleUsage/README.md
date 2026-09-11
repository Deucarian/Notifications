# Simple usage

Import TMP Essential Resources once when Unity prompts for text setup. The bundled list uses the shared Theming font.

Place the package's DefaultNotificationList prefab under your Canvas and add NotificationHost to the list. It owns the store, presenter, and unscaled scheduler while enabled. Add ThemeAudioHost once for shared themed feedback, or assign an existing DeucarianThemeAudioPlayer to NotificationHost. Configure the palette in the existing Theming settings. Ordinary callers only use the two methods below.

For separate notification areas, create a NotificationService per area and pass its view at construction. Only the application's default host registers with NotificationManager. Duplicate default registrations throw. A registration borrows its service; it never disposes that service. Repeating an ID updates the existing episode; Resolve removes it and cancels pending activation. Show(NotificationDefinition) remains available for reusable definitions and timed notices.

Import the **Simple Usage** sample from Unity Package Manager. Its caller script is:

Definition fields now use named, domain-specific keys. Select an existing definition from the Inspector dropdown or pass the same named key in code. Declare each project key once in a marked key set; ordinary caller methods do not accept raw IDs. Generated keys for asset-authored definitions require no asset reference in the caller. Owner-issued selection and row handles represent runtime instances.

Warn requests warning audio automatically. Repeating an active key updates the warning without replaying its activation sound; resolving and later reactivating it starts a new warning episode. Configure the host audio player or the shared ThemeAudioHost once. Do not add ThemeAudio.Play to each warning call.

```csharp
using UnityEngine;

namespace Deucarian.Notifications.Samples.SimpleUsage
{
    public sealed class SimpleUsageExample : MonoBehaviour
    {
        [SerializeField] private NotificationKey warning = NotificationKeys.ConnectionLost;

        public void ShowWarning() => NotificationManager.Warn(warning, "Connection lost", "Please reconnect your device.");
        public void ResolveWarning() => NotificationManager.Resolve(warning);
    }
}
```

namespace Deucarian.Notifications
{
    /// <summary>Explicit advanced per-activation changes. Null inherits the reusable definition.</summary>
    public sealed class NotificationContentOverrides
    {
        public NotificationContentOverrides(string title = null, string message = null, string feedbackRoleId = null)
        { Title = title; Message = message; FeedbackRoleId = feedbackRoleId; }
        public string Title { get; }
        public string Message { get; }
        public string FeedbackRoleId { get; }
    }
}

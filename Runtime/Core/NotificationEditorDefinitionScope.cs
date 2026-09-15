#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace Deucarian.Notifications
{
    /// <summary>Internal Lab-owned temporary declarations. No equivalent is compiled into players.</summary>
    internal sealed class NotificationEditorDefinitionScope : IDisposable
    {
        private readonly NotificationStore store;
        private readonly HashSet<NotificationId> ids = new HashSet<NotificationId>();
        private bool disposed;

        internal NotificationEditorDefinitionScope(NotificationStore store) { this.store = store; }
        internal IEnumerable<NotificationId> Ids => ids;
        internal bool Contains(NotificationId id) => !disposed && ids.Contains(id);

        internal void Register(NotificationDefinition definition)
        {
            if (disposed) throw new ObjectDisposedException(nameof(NotificationEditorDefinitionScope));
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            store.ValidateEditorRegistration(this, definition);
            ids.Add(definition.Id);
        }

        internal void Clear()
        {
            if (disposed) throw new ObjectDisposedException(nameof(NotificationEditorDefinitionScope));
            ids.Clear();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            store.RemoveEditorScope(this);
            ids.Clear();
        }
    }
}
#endif

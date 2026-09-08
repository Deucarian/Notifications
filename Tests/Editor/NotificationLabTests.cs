using System;
using System.Collections;
using System.Collections.Generic;
using Deucarian.Editor;
using Deucarian.Notifications.Editor;
using Deucarian.Theming;
using Deucarian.Theming.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Deucarian.Notifications.Tests
{
    public sealed class NotificationLabTests
    {
        private sealed class Clock : INotificationClock { public double NowSeconds { get; set; } }
        private sealed class Feedback : INotificationFeedbackSink
        {
            public int Count;
            public bool TryRequestFeedback(NotificationFeedbackRequest request) { Count++; return true; }
        }
        private sealed class Preview : IDeucarianAudioPreviewService
        {
            public bool IsAvailable => true;
            public bool IsPlaying { get; private set; }
            public int Plays;
            public int Stops;
            public AudioClip LastClip;
            public bool Play(AudioClip clip) { LastClip = clip; Plays++; IsPlaying = true; return true; }
            public void Stop() { Stops++; IsPlaying = false; }
        }

        private readonly List<Object> objects = new List<Object>();

        [TearDown]
        public void Cleanup()
        {
            for (int i = objects.Count - 1; i >= 0; i--) if (objects[i] != null) Object.DestroyImmediate(objects[i]);
            objects.Clear();
        }

        [Test]
        public void RepeatedMessageHasOneEpisodeAndOnePing()
        {
            var sink = new Feedback();
            using (var session = new NotificationLabSession(new Clock(), sink))
            {
                for (int i = 0; i < 10; i++) session.Show(Warning(), new NotificationTimingPolicy(0, 1));
                Assert.AreEqual(1, session.Store.Snapshot.Count);
                Assert.AreEqual(1, session.PingCount);
                Assert.AreEqual(1, sink.Count);
                Assert.AreEqual(1, session.LastBatchSize);
            }
        }

        [Test]
        public void SimultaneousExamplesShareOnePingAndOrderByPriority()
        {
            using (var session = new NotificationLabSession(new Clock(), new Feedback()))
            {
                session.ShowBatch(new[] { NotificationLabSession.Example(NotificationSeverity.Info), Warning(),
                    NotificationLabSession.Example(NotificationSeverity.Error) }, new NotificationTimingPolicy(0, 0));
                Assert.AreEqual(3, session.Store.Snapshot.Count);
                Assert.AreEqual(NotificationSeverity.Error, session.Store.Snapshot[0].Definition.Severity);
                Assert.AreEqual(1, session.PingCount);
                Assert.AreEqual(3, session.LastBatchSize);
                session.ResolveAll();
                Assert.AreEqual(0, session.Store.Snapshot.Count);
                Assert.AreEqual(1, session.PingCount);
            }
        }

        [Test]
        public void RecoveryWaitsAndRelapseKeepsTheEpisode()
        {
            var clock = new Clock();
            using (var session = new NotificationLabSession(clock, new Feedback()))
            {
                session.Show(Warning(), new NotificationTimingPolicy(0.75, 1));
                Assert.AreEqual(1, session.PendingCount);
                clock.NowSeconds = 0.74;
                session.Tick();
                Assert.AreEqual(0, session.Store.Snapshot.Count);
                clock.NowSeconds = 0.75;
                session.Tick();
                long episode = session.Store.Snapshot[0].Episode;
                session.Resolve(Warning().Id);
                clock.NowSeconds = 1;
                session.Show(Warning(), new NotificationTimingPolicy(0.75, 1));
                Assert.IsFalse(session.IsRecovering(Warning().Id));
                Assert.AreEqual(episode, session.Store.Snapshot[0].Episode);
                Assert.AreEqual(1, session.PingCount);
                session.ResolveAll();
                clock.NowSeconds = 1.99;
                session.Tick();
                Assert.AreEqual(1, session.Store.Snapshot.Count);
                clock.NowSeconds = 2;
                session.Tick();
                Assert.AreEqual(0, session.Store.Snapshot.Count);
                Assert.AreEqual(0, session.PendingCount);
            }
        }

        [Test]
        public void ResolveAllCancelsMessagesStillWaitingToAppear()
        {
            var clock = new Clock();
            using (var session = new NotificationLabSession(clock, new Feedback()))
            {
                session.Show(Warning(), new NotificationTimingPolicy(2, 1));
                session.ResolveAll();
                clock.NowSeconds = 10;
                session.Tick();
                Assert.AreEqual(0, session.Store.Snapshot.Count);
                Assert.AreEqual(0, session.PendingCount);
                Assert.AreEqual(0, session.PingCount);
            }
        }

        [Test]
        public void ResetClearsPendingWorkAndCountersAndDisposeIsIdempotent()
        {
            var clock = new Clock();
            var session = new NotificationLabSession(clock, new Feedback());
            session.Show(Warning(), new NotificationTimingPolicy(0, 1));
            session.Show(NotificationLabSession.Example(NotificationSeverity.Info), new NotificationTimingPolicy(2, 1));
            session.Reset();
            clock.NowSeconds = 10;
            session.Tick();
            Assert.AreEqual(0, session.Store.Snapshot.Count);
            Assert.AreEqual(0, session.PingCount);
            Assert.AreEqual(0, session.LastBatchSize);
            session.Dispose();
            session.Dispose();
            Assert.Throws<ObjectDisposedException>(() => session.Tick());
        }

        [Test]
        public void UpdatingContentDoesNotRepingAndOtherStoresRemainUntouched()
        {
            using (var other = new NotificationStore())
            using (var session = new NotificationLabSession(new Clock(), new Feedback()))
            {
                session.Show(Warning(), new NotificationTimingPolicy(0, 0));
                session.Show(new NotificationDefinition(Warning().Id, NotificationSeverity.Warning,
                    "Updated", "New body", 20, DeucarianBuiltinAudioRoleIds.Warning), new NotificationTimingPolicy(0, 0));
                Assert.AreEqual("Updated", session.Store.Snapshot[0].Definition.Title);
                Assert.AreEqual(1, session.PingCount);
                Assert.AreEqual(0, other.Snapshot.Count);
            }
        }

        [Test]
        public void PaletteExperienceSelectsClipAndChangingItStopsWithoutAutoplay()
        {
            var preview = new Preview();
            DeucarianAudioPaletteSet set = PaletteSet(out AudioClip fallback, out AudioClip xr);
            using (var audio = new NotificationLabAudio(preview))
            {
                audio.Configure(set, DeucarianAudioExperience.XR, true);
                Assert.AreEqual(0, preview.Plays);
                Assert.AreEqual(0, preview.Stops, "Opening an idle lab must not stop someone else's preview.");
                Assert.IsTrue(audio.TryRequestFeedback(Request()));
                Assert.AreSame(xr, preview.LastClip);
                audio.Configure(set, DeucarianAudioExperience.WebGL, true);
                Assert.AreEqual(1, preview.Stops);
                Assert.AreEqual(1, preview.Plays);
                Assert.IsTrue(audio.TryRequestFeedback(Request()));
                Assert.AreSame(fallback, preview.LastClip);
            }
            Assert.IsFalse(preview.IsPlaying);
        }

        [Test]
        public void MutedOrMissingAudioDoesNotBlockMessagesOrTheirPingCounter()
        {
            var preview = new Preview();
            using (var audio = new NotificationLabAudio(preview))
            using (var session = new NotificationLabSession(new Clock(), audio))
            {
                audio.Configure(null, DeucarianAudioExperience.XR, true);
                session.Show(Warning(), new NotificationTimingPolicy(0, 0));
                Assert.AreEqual(1, session.Store.Snapshot.Count);
                Assert.AreEqual(1, session.PingCount);
                Assert.AreEqual(0, preview.Plays);
                session.ResolveAll();
                audio.Configure(PaletteSet(out _, out _), DeucarianAudioExperience.XR, false);
                session.Show(Warning(), new NotificationTimingPolicy(0, 0));
                Assert.AreEqual(1, session.Store.Snapshot.Count);
                Assert.AreEqual(2, session.PingCount);
                Assert.AreEqual(0, preview.Plays);
            }
        }

        [Test]
        public void WindowLifecycleDoesNotCreateSceneObjectsOrKeepPendingMessages()
        {
            Scene scene = SceneManager.GetActiveScene();
            int roots = scene.rootCount;
            bool wasDirty = scene.isDirty;
            var window = Keep(ScriptableObject.CreateInstance<DeucarianNotificationLabWindow>());
            window.SessionForTests.Show(Warning(), new NotificationTimingPolicy(5, 1));
            window.TransitionForTests(PlayModeStateChange.ExitingEditMode);
            Assert.IsNull(window.SessionForTests);
            window.TransitionForTests(PlayModeStateChange.EnteredPlayMode);
            Assert.AreEqual(0, window.SessionForTests.Store.Snapshot.Count);
            Assert.AreEqual(0, window.SessionForTests.PendingCount);
            window.DisableForTests();
            Assert.AreEqual(roots, scene.rootCount);
            Assert.AreEqual(wasDirty, scene.isDirty);
        }

        [Test]
        public void LabIsDiscoverableUnderExperienceInControlCenter()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(
                typeof(NotificationsControlCenterRegistration).TypeHandle);
            Assert.IsTrue(DeucarianToolRegistry.TryGet(NotificationsControlCenterRegistration.ToolId, out var tool));
            Assert.AreEqual(DeucarianControlCenterArea.Experience, tool.Area);
            Assert.AreEqual("com.deucarian.notifications", tool.OwningPackage);
        }

        [UnityTest]
        public IEnumerator WindowRendersExampleMessagesAtCompactAndWideSizes()
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
                Assert.Ignore("Window rendering requires a graphics device; run this test without -nographics.");

            var window = Keep(ScriptableObject.CreateInstance<DeucarianNotificationLabWindow>());
            window.position = new Rect(0, 0, 560, 760);
            window.Show();
            window.SessionForTests.ShowBatch(new[] { Warning(), NotificationLabSession.Example(NotificationSeverity.Error) },
                new NotificationTimingPolicy(0, 0));
            window.Repaint();
            yield return null;
            yield return null;
            Assert.AreEqual(2, window.SessionForTests.Store.Snapshot.Count);
            window.position = new Rect(0, 0, 1060, 760);
            window.Repaint();
            yield return null;
            yield return null;
            window.SessionForTests.ResolveAll();
            window.Repaint();
            yield return null;
            window.Close();
        }

        private static NotificationDefinition Warning() => NotificationLabSession.Example(NotificationSeverity.Warning);
        private static NotificationFeedbackRequest Request() => new NotificationFeedbackRequest(
            DeucarianBuiltinAudioRoleIds.Warning, NotificationSeverity.Warning, 20, 1);

        private T Keep<T>(T value) where T : Object { objects.Add(value); return value; }

        private DeucarianAudioPaletteSet PaletteSet(out AudioClip fallback, out AudioClip xr)
        {
            fallback = Keep(AudioClip.Create("Default ping", 16, 1, 8000, false));
            xr = Keep(AudioClip.Create("XR ping", 16, 1, 8000, false));
            var role = Keep(ScriptableObject.CreateInstance<DeucarianAudioRole>());
            role.Configure(DeucarianBuiltinAudioRoleIds.Warning, "Warning", "Feedback", "Test role", DeucarianAudioCue.Empty, false);
            var library = Keep(ScriptableObject.CreateInstance<DeucarianAudioRoleLibrary>());
            library.AddRole(role);
            var defaultPalette = Keep(ScriptableObject.CreateInstance<DeucarianAudioPalette>());
            defaultPalette.Configure("lab.default", "Default", library);
            defaultPalette.SetCue(role, new DeucarianAudioCue(fallback));
            var xrPalette = Keep(ScriptableObject.CreateInstance<DeucarianAudioPalette>());
            xrPalette.Configure("lab.xr", "XR", library);
            xrPalette.SetCue(role, new DeucarianAudioCue(xr));
            var profile = new DeucarianAudioPaletteProfile();
            profile.Configure(DeucarianAudioExperience.XR, xrPalette);
            var set = Keep(ScriptableObject.CreateInstance<DeucarianAudioPaletteSet>());
            set.Configure(defaultPalette, new[] { profile });
            return set;
        }
    }
}

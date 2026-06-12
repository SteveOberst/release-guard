using System;
using System.Collections.Generic;
using AttackSurfaceFixture.Game.Core;
using ReflectionUtility;
using UnityEngine;

namespace AttackSurfaceFixture.Game.Runtime
{
    /// <summary>
    /// Loads server-specified event-responder classes by type name using
    /// <see cref="TypeBinder"/>, and activates them at runtime.
    ///
    /// <b>This is the canonical example of when <c>[Preserve]</c> IS required.</b>
    ///
    /// The type names come from <c>RemoteConfigClient</c> at runtime  -- they are opaque
    /// strings from the linker's perspective. If managed stripping removes
    /// <c>BonusRewardResponder</c> because nothing in the compiled call graph references
    /// it directly, <c>TypeBinder.FindType</c> will return null and the feature
    /// silently breaks in the release build.
    ///
    /// The fix is targeted <c>[Preserve]</c> on each concrete responder class.
    /// This is the <em>narrow</em>, correct use of the attribute  -- one class, one reason  --
    /// not <c>[assembly: Preserve]</c> which disables all stripping for the whole assembly
    /// and negates much of the build-size and security benefit that stripping provides.
    ///
    /// <b>Contrast with <c>BuffEffectRegistry</c></b>, which does <em>not</em> need
    /// <c>[Preserve]</c> because each effect is directly instantiated in the registry's
    /// static constructor and therefore visible to the linker.
    /// </summary>
    public sealed class EventResponderHost : MonoBehaviour
    {
        // The RemoteConfig key whose value is a comma-separated list of fully-qualified
        // type names, e.g. "AttackSurfaceFixture.Game.Runtime.BonusRewardResponder".
        private const string ConfigKey = "active_event_responders";

        private readonly List<IEventResponder> _active = new List<IEventResponder>();

        private void Start()
        {
            ServiceLocator.TryGet<RemoteConfigKit.RemoteConfigClient>(out var rc);
            var csv = rc != null ? rc.GetValue(ConfigKey, string.Empty) : string.Empty;

            if (string.IsNullOrWhiteSpace(csv)) return;

            foreach (var typeName in csv.Split(','))
            {
                TryActivate(typeName.Trim());
            }
        }

        private void OnDestroy()
        {
            foreach (var r in _active)
            {
                try
                {
                    r.Deactivate();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            _active.Clear();
        }

        private void TryActivate(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return;

            // TypeBinder scans AppDomain.CurrentDomain.GetAssemblies() for the type.
            // This only succeeds under IL2CPP if the target type carries [Preserve].
            var type = TypeBinder.FindType(typeName);
            if (type == null)
            {
                Debug.LogWarning(
                    $"[EventResponderHost] Type '{typeName}' requested by server was not found. " +
                    "If this is a release build, the type may have been removed by managed stripping. " +
                    "Add [UnityEngine.Scripting.Preserve] to the class declaration.");
                return;
            }

            if (!typeof(IEventResponder).IsAssignableFrom(type))
            {
                Debug.LogWarning($"[EventResponderHost] '{typeName}' does not implement IEventResponder.");
                return;
            }

            try
            {
                var responder = (IEventResponder)Activator.CreateInstance(type);
                responder.Activate();
                _active.Add(responder);
                Debug.Log($"[EventResponderHost] Activated '{type.Name}'.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EventResponderHost] Failed to activate '{typeName}': {ex.Message}");
            }
        }
    }
}
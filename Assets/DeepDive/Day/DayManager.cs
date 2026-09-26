using DeepDive.Core.Contracts;
using UnityEngine;

namespace DeepDive.Day
{
    // Thin host-side shell around DayEngine: it owns the engine's lifetime and feeds it host time.
    // Like BoatTripManager it binds in an explicit Configure()/Shutdown() rather than OnEnable, since
    // plain MonoBehaviours do not receive lifecycle callbacks in EditMode tests.
    [DisallowMultipleComponent]
    public sealed class DayManager : MonoBehaviour
    {
        private const float PersistRetrySeconds = 2f;

        private System.Func<bool> _lock;
        private System.Func<CampaignDayState> _state;
        private bool _configured;
        private float _nextRetry;

        public DayEngine Engine { get; } = new DayEngine();

        public void Configure(IDayRoster roster, IDayCloseHooks hooks, int campaignSeed = 0)
        {
            Shutdown();
            Engine.Configure(roster, hooks, campaignSeed);
            _lock = () => Engine.IsLocked;
            DayLock.Bind(_lock);
            _state = () => Engine.State;
            DayLock.BindState(_state, () => Engine.LastSummary);
            _configured = true;
        }

        public void Shutdown()
        {
            if (_lock != null) DayLock.Unbind(_lock);
            if (_state != null) DayLock.UnbindState(_state);
            _lock = null;
            _state = null;
            _configured = false;
        }

        private void Update()
        {
            if (!_configured) return;

            switch (Engine.Phase)
            {
                case DayPhase.Running:
                    Engine.Tick(Time.unscaledDeltaTime);
                    break;
                case DayPhase.Summary:
                    // The write failed: same close id, same summary, try the write again.
                    if (Time.unscaledTime >= _nextRetry)
                    {
                        _nextRetry = Time.unscaledTime + PersistRetrySeconds;
                        Engine.RetryClose();
                    }
                    break;
                case DayPhase.Morning:
                    Engine.CompleteMorning();
                    break;
            }
        }

        private void OnDisable() => Shutdown();
        private void OnDestroy() => Shutdown();
    }
}

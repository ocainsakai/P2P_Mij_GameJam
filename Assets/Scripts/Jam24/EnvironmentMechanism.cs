using UnityEngine;

namespace Jam24
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class EnvironmentMechanism : MonoBehaviour
    {
        public MechanismType Type { get; private set; }
        public int State { get; private set; }
        public int RequiredState { get; private set; }
        public int StateCount { get; private set; }
        public bool TimingOnly { get; private set; }
        public bool IsCorrect => State == RequiredState;

        private SoleFlowPuzzle owner;
        private SpriteRenderer body;
        private TextMesh label;
        private float pulseOverrideUntil;

        public void Initialize(SoleFlowPuzzle puzzle, MechanismSpec spec, Sprite sprite)
        {
            owner = puzzle;
            Type = spec.type;
            State = spec.initialState;
            RequiredState = spec.requiredState;
            StateCount = spec.stateCount;
            TimingOnly = spec.timingOnly;
            body = GetComponent<SpriteRenderer>();
            body.sprite = sprite;
            label = GetComponentInChildren<TextMesh>();
            Refresh();
        }

        public void Cycle(bool countAction = true)
        {
            if (!owner.CanInteract(this)) return;
            int old = State;
            State = (State + 1) % StateCount;
            if (Type == MechanismType.PulseCurrent) pulseOverrideUntil = Time.unscaledTime + 1.25f;
            SoleAudio.Click();
            Refresh();
            owner.MechanismChanged(this, old, countAction);
        }

        public void SetState(int value)
        {
            State = Mathf.Clamp(value, 0, StateCount - 1);
            Refresh();
        }

        public void TickPulse(float time)
        {
            if (Type != MechanismType.PulseCurrent) return;
            if (time < pulseOverrideUntil) return;
            int next = Mathf.Sin(time * 2.2f) > 0f ? 1 : 0;
            if (next != State) { State = next; Refresh(); }
        }

        private void Refresh()
        {
            if (body == null) return;
            body.color = State == RequiredState ? Color.white : new Color(.48f,.53f,.56f,1f);
            transform.localRotation = Type == MechanismType.RotatingJet ? Quaternion.Euler(0,0, State * 45f) : Quaternion.identity;
            if (label != null) label.text = Icon(Type) + "\n" + StateName();
        }

        private string StateName()
        {
            if (Type == MechanismType.RotatingJet) return $"{State * 45}°";
            if (Type == MechanismType.RockDiverter) return State == 0 ? "SLOT A" : "SLOT B";
            if (Type == MechanismType.FlowDivider) return State == 0 ? "LOW" : "HIGH";
            if (Type == MechanismType.SeaweedGate) return State == 0 ? "CLOSED" : "OPEN";
            return State == 0 ? "OFF" : "ON";
        }

        private static string Icon(MechanismType type) => type switch
        {
            MechanismType.FixedCurrent => "CURRENT", MechanismType.RotatingJet => "JET", MechanismType.WaterValve => "VALVE",
            MechanismType.BubbleColumn => "BUBBLES", MechanismType.RockDiverter => "ROCK", MechanismType.BounceShell => "SHELL",
            MechanismType.SeaweedGate => "SEAWEED", MechanismType.FlowDivider => "DIVIDER", MechanismType.PulseCurrent => "PULSE",
            _ => "SWITCH"
        };

    }
}

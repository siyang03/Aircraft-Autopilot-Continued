// GPL-3.0-only.
using System;
namespace AircraftAutopilotContinued
{
    public sealed class CallbackOwnership<T> where T : class
    {
        public T Owner { get; private set; }
        public void Attach(T owner, Action<T> attach, Action<T> detach)
        {
            if (ReferenceEquals(Owner, owner)) return;
            Detach(detach);
            if (owner == null) return;
            Owner = owner;
            try { attach(owner); } catch { Detach(detach); throw; }
        }
        public void Detach(Action<T> detach)
        {
            T old = Owner; Owner = null;
            if (old != null) detach(old);
        }
    }
}

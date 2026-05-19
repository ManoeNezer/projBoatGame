using UnityEngine;

namespace BoatGame.Events
{
    public abstract class MaritimeEventBase
    {
        protected MaritimeEventManager Manager { get; private set; }
        protected Transform Target { get; private set; }
        protected Rigidbody TargetBody { get; private set; }
        protected Vector3 Origin { get; set; }
        protected float Duration { get; private set; }
        protected float Elapsed { get; private set; }

        public bool IsFinished => Elapsed >= Duration;
        public abstract string DisplayName { get; }

        public void Begin(MaritimeEventManager manager, Transform target, Rigidbody targetBody, Vector3 origin, float duration)
        {
            Manager = manager;
            Target = target;
            TargetBody = targetBody;
            Origin = origin;
            Duration = Mathf.Max(0.1f, duration);
            Elapsed = 0f;
            OnBegin();
        }

        public void Tick(float deltaTime)
        {
            Elapsed += Mathf.Max(0f, deltaTime);
            OnTick(deltaTime);
        }

        public void Finish()
        {
            OnFinish();
        }

        public virtual void DrawGizmos()
        {
        }

        protected float NormalizedTime => Mathf.Clamp01(Elapsed / Duration);
        protected float Bell01 => Mathf.Sin(NormalizedTime * Mathf.PI);

        protected virtual void OnBegin()
        {
        }

        protected abstract void OnTick(float deltaTime);

        protected virtual void OnFinish()
        {
        }
    }
}

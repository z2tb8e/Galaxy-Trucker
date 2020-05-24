using System;
using System.Collections.Generic;
using System.Threading;

namespace GalaxyTrucker.Model
{
    public abstract class CardEvent : IDisposable
    {
        private readonly AutoResetEvent _proceedEvent;
        private int _lastResolved;

        protected int LastResolved
        {
            get
            {
                return _lastResolved;
            }
            set
            {
                _lastResolved = value;
            }
        }
        
        public GameStage Stage { get; set; }

        public bool RequiresAttributes { get; protected set; }

        public bool RequiresOrder { get; protected set; }

        public event EventHandler<DiceRolledEventArgs> DiceRolled;

        public CardEvent(GameStage stage)
        {
            RequiresOrder = false;
            RequiresAttributes = false;
            _proceedEvent = new AutoResetEvent(false);
            Stage = stage;
        }

        public abstract bool IsResolved();

        public void ProceedCurrent()
        {
            _proceedEvent.Set();
        }

        public abstract override string ToString();

        public virtual string ToolTip() { return null; }

        public abstract string GetDescription();

        public virtual IEnumerable<OptionOrSubEvent> GetOptionsOrSubEvents() { return null; }

        public virtual void ApplyOption(Ship ship, int option) { }

        protected void OnDiceRolled(Projectile projectile, Direction direction, int number)
        {
            DiceRolled?.Invoke(this, new DiceRolledEventArgs(projectile, direction, number));
            _proceedEvent.WaitOne();
        }

        public void Dispose()
        {
            _proceedEvent.Dispose();
        }
    }
}

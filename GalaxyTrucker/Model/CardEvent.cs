using System;
using System.Collections.Generic;
using System.Threading;

namespace GalaxyTrucker.Model
{
    public abstract class CardEvent
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

        public event EventHandler<(int, int)> DiceRolled;

        public CardEvent(GameStage stage)
        {
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

        protected void OnDiceRolled(int roll1, int roll2)
        {
            DiceRolled?.Invoke(this, (roll1, roll2));
            _proceedEvent.WaitOne();
        }
    }
}

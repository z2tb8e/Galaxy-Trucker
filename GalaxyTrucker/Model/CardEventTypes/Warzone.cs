using System.Collections.Generic;
using System.Text;

namespace GalaxyTrucker.Model.CardEventTypes
{
    public class WarzoneEvent<T>
    {
        public CardCheckAttribute Attribute { get; }

        public CardEventPenalty PenaltyType { get; }

        public T Penalty { get; }

        public WarzoneEvent(CardCheckAttribute attribute, CardEventPenalty penaltyType, T penalty)
        {
            Attribute = attribute;
            PenaltyType = penaltyType;
            Penalty = penalty;
        }

        public override string ToString()
        {
            return ((int)Attribute).ToString() + ((int)PenaltyType).ToString();
        }
    }

    public class Warzone : CardEvent
    {
        public WarzoneEvent<int> Event1 { get; }

        public WarzoneEvent<int> Event2 { get; }

        public WarzoneEvent<List<(Projectile, Direction)>> Event3 { get; }

        public Warzone(GameStage stage, WarzoneEvent<int> event1, WarzoneEvent<int> event2, WarzoneEvent<List<(Projectile, Direction)>> event3) : base(stage)
        {
            Event1 = event1;
            Event2 = event2;
            Event3 = event3;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(base.ToString() + "w");
            sb.Append(Event1.ToString() + Event1.Penalty.ToString());
            sb.Append(Event2.ToString() + Event2.Penalty.ToString());
            sb.Append(Event3.ToString() + Event3.Penalty.Count.ToString());
            foreach((Projectile, Direction) pair in Event3.Penalty)
            {
                sb.Append(((int)pair.Item1).ToString() + ((int)pair.Item2).ToString());
            }
            return sb.ToString();
        }
    }
}

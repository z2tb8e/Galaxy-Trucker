namespace GalaxyTrucker.Client.Model.CardEventTypes
{
    public class WarzoneEvent<T>
    {
        public CardCheckAttribute Attribute { get; }

        public CardEventPenalty PenaltyType { get; }

        public T Penalty { get; }

        public WarzoneEvent(CardCheckAttribute attribute, CardEventPenalty penaltyType, T penalty) =>
            (Attribute, PenaltyType, Penalty) = (attribute, penaltyType, penalty);
    }

    public class Warzone : CardEvent
    {
        public WarzoneEvent<int> Event1 { get; }

        public WarzoneEvent<int> Event2 { get; }

        public WarzoneEvent<Barrage> Event3 { get; }

        public Warzone(WarzoneEvent<int> event1, WarzoneEvent<int> event2, WarzoneEvent<Barrage> event3) =>
            (Event1, Event2, Event3) = (event1, event2, event3);
    }
}

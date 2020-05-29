using GalaxyTrucker.Model;
using System;
using System.Collections.Generic;
using Xunit;

namespace UnitTests
{
    public class PlayerOrderManagerTests
    {
        [Fact]
        public void InitializeTest()
        {
            PlayerOrderManager orderManager;
            List<PlayerColor> invalidArgument = new List<PlayerColor>
            {
                PlayerColor.Yellow,
                PlayerColor.Blue,
                PlayerColor.Green,
                PlayerColor.Yellow
            };

            //trying to create an orderManager with duplicate playercolors
            Assert.Throws<ArgumentException>(() => orderManager = new PlayerOrderManager(invalidArgument, GameStage.First));

            List<PlayerColor> order = new List<PlayerColor>()
            {
                PlayerColor.Red,
                PlayerColor.Green,
                PlayerColor.Blue,
                PlayerColor.Yellow
            };

            //first player position is always 20

            //stage 1
            //other players start with 2 places behind each other
            orderManager = new PlayerOrderManager(order, GameStage.First);
            Assert.Equal(20, orderManager.Properties[PlayerColor.Red].PlaceValue);
            Assert.Equal(18, orderManager.Properties[PlayerColor.Green].PlaceValue);
            Assert.Equal(16, orderManager.Properties[PlayerColor.Blue].PlaceValue);
            Assert.Equal(14, orderManager.Properties[PlayerColor.Yellow].PlaceValue);

            //stage 2
            //other players start with 3 places behind each other
            orderManager = new PlayerOrderManager(order, GameStage.Second);
            Assert.Equal(20, orderManager.Properties[PlayerColor.Red].PlaceValue);
            Assert.Equal(17, orderManager.Properties[PlayerColor.Green].PlaceValue);
            Assert.Equal(14, orderManager.Properties[PlayerColor.Blue].PlaceValue);
            Assert.Equal(11, orderManager.Properties[PlayerColor.Yellow].PlaceValue);

            //stage 3
            //other players start with 4 places behind each other
            orderManager = new PlayerOrderManager(order, GameStage.Third);
            Assert.Equal(20, orderManager.Properties[PlayerColor.Red].PlaceValue);
            Assert.Equal(16, orderManager.Properties[PlayerColor.Green].PlaceValue);
            Assert.Equal(12, orderManager.Properties[PlayerColor.Blue].PlaceValue);
            Assert.Equal(8, orderManager.Properties[PlayerColor.Yellow].PlaceValue);
        }

        [Fact]
        public void CrashTest()
        {
            List<PlayerColor> initialOrder = new List<PlayerColor> { PlayerColor.Blue, PlayerColor.Green, PlayerColor.Red, PlayerColor.Yellow };
            //dictionary for the ordermanager to use for signaling that a player crashed
            Dictionary<PlayerColor, bool> playersCrashed = new Dictionary<PlayerColor, bool>
            {
                {PlayerColor.Blue, false },
                {PlayerColor.Green, false },
                {PlayerColor.Red, false },
                {PlayerColor.Yellow, false }
            };
            PlayerOrderManager orderManger = new PlayerOrderManager(initialOrder, GameStage.First);
            orderManger.PlayerCrashed += (sender, e) =>
            {
                playersCrashed[e] = true;
            };

            //traveling zero distance crashes the player
            orderManger.AddDistance(PlayerColor.Yellow, 0);
            Assert.True(playersCrashed[PlayerColor.Yellow]);

            //40 is the lapsize, so by traveling -40 units the player surely steps over another player
            //making them crash due to the other player being a lap up on them
            orderManger.AddDistance(PlayerColor.Red, -40);
            Assert.True(playersCrashed[PlayerColor.Red]);

            //resetting ordermanager and the dictionary
            orderManger = new PlayerOrderManager(initialOrder, GameStage.First);
            playersCrashed[PlayerColor.Yellow] = false;
            playersCrashed[PlayerColor.Red] = false;
            orderManger.PlayerCrashed += (sender, e) =>
            {
                playersCrashed[e] = true;
            };

            //traveling 40 units makes blue up a lap on green, making everyone else crash
            orderManger.AddDistance(PlayerColor.Blue, 40);
            Assert.True(playersCrashed[PlayerColor.Green]);
            Assert.True(playersCrashed[PlayerColor.Red]);
            Assert.True(playersCrashed[PlayerColor.Yellow]);
        }

        [Fact]
        public void TwoPlayerAddDistanceTest()
        {
            //create a PlayerOrderManager with yellow - red initial order in the first stage
            PlayerOrderManager orderManager = new PlayerOrderManager(new List<PlayerColor> { PlayerColor.Yellow, PlayerColor.Red }, GameStage.First);

            //stepping 5 with red, given that red stepped over yellow, that means the total distance increases to 6
            orderManager.AddDistance(PlayerColor.Red, 5);
            Assert.Equal(24, orderManager.Properties[PlayerColor.Red].PlaceValue);
            List<PlayerColor> expectedOrder = new List<PlayerColor> { PlayerColor.Red, PlayerColor.Yellow };
            Assert.Equal(expectedOrder, orderManager.GetOrder());

            //stepping another 5 with red, no step over, so total distance is 5
            orderManager.AddDistance(PlayerColor.Red, 5);
            Assert.Equal(29, orderManager.Properties[PlayerColor.Red].PlaceValue);

            //stepping 9 with yellow - would land on red's tile, so steps another one, landing at 30
            orderManager.AddDistance(PlayerColor.Yellow, 9);
            Assert.Equal(30, orderManager.Properties[PlayerColor.Yellow].PlaceValue);
            expectedOrder = new List<PlayerColor> { PlayerColor.Yellow, PlayerColor.Red };
            Assert.Equal(expectedOrder, orderManager.GetOrder());

            //stepping -5 with yellow - steps over yellow, so total distance is -6
            orderManager.AddDistance(PlayerColor.Yellow, -5);
            Assert.Equal(24, orderManager.Properties[PlayerColor.Yellow].PlaceValue);
            expectedOrder = new List<PlayerColor> { PlayerColor.Red, PlayerColor.Yellow };
            Assert.Equal(expectedOrder, orderManager.GetOrder());

            //stepping 4 with yellow - this means no step over
            orderManager.AddDistance(PlayerColor.Yellow, 4);
            Assert.Equal(28, orderManager.Properties[PlayerColor.Yellow].PlaceValue);
            Assert.Equal(expectedOrder, orderManager.GetOrder());
        }

        [Fact]
        public void FourPlayerAddDistanceTest()
        {
            PlayerOrderManager orderManager = new PlayerOrderManager(new List<PlayerColor> { PlayerColor.Yellow, PlayerColor.Red, PlayerColor.Green, PlayerColor.Blue }, GameStage.First);

            //green steps 4, which means he steps over red and yellow, total movement 6
            orderManager.AddDistance(PlayerColor.Green, 4);
            Assert.Equal(22, orderManager.Properties[PlayerColor.Green].PlaceValue);
            List<PlayerColor> expectedOrder = new List<PlayerColor> { PlayerColor.Green, PlayerColor.Yellow, PlayerColor.Red, PlayerColor.Blue };
            Assert.Equal(expectedOrder, orderManager.GetOrder());

            //blue steps 10, which means he steps over everyone else (3 players), total movement 13
            orderManager.AddDistance(PlayerColor.Blue, 10);
            Assert.Equal(27, orderManager.Properties[PlayerColor.Blue].PlaceValue);
            expectedOrder = new List<PlayerColor> { PlayerColor.Blue, PlayerColor.Green, PlayerColor.Yellow, PlayerColor.Red };
            Assert.Equal(expectedOrder, orderManager.GetOrder());

            //green steps -5, which means he steps over yellow and red, total movement -7
            orderManager.AddDistance(PlayerColor.Green, -5);
            Assert.Equal(15, orderManager.Properties[PlayerColor.Green].PlaceValue);
            expectedOrder = new List<PlayerColor> { PlayerColor.Blue, PlayerColor.Yellow, PlayerColor.Red, PlayerColor.Green };
            Assert.Equal(expectedOrder, orderManager.GetOrder());

        }
    }
}

using GalaxyTrucker.Model;
using GalaxyTrucker.Model.PartTypes;
using System;
using Xunit;

namespace UnitTests
{
    public class PartTests
    {
        [Fact]
        public void RotateTest()
        {
            Part p = new Pipe(Connector.None, Connector.Single, Connector.Double, Connector.Universal);
            //the ctor connectors are top, right, bottom, left in that order
            //the default rotation is towards top
            Assert.Equal(Connector.None, p.GetConnector(Direction.Top));
            Assert.Equal(Connector.Single, p.GetConnector(Direction.Right));
            Assert.Equal(Connector.Double, p.GetConnector(Direction.Bottom));
            Assert.Equal(Connector.Universal, p.GetConnector(Direction.Left));

            Assert.Equal(Direction.Top, p.Rotation);

            //the supplied int to rotate with is either +1 for +90° or -1 for -90°
            for(int i = -5; i <= 5; ++i)
            {
                if(i != -1 && i != 1)
                {
                    Assert.Throws<ArgumentOutOfRangeException>(() => p.Rotate(i));
                }
            }

            //rotate 90° to the left
            p.Rotate(1);
            Assert.Equal(Connector.Universal, p.GetConnector(Direction.Top));
            Assert.Equal(Connector.None, p.GetConnector(Direction.Right));
            Assert.Equal(Connector.Single, p.GetConnector(Direction.Bottom));
            Assert.Equal(Connector.Double, p.GetConnector(Direction.Left));

            Assert.Equal(Direction.Right, p.Rotation);

            //rotate another 90° to the left
            p.Rotate(1);
            Assert.Equal(Connector.Double, p.GetConnector(Direction.Top));
            Assert.Equal(Connector.Universal, p.GetConnector(Direction.Right));
            Assert.Equal(Connector.None, p.GetConnector(Direction.Bottom));
            Assert.Equal(Connector.Single, p.GetConnector(Direction.Left));

            Assert.Equal(Direction.Bottom, p.Rotation);

            //rotate 2 * 90° to the left to get the original layout
            p.Rotate(1);
            Assert.Equal(Connector.Single, p.GetConnector(Direction.Top));
            Assert.Equal(Connector.Double, p.GetConnector(Direction.Right));
            Assert.Equal(Connector.Universal, p.GetConnector(Direction.Bottom));
            Assert.Equal(Connector.None, p.GetConnector(Direction.Left));

            Assert.Equal(Direction.Left, p.Rotation);

            p.Rotate(1);
            Assert.Equal(Connector.None, p.GetConnector(Direction.Top));
            Assert.Equal(Connector.Single, p.GetConnector(Direction.Right));
            Assert.Equal(Connector.Double, p.GetConnector(Direction.Bottom));
            Assert.Equal(Connector.Universal, p.GetConnector(Direction.Left));

            Assert.Equal(Direction.Top, p.Rotation);

            //rotate 4 * 90° to the right
            p.Rotate(-1);
            Assert.Equal(Connector.Single, p.GetConnector(Direction.Top));
            Assert.Equal(Connector.Double, p.GetConnector(Direction.Right));
            Assert.Equal(Connector.Universal, p.GetConnector(Direction.Bottom));
            Assert.Equal(Connector.None, p.GetConnector(Direction.Left));

            Assert.Equal(Direction.Left, p.Rotation);

            p.Rotate(-1);
            Assert.Equal(Connector.Double, p.GetConnector(Direction.Top));
            Assert.Equal(Connector.Universal, p.GetConnector(Direction.Right));
            Assert.Equal(Connector.None, p.GetConnector(Direction.Bottom));
            Assert.Equal(Connector.Single, p.GetConnector(Direction.Left));

            Assert.Equal(Direction.Bottom, p.Rotation);

            p.Rotate(-1);
            Assert.Equal(Connector.Universal, p.GetConnector(Direction.Top));
            Assert.Equal(Connector.None, p.GetConnector(Direction.Right));
            Assert.Equal(Connector.Single, p.GetConnector(Direction.Bottom));
            Assert.Equal(Connector.Double, p.GetConnector(Direction.Left));

            Assert.Equal(Direction.Right, p.Rotation);

            p.Rotate(-1);
            Assert.Equal(Connector.None, p.GetConnector(Direction.Top));
            Assert.Equal(Connector.Single, p.GetConnector(Direction.Right));
            Assert.Equal(Connector.Double, p.GetConnector(Direction.Bottom));
            Assert.Equal(Connector.Universal, p.GetConnector(Direction.Left));

            Assert.Equal(Direction.Top, p.Rotation);
        }

        [Fact]
        public void BatteryTest()
        {
            //create a battery with the capacity of 5
            Battery battery = new Battery(Connector.None, Connector.None, Connector.None, Connector.None, 5);

            //it should have charges full
            Assert.Equal(5, battery.Charges);

            //trying to use a charge 10 times, should reduce by 1 after each use, not going below 0
            //usecharge returns a logical value indicating whether there was an energy to use
            for(int i = 0; i < 10; ++i)
            {
                int startingCharge = battery.Charges;
                if(startingCharge > 0)
                {
                    Assert.True(battery.UseCharge());
                    Assert.Equal(startingCharge - 1, battery.Charges);
                }
                else
                {
                    Assert.False(battery.UseCharge());
                    Assert.Equal(0, battery.Charges);
                }
            }
        }

        [Fact]
        public void CabinTest()
        {
            //a new cabin should not have any personnel
            Cabin cabin = new Cabin(Connector.None, Connector.None, Connector.None, Connector.None);

            Assert.Equal(Personnel.None, cabin.Personnel);

            //set personnel as alien, removing a single personnel should leave the cabin empty
            cabin.Personnel = Personnel.LaserAlien;
            cabin.RemoveSinglePersonnel();
            Assert.Equal(Personnel.None, cabin.Personnel);

            cabin.Personnel = Personnel.EngineAlien;
            cabin.RemoveSinglePersonnel();
            Assert.Equal(Personnel.None, cabin.Personnel);

            //setting personnel as double humans, removing a singl personnel should leave the cabin with 1 human
            cabin.Personnel = Personnel.HumanDouble;
            cabin.RemoveSinglePersonnel();
            Assert.Equal(Personnel.HumanSingle, cabin.Personnel);

            //removing personnel a second times makes it empty again
            cabin.RemoveSinglePersonnel();
            Assert.Equal(Personnel.None, cabin.Personnel);
        }

        [Fact]
        public void ActivatableTest()
        {
            //by default part is not activated
            IActivatable activatable = new Shield(Connector.None, Connector.None, Connector.None, Connector.None);
            Assert.False(activatable.Activated);

            activatable.Activate();
            Assert.True(activatable.Activated);

            activatable.Deactivate();
            Assert.False(activatable.Activated);

            //since IActivatable is only an interface, test for each implementing type
            activatable = new LaserDouble(Connector.None, Connector.None, Connector.None, Connector.None);
            Assert.False(activatable.Activated);

            activatable.Activate();
            Assert.True(activatable.Activated);

            activatable.Deactivate();
            Assert.False(activatable.Activated);

            activatable = new EngineDouble(Connector.None, Connector.None, Connector.None, Connector.None);
            Assert.False(activatable.Activated);

            activatable.Activate();
            Assert.True(activatable.Activated);

            activatable.Deactivate();
            Assert.False(activatable.Activated);
        }

        [Fact]
        public void EngineTest()
        {
            //an engine can't have a connector facing downwards
            Engine invalidEngine;
            Assert.Throws<ArgumentException>(() => invalidEngine = new Engine(Connector.None, Connector.None, Connector.Universal, Connector.None));

            //engines also can't be rotated
            Engine regularEngine = new Engine(Connector.None, Connector.None, Connector.None, Connector.None);
            regularEngine.Rotate(1);
            Assert.Equal(Direction.Top, regularEngine.Rotation);

            //regular engine has 1 enginepower
            Assert.Equal(1, regularEngine.Enginepower);

            //while deactivated a double engine has 0 enginepower
            EngineDouble doubleEngine = new EngineDouble(Connector.None, Connector.None, Connector.None, Connector.None);
            Assert.False(doubleEngine.Activated);
            Assert.Equal(0, doubleEngine.Enginepower);

            //while activated a double engine has 2 enginepower
            doubleEngine.Activate();
            Assert.Equal(2, doubleEngine.Enginepower);
        }

        [Fact]
        public void LaserTest()
        {
            //lasers can't have a connector facing upwards
            Laser invalidLaser;
            Assert.Throws<ArgumentException>(() => invalidLaser = new Laser(Connector.Universal, Connector.None, Connector.None, Connector.None));

            Laser regularLaser = new Laser(Connector.None, Connector.None, Connector.None, Connector.None);

            //while facing top, lasers have a firepower of 2
            Assert.Equal(Direction.Top, regularLaser.Rotation);
            Assert.Equal(2, regularLaser.Firepower);

            //while facing any other direction, regular laser has a firepower of 1
            regularLaser.Rotate(1);
            Assert.Equal(1, regularLaser.Firepower);

            regularLaser.Rotate(1);
            Assert.Equal(1, regularLaser.Firepower);

            regularLaser.Rotate(1);
            Assert.Equal(1, regularLaser.Firepower);

            //inactive double laser has a firepower of 0 regardless of rotation
            LaserDouble doubleLaser = new LaserDouble(Connector.None, Connector.None, Connector.None, Connector.None);
            for(int i = 0; i < 4; ++i)
            {
                doubleLaser.Rotate(1);
                Assert.Equal(0, doubleLaser.Firepower);
            }

            //while activated, double laser has 4 firepower if facing top, 2 if facing any other direction
            doubleLaser.Activate();
            Assert.Equal(Direction.Top, doubleLaser.Rotation);
            Assert.Equal(4, doubleLaser.Firepower);

            doubleLaser.Rotate(1);
            Assert.Equal(2, doubleLaser.Firepower);

            doubleLaser.Rotate(1);
            Assert.Equal(2, doubleLaser.Firepower);

            doubleLaser.Rotate(1);
            Assert.Equal(2, doubleLaser.Firepower);
        }

        [Fact]
        public void ShieldTest()
        {
            //shield is always shielding from 2 directions, one of those is the one it's rotation is, the other is the next direction clockwise
            Shield shield = new Shield(Connector.None, Connector.None, Connector.None, Connector.None);

            Assert.Equal(Direction.Top, shield.Rotation);
            Assert.Equal(Direction.Top, shield.Directions.Item1);
            Assert.Equal(Direction.Right, shield.Directions.Item2);

            shield.Rotate(1);
            Assert.Equal(Direction.Right, shield.Rotation);
            Assert.Equal(Direction.Right, shield.Directions.Item1);
            Assert.Equal(Direction.Bottom, shield.Directions.Item2);

            shield.Rotate(1);
            Assert.Equal(Direction.Bottom, shield.Rotation);
            Assert.Equal(Direction.Bottom, shield.Directions.Item1);
            Assert.Equal(Direction.Left, shield.Directions.Item2);

            shield.Rotate(1);
            Assert.Equal(Direction.Left, shield.Rotation);
            Assert.Equal(Direction.Left, shield.Directions.Item1);
            Assert.Equal(Direction.Top, shield.Directions.Item2);
        }

        [Fact]
        public void StorageTest()
        {
            /*wares values:
             * blue - 1
             * green - 2
             * yellow - 3
             * red - 4  
             */
            
            //by default storage is empty
            Storage regularStorage = new Storage(Connector.None, Connector.None, Connector.None, Connector.None, 3);
            Assert.Equal(0, regularStorage.Value);

            //regular storage can't store red wares
            regularStorage.AddWare(Ware.Red);
            Assert.Equal(0, regularStorage.Value);

            //fill the storage with wares of varying value
            regularStorage.AddWare(Ware.Yellow);
            regularStorage.AddWare(Ware.Blue);
            regularStorage.AddWare(Ware.Green);

            Assert.Equal(Ware.Yellow, regularStorage.Max);
            Assert.Equal(Ware.Blue, regularStorage.Min);
            Assert.Equal(6, regularStorage.Value);

            //adding another ware replaces the one with the lowest value
            regularStorage.AddWare(Ware.Yellow);
            Assert.Equal(Ware.Green, regularStorage.Min);
            Assert.Equal(8, regularStorage.Value);

            //removing the ware with the highest value
            regularStorage.RemoveMax();
            Assert.Equal(5, regularStorage.Value);

            //special storage is the same, but can store red wares too
            Storage specialStorage = new SpecialStorage(Connector.None, Connector.None, Connector.None, Connector.None, 3);
            specialStorage.AddWare(Ware.Red);
            Assert.Equal(Ware.Red, specialStorage.Max);
            Assert.Equal(4, specialStorage.Value);
        }
    }
}

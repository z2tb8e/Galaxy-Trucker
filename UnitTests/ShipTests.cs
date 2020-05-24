using GalaxyTrucker.Model;
using GalaxyTrucker.Model.PartTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace UnitTests
{
    public class ShipTests
    {
        [Fact]
        public void LayoutTest()
        {
            Dictionary<ShipLayout, int[,]> expectedLayouts = new Dictionary<ShipLayout, int[,]>
            {
                {ShipLayout.Small, GetSmallLayout() },
                {ShipLayout.Medium, GetMediumLayout() },
                {ShipLayout.BigLong, GetBigLongLayout() },
                {ShipLayout.BigWide, GetBigWideLayout() }
            };

            Dictionary<ShipLayout, Ship> ships = new Dictionary<ShipLayout, Ship>
            {
                {ShipLayout.Small, new Ship(ShipLayout.Small, PlayerColor.Blue) },
                {ShipLayout.Medium, new Ship(ShipLayout.Medium, PlayerColor.Blue) },
                {ShipLayout.BigLong, new Ship(ShipLayout.BigLong, PlayerColor.Blue) },
                {ShipLayout.BigWide, new Ship(ShipLayout.BigWide, PlayerColor.Blue) }
            };

            foreach(ShipLayout layout in ships.Keys)
            {
                for(int i = 0; i < 11; ++i)
                {
                    for(int j = 0; j < 11; ++j)
                    {
                        bool expectedResult = expectedLayouts[layout][i, j] == 1 ? true : false;
                        Assert.Equal(expectedResult, ships[layout].IsValidField(i, j));
                    }
                }
                Assert.Contains(ships[layout].Parts, p => p is Cockpit);
            }

        }

        [Fact]
        public void ConnectorsMatchTest()
        {
            //create a ship, and a part with all kinds of connectors
            Ship ship = new Ship(ShipLayout.Small, PlayerColor.Blue);
            //ctor connectors order: top, right, bottom, left
            Part matchingPart = new Pipe(Connector.None, Connector.Single, Connector.Double, Connector.Universal);
            PartAddProblems result;
            //note: if the returned enum has any error flags, the part was not added
            /*None connectors can only face other None connectors (that doesn't count as a connection though)
            * Single connectors can't face Double connectors
            * the rest of the combinations are allowed and count as a connection
            * cockpit is at the indeces 5,5, with universal connectors on all sides
            */

            //adding the part to the right of the cockpit, connectors: universal, universal
            result = ship.AddPart(matchingPart, 5, 6);
            Assert.False(result.HasFlag(PartAddProblems.ConnectorsDontMatch));
            Assert.False(result.HasFlag(PartAddProblems.HasNoConnection));

            Part part1 = new Pipe(Connector.Double, Connector.None, Connector.None, Connector.Double);
            //trying to add right to the previously added pipe, connectors: single, double
            result = ship.AddPart(part1, 5, 7);
            Assert.True(result.HasFlag(PartAddProblems.ConnectorsDontMatch));
            Assert.True(result.HasFlag(PartAddProblems.HasNoConnection));

            //trying to add on top of the previously added pipe, connectors: none, none
            result = ship.AddPart(part1, 4, 6);
            Assert.False(result.HasFlag(PartAddProblems.ConnectorsDontMatch));
            //also, this doesn't count as a connection
            Assert.True(result.HasFlag(PartAddProblems.HasNoConnection));

            //trying to add below the previously added pipe, connectors: double, double
            result = ship.AddPart(part1, 6, 6);
            Assert.False(result.HasFlag(PartAddProblems.ConnectorsDontMatch));
            Assert.False(result.HasFlag(PartAddProblems.HasNoConnection));


            Part part2 = new Pipe(Connector.None, Connector.None, Connector.None, Connector.Single);
            //trying to add another part right to the first added pipe, connectors: single, single
            result = ship.AddPart(part2, 5, 7);
            Assert.False(result.HasFlag(PartAddProblems.ConnectorsDontMatch));
            Assert.False(result.HasFlag(PartAddProblems.HasNoConnection));
        }

        [Fact]
        public void AddPartTest()
        {
            Ship ship = new Ship(ShipLayout.Small, PlayerColor.Blue);

            //add a fitting part to the right of the cabin
            //note: if the returned enum has any error flags, the part was not added
            Part pipe = new Pipe(Connector.Universal, Connector.Universal, Connector.Universal, Connector.Universal);
            Part laser = new Laser(Connector.None, Connector.Universal, Connector.None, Connector.Universal);
            Part engine = new Engine(Connector.None, Connector.Universal, Connector.None, Connector.Universal);
            PartAddProblems result;

            //trying to add a part to an occupied space (cockpit is at 5,5)
            result = ship.AddPart(engine, 5, 5);
            Assert.True(result.HasFlag(PartAddProblems.Occupied));

            //trying to add a part outside the allowed zone (also signaled by occupied)
            int row = 1, column = 1;
            Assert.False(ship.IsValidField(row, column));
            result = ship.AddPart(pipe, row, column);
            Assert.True(result.HasFlag(PartAddProblems.Occupied));

            //adding the pipe on top and below the cockpit, should be a valid operations
            result = ship.AddPart(pipe, 4, 5);
            Assert.Equal(PartAddProblems.None, result);
            result = ship.AddPart(pipe, 6, 5);
            Assert.Equal(PartAddProblems.None, result);

            //addig the laser to the right of the bottom pipe, the engine to the right of the top pipe, both should be valid
            result = ship.AddPart(laser, 6, 6);
            Assert.Equal(PartAddProblems.None, result);
            result = ship.AddPart(engine, 4, 6);
            Assert.Equal(PartAddProblems.None, result);

            //trying to add a laser to the right of the cockpit
            /*problems:
             * the new laser's facing direction (right above it) is obscured
             * the above placed engine has its exhaust blocked by the new laser
             * the below placed laser's facing direction (right above it) is obscured
             */
            result = ship.AddPart(laser, 5, 6);
            Assert.Equal(PartAddProblems.BlockedAsLaser | PartAddProblems.BlocksEngine | PartAddProblems.BlocksLaser, result);

            //trying to add an engine to the right of the cockpit
            /*problems:
             * the new engine's exhaust is blocked by the laser below
             * the below and above placed laser and engine are obscured by the new engine (as described at the previous case)
             */
            result = ship.AddPart(engine, 5, 6);
            Assert.Equal(PartAddProblems.BlockedAsEngine | PartAddProblems.BlocksEngine | PartAddProblems.BlocksLaser, result);
        }

        [Fact]
        public void AlienTest()
        {
            //ship not able to house alien crew
            Ship invalidShip = new Ship(ShipLayout.Small, PlayerColor.Blue);
            //ship able to house alien crew
            Ship validShip = new Ship(ShipLayout.Small, PlayerColor.Blue);

            Cabin cabin = new Cabin(Connector.Universal, Connector.Universal, Connector.Universal, Connector.Universal);
            EngineCabin engineCabin = new EngineCabin(Connector.Universal, Connector.Universal, Connector.Universal, Connector.Universal);

            //parts connected right next to the cockpit(5,5)
            //on the cockpit's right side is the engineCabin, right below that the other cabin
            validShip.AddPart(engineCabin, 5, 6);
            validShip.AddPart(cabin, 6, 6);

            foreach(Personnel pc in Enum.GetValues(typeof(Personnel)))
            {
                //invalidShip can't house any kind of alien, validShip can house engine aliens
                //(can house humans, which is included, in the Personnel enum, but it auto-returns false)
                Assert.False(invalidShip.HighlightCabinsForAlien(pc));
                if(pc != Personnel.EngineAlien)
                {
                    Assert.False(validShip.HighlightCabinsForAlien(pc));
                }
            }
            Assert.True(validShip.HighlightCabinsForAlien(Personnel.EngineAlien));

            //trying to insert right personnel to the wrong cabin
            Assert.False(validShip.AddAlien(5, 6, Personnel.EngineAlien));

            //to the cockpit, which even though would be valid as a cabin, but the cockpit explicitly must have humans as personel
            Assert.False(validShip.AddAlien(5, 5, Personnel.EngineAlien));

            //trying to add humans or a laseralien, which's requirement is not met
            foreach(Personnel pc in Enum.GetValues(typeof(Personnel)))
            {
                if(pc != Personnel.EngineAlien)
                {
                    Assert.False(validShip.AddAlien(6, 6, pc));
                }
            }

            //adding the actual alien
            Assert.True(validShip.AddAlien(6, 6, Personnel.EngineAlien));

            //only one alien of each type can be housed
            Assert.False(validShip.AddAlien(6, 6, Personnel.EngineAlien));
        }

        [Fact]
        public void RemoveAlienCabinTest()
        {
            Ship ship = new Ship(ShipLayout.Small, PlayerColor.Blue);
            //the alien itself is housed in the cabin, the neighbouring alienCabin is the requisite for the cabin to be able to house it
            Cabin cabin = new Cabin(Connector.Universal, Connector.Universal, Connector.Universal, Connector.Universal);
            Part alienCabin = new EngineCabin(Connector.Universal, Connector.Universal, Connector.Universal, Connector.Universal);

            //add the parts and the alien to the ship
            ship.AddPart(cabin, 5, 6);
            ship.AddPart(alienCabin, 5, 7);

            ship.AddAlien(5, 6, Personnel.EngineAlien);

            //the alien in fact got added
            Assert.Equal(Personnel.EngineAlien, cabin.Personnel);

            //remove the life support unit - the alien should get removed as well
            ship.RemovePartAtIndex(5, 7);
            Assert.Equal(Personnel.None, cabin.Personnel);

            //cabin can no longer house alien
            Assert.False(ship.AddAlien(5, 6, Personnel.EngineAlien));
        }

        [Fact]
        public void OpenConnectorsTest()
        {
            Ship ship = new Ship(ShipLayout.Small, PlayerColor.Blue);
            //at start the cockpit has 4 open connectors in each direction
            Assert.Equal(4, ship.GetOpenConnectorCount());

            //add a part below the cockpit to close that open connector
            Part bottomPipe = new Pipe(Connector.Universal, Connector.None, Connector.None, Connector.None);
            ship.AddPart(bottomPipe, 6, 5);
            Assert.Equal(3, ship.GetOpenConnectorCount());

            //close off another open connection on the right
            Part rightPipe = new Pipe(Connector.None, Connector.None, Connector.None, Connector.Universal);
            ship.AddPart(rightPipe, 5, 6);
            Assert.Equal(2, ship.GetOpenConnectorCount());

            //add a part at top which closes one open connector, but introduces 3 new
            Part topPipe = new Pipe(Connector.Universal, Connector.Universal, Connector.Universal, Connector.Universal);
            ship.AddPart(topPipe, 4, 5);
            Assert.Equal(4, ship.GetOpenConnectorCount());

            //add another part on the left which closes one, introduces 2 new open connectors
            Part leftPipe = new Pipe(Connector.Universal, Connector.Universal, Connector.Universal, Connector.None);
            ship.AddPart(leftPipe, 5, 4);
            Assert.Equal(5, ship.GetOpenConnectorCount());
        }

        [Fact]
        public void RemovePersonnelTest()
        {
            Ship ship = new Ship(ShipLayout.Small, PlayerColor.Blue);
            Cabin cabinWithEngineAlien = new Cabin(Connector.Universal, Connector.Universal, Connector.Universal, Connector.Universal);
            Part engineSupportUnit = new EngineCabin(Connector.Universal, Connector.Universal, Connector.Universal, Connector.Universal);
            Cabin cabinWithHumans1 = new Cabin(Connector.Universal, Connector.Universal, Connector.Universal, Connector.Universal);
            Cabin cabinWithHumans2 = new Cabin(Connector.Universal, Connector.Universal, Connector.Universal, Connector.Universal);
            Cabin cockpit = ship.GetCockpit() as Cabin;

            //crew is empty by default
            Assert.Equal(Personnel.None, cockpit.Personnel);
            Assert.Equal(Personnel.None, cabinWithEngineAlien.Personnel);
            Assert.Equal(Personnel.None, cabinWithHumans1.Personnel);
            Assert.Equal(Personnel.None, cabinWithHumans2.Personnel);

            Assert.Equal(0, ship.CrewCount);

            ship.AddPart(cabinWithEngineAlien, 5, 6);
            ship.AddPart(engineSupportUnit, 5, 7);
            ship.AddPart(cabinWithHumans1, 6, 5);
            ship.AddPart(cabinWithHumans2, 4, 5);

            ship.AddAlien(5, 6, Personnel.EngineAlien);
            ship.FillCabins();
            //FillCabisn sets all cabins' personnel (where its not set already) to humandouble
            Assert.Equal(Personnel.HumanDouble, cockpit.Personnel);
            Assert.Equal(Personnel.EngineAlien, cabinWithEngineAlien.Personnel);
            Assert.Equal(Personnel.HumanDouble, cabinWithHumans1.Personnel);
            Assert.Equal(Personnel.HumanDouble, cabinWithHumans2.Personnel);
            Assert.Equal(7, ship.CrewCount);

            //trying to remove 3 crewmembers should succeed
            Assert.Equal(3, ship.RemovePersonnel(3));

            //the removed personnel are prioritised to be the humans outside the cockpit
            Assert.Equal(4, ship.CrewCount);
            Assert.Equal(Personnel.EngineAlien, cabinWithEngineAlien.Personnel);
            Assert.Equal(Personnel.HumanDouble, cockpit.Personnel);

            //trying to remove further 2, keeping cockpit personnel intact if possible
            Assert.Equal(2, ship.RemovePersonnel(2));
            Assert.Equal(2, ship.CrewCount);
            Assert.Equal(Personnel.None, cabinWithEngineAlien.Personnel);
            Assert.Equal(Personnel.HumanDouble, cockpit.Personnel);

            //trying to remove 3 more, this will empty the cockpit as well
            //there were only 2 members left
            Assert.Equal(2, ship.RemovePersonnel(3));
            Assert.Equal(0, ship.CrewCount);
            Assert.Equal(Personnel.None, cockpit.Personnel);
        }

        [Fact]
        public void WaresTest()
        {
            Ship ship = new Ship(ShipLayout.Small, PlayerColor.Blue);
            //create two storages, (only the specialstorage can store red wares), with capacities 5 and 3
            Storage storage = new Storage(Connector.Universal, Connector.Universal, Connector.Universal, Connector.Universal, 5);
            Storage specialStorage = new SpecialStorage(Connector.Universal, Connector.Universal, Connector.Universal, Connector.Universal, 3);
            ship.AddPart(storage, 5, 4);
            ship.AddPart(specialStorage, 5, 6);

            //initially the storages are empty
            Assert.Equal(0, ship.GetWaresValue());

            Assert.Equal(0, storage.Value);
            Assert.Equal(0, specialStorage.Value);

            //adding red wares, which can only be stored in the specialstorage, the excess won't be stored
            /*Ware values:
             * blue - 1
             * green - 2
             * yellow - 3
             * red - 4
             */
            ship.AddWares(Enumerable.Repeat(Ware.Red, 5));
            Assert.Equal(0, storage.Value);
            Assert.Equal(3 * 4, specialStorage.Value);
            Assert.Equal(3 * 4, ship.GetWaresValue());

            //fill the regular storage with blue wares
            ship.AddWares(Enumerable.Repeat(Ware.Blue, 5));
            Assert.Equal(5, storage.Value);
            Assert.Equal(17, ship.GetWaresValue());

            //try to add a ware more valuable than blue - blue gets replaced
            ship.AddWares(Enumerable.Repeat(Ware.Yellow, 3));
            Assert.Equal(2 * 1 + 3 * 3, storage.Value);

            //try to add a ware less valuable than yellow, but more valuable than blue - resources get partially replaced
            //so the current content of the regular storage is 3 * yellow + 2 * green (specialstorage intact)
            ship.AddWares(Enumerable.Repeat(Ware.Green, 4));
            Assert.Equal(13, storage.Value);
            Assert.Equal(12, specialStorage.Value);
            Assert.Equal(25, ship.GetWaresValue());

            //try to remove a number of resources - the most valuable ones should get removed first
            //with 4 getting removed, it should be 3 red from the special storage, and 1 yellow from the regular one
            Assert.Equal(4, ship.RemoveWares(4));
            Assert.Equal(0, specialStorage.Value);
            Assert.Equal(10, storage.Value);

            //remove a number of wares higher than the remaining(4)
            Assert.Equal(4, ship.RemoveWares(10));
            Assert.Equal(0, storage.Value);
            Assert.Equal(0, specialStorage.Value);
            Assert.Equal(0, ship.GetWaresValue());
        }

        [Fact]
        public void ActivatablesTest()
        {
            Ship ship = new Ship(ShipLayout.Small, PlayerColor.Blue);

            Battery battery = new Battery(Connector.Universal, Connector.Universal, Connector.Universal, Connector.Universal, 5);
            Shield shield = new Shield(Connector.Universal, Connector.Universal, Connector.Universal, Connector.Universal);
            EngineDouble engine = new EngineDouble(Connector.Universal, Connector.Universal, Connector.None, Connector.Universal);
            LaserDouble laser = new LaserDouble(Connector.None, Connector.Universal, Connector.Universal, Connector.Universal);

            //ship has no activatable parts to begin with
            Assert.False(ship.HighlightActivatables());

            //once again cockpit is at 5,5
            ship.AddPart(battery, 5, 4);
            ship.AddPart(shield, 5, 6);
            ship.AddPart(engine, 6, 5);
            ship.AddPart(laser, 4, 5);

            //ship has activatable parts now
            Assert.True(ship.HighlightActivatables());

            //none are activated to begin with, battery has maximum charges
            Assert.Equal(5, battery.Charges);
            Assert.False(shield.Activated);
            Assert.False(engine.Activated);
            Assert.False(laser.Activated);

            //in this state the laser and the engine have 0 fire and enginepower
            Assert.Equal(0, laser.Firepower);
            Assert.Equal(0, engine.Enginepower);

            //activate the shield - one charge should be removed, and shield should be activate
            ship.ActivatePartAt(5, 6);
            Assert.Equal(4, battery.Charges);
            Assert.True(shield.Activated);

            //activate the engine - apart from the above, the enginepower should be 2
            ship.ActivatePartAt(6, 5);
            Assert.Equal(3, battery.Charges);
            Assert.True(engine.Activated);
            Assert.Equal(2, engine.Enginepower);

            //activate the laser - apart from the above, the enginepower should be 4
            ship.ActivatePartAt(4, 5);
            Assert.Equal(2, battery.Charges);
            Assert.True(laser.Activated);
            Assert.Equal(4, laser.Firepower);

            //deactivate all
            ship.ResetActivatables();
            Assert.False(shield.Activated);
            Assert.False(engine.Activated);
            Assert.False(laser.Activated);
            Assert.Equal(0, engine.Enginepower);
            Assert.Equal(0, laser.Firepower);
        }

        [Fact]
        public void PandemicTest()
        {
            Ship ship = new Ship(ShipLayout.Small, PlayerColor.Blue);
            Cabin cabin1 = new Cabin(Connector.Universal, Connector.Universal, Connector.Universal, Connector.Universal);
            Cabin cabin2 = new Cabin(Connector.Universal, Connector.Universal, Connector.Universal, Connector.Universal);
            Cabin cabin3 = new Cabin(Connector.Universal, Connector.Universal, Connector.Universal, Connector.Universal);
            Part alienCabin = new EngineCabin(Connector.Universal, Connector.Universal, Connector.Universal, Connector.Universal);

            //insert cabins, two of them to the right of the cockpit one after another
            ship.AddPart(cabin1, 5, 6);
            ship.AddPart(cabin2, 5, 7);
            //the aliencabin and the cabin right below the 5,6 cabin
            ship.AddPart(alienCabin, 6, 6);
            ship.AddPart(cabin3, 7, 6);

            ship.AddAlien(5, 6, Personnel.EngineAlien);
            ship.FillCabins();

            //check is cabins are correctly filled to begin with
            Assert.Equal(Personnel.EngineAlien, cabin1.Personnel);
            Assert.Equal(Personnel.HumanDouble, cabin2.Personnel);
            Assert.Equal(Personnel.HumanDouble, cabin3.Personnel);
            Assert.Equal(Personnel.HumanDouble, (ship.GetCockpit() as Cabin).Personnel);

            //apply the pandemic,
            ship.ApplyPandemic();

            //this should remove one human from cabin2 and the cockpit, and the alien from cabin1, cabin3 should remain intact
            Assert.Equal(Personnel.None, cabin1.Personnel);
            Assert.Equal(Personnel.HumanSingle, cabin2.Personnel);
            Assert.Equal(Personnel.HumanSingle, (ship.GetCockpit() as Cabin).Personnel);
            Assert.Equal(Personnel.HumanDouble, cabin3.Personnel);

            //apply the pandemic again
            ship.ApplyPandemic();

            //since cabin 1 is empty, no crew members should be removed
            Assert.Equal(Personnel.None, cabin1.Personnel);
            Assert.Equal(Personnel.HumanSingle, cabin2.Personnel);
            Assert.Equal(Personnel.HumanSingle, (ship.GetCockpit() as Cabin).Personnel);
            Assert.Equal(Personnel.HumanDouble, cabin3.Personnel);
        }

        [Fact]
        public void ProjectileTest()
        {
            Ship ship = new Ship(ShipLayout.Small, PlayerColor.Blue);

            Part battery = new Battery(Connector.Universal, Connector.Universal, Connector.Universal, Connector.Universal, 100);
            Part laser = new Laser(Connector.None, Connector.Universal, Connector.Universal, Connector.Universal);
            Shield shield = new Shield(Connector.None, Connector.Universal, Connector.None, Connector.None);

            ship.AddPart(battery, 5, 6);
            ship.AddPart(laser, 4, 5);
            ship.AddPart(shield, 5, 4);

            //the newly added parts are in the parts collection
            Assert.Contains(battery, ship.Parts);
            Assert.Contains(laser, ship.Parts);
            Assert.Contains(shield, ship.Parts);
            Assert.Equal(0, ship.Penalty);

            //launch a small meteor at the ship - this should only cause damage if it hits an open connector - and even then can be blocked by shields
            //this hits the front of the laser, but there is no connector there
            ship.ApplyProjectile(Projectile.MeteorSmall, Direction.Top, 5);
            Assert.Contains(laser, ship.Parts);
            Assert.Equal(0, ship.Penalty);

            //this hits the front of the shield - yet again, there's no connector there
            ship.ApplyProjectile(Projectile.MeteorSmall, Direction.Top, 4);
            Assert.Contains(shield, ship.Parts);
            Assert.Equal(0, ship.Penalty);

            //try to hit into an open connector on the battery, but with shields activated(currently the shield is facing top/right)
            ship.ActivatePartAt(5, 4);
            ship.ApplyProjectile(Projectile.MeteorSmall, Direction.Top, 6);
            Assert.True(shield.Activated);
            Assert.Contains(battery, ship.Parts);
            Assert.Equal(0, ship.Penalty);

            //same with shields not active - the battery gets removed
            ship.ResetActivatables();
            ship.ApplyProjectile(Projectile.MeteorSmall, Direction.Top, 6);
            Assert.False(shield.Activated);
            Assert.DoesNotContain(battery, ship.Parts);
            Assert.Equal(1, ship.Penalty);

            //launch a large meteor at the ship - this should cause damage unless it's shot by an active laser facing it in the destined line
            ship.AddPart(battery, 5, 6);
            Part doubleLaser = new LaserDouble(Connector.None, Connector.Universal, Connector.Universal, Connector.Universal);
            //add an activatable laser
            ship.AddPart(doubleLaser, 4, 6);

            //flying into a regular laser - laser remains instact
            ship.ApplyProjectile(Projectile.MeteorLarge, Direction.Top, 5);
            Assert.Contains(laser, ship.Parts);
            Assert.Equal(1, ship.Penalty);

            //flying into the activated double laser - laser remains intact
            ship.ActivatePartAt(4, 6);
            ship.ApplyProjectile(Projectile.MeteorLarge, Direction.Top, 6);
            Assert.Contains(doubleLaser, ship.Parts);
            Assert.Equal(1, ship.Penalty);

            //Flying into the deactivated double laser - laser gets removed
            ship.ResetActivatables();
            ship.ApplyProjectile(Projectile.MeteorLarge, Direction.Top, 6);
            Assert.DoesNotContain(doubleLaser, ship.Parts);
            Assert.Equal(2, ship.Penalty);

            //flying into the deactivated laser with shields on - does not block the projectile
            ship.AddPart(doubleLaser, 4, 6);
            ship.ActivatePartAt(5, 4);
            ship.ApplyProjectile(Projectile.MeteorLarge, Direction.Top, 6);
            Assert.DoesNotContain(doubleLaser, ship.Parts);
            Assert.Equal(3, ship.Penalty);

            //launch a small shot at the ship - this causes damage unless shielded
            //shield is still active
            ship.ApplyProjectile(Projectile.ShotSmall, Direction.Top, 5);
            Assert.True(shield.Activated);
            Assert.Contains(laser, ship.Parts);
            Assert.Equal(3, ship.Penalty);

            //repeat the small shot with shields inactive, the laser doesn't stop it either
            ship.ResetActivatables();
            ship.ApplyProjectile(Projectile.ShotSmall, Direction.Top, 5);
            Assert.False(shield.Activated);
            Assert.DoesNotContain(laser, ship.Parts);
            Assert.Equal(4, ship.Penalty);

            //launch a large shot at the ship - this causes damage regardless of anything
            //fired into the laser, while shields are active too
            ship.ActivatePartAt(5, 4);
            ship.AddPart(laser, 4, 5);
            Assert.Contains(laser, ship.Parts);
            ship.ApplyProjectile(Projectile.ShotLarge, Direction.Top, 5);
            Assert.True(shield.Activated);
            Assert.DoesNotContain(laser, ship.Parts);
            Assert.Equal(5, ship.Penalty);

            //fire at the shield to deactivate it
            ship.ApplyProjectile(Projectile.ShotLarge, Direction.Top, 4);
            Assert.DoesNotContain(shield, ship.Parts);
            //note - 5 is the upper limit for penalty for the small shiplayout
            Assert.Equal(5, ship.Penalty);

            //fire a small shot at the cockpit - shield is removed, so the shot deals damage, and wrecks the ship
            bool wrecked = false;
            ship.Wrecked += (sender, e) =>
            {
                wrecked = true;
            };
            ship.ApplyProjectile(Projectile.ShotSmall, Direction.Top, 5);
            Assert.Null(ship.GetCockpit());
            Assert.True(wrecked);
        }

        //layouts with ints for clearer readability with 0 for false and 1 for true
        private int[,] GetSmallLayout()
        {
            /*
             *          o
             *         ooo
             *        ooooo
             *        ooooo
             *        oo oo
             */
            return new int[11,11]
            {
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                {0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0 },
                {0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0 },
                {0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0 },
                {0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0 },
                {0, 0, 0, 1, 1, 0, 1, 1, 0, 0, 0 },
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            };
        }

        private int[,] GetMediumLayout()
        {
            /*
             *          o
             *         ooo
             *        ooooo
             *        ooooo
             *       ooooooo
             *       ooo ooo
             */
            return new int[11, 11]
            {
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                {0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0 },
                {0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0 },
                {0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0 },
                {0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0 },
                {0, 0, 1, 1, 1, 1, 1, 1, 1, 0, 0 },
                {0, 0, 1, 1, 1, 0, 1, 1, 1, 0, 0 },
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            };
        }

        private int[,] GetBigLongLayout()
        {
            /*
             *         ooo
             *        ooooo
             *        oo oo
             *        ooooo
             *         ooo
             *        o o o
             *        ooooo
             *        ooooo
             *        o o o
             */
            return new int[11, 11]
            {
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                {0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0 },
                {0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0 },
                {0, 0, 0, 1, 1, 0, 1, 1, 0, 0, 0 },
                {0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0 },
                {0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0 },
                {0, 0, 0, 1, 0, 1, 0, 1, 0, 0, 0 },
                {0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0 },
                {0, 0, 0, 1, 1, 1, 1, 1, 0, 0, 0 },
                {0, 0, 0, 1, 0, 1, 0, 1, 0, 0, 0 },
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            };
        }

        private int[,] GetBigWideLayout()
        {
            /*
             *          o
             *         ooo
             *        ooooo
             *      o ooooo o
             *      ooooooooo
             *      ooooooooo
             *      oo ooo oo
             */
            return new int[11, 11]
            {
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                {0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0 },
                {0, 0, 0, 0, 1, 1, 1, 0, 0, 0, 0 },
                {0, 1, 0, 1, 1, 1, 1, 1, 0, 1, 0 },
                {0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0 },
                {0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0 },
                {0, 1, 1, 0, 1, 1, 1, 0, 1, 1, 0 },
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
                {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            };
        }
    }
}

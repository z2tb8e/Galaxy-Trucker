namespace GalaxyTrucker.Model
{
    /// <summary>
    /// Class used to tell carry information about other players's ships
    /// </summary>
    public class PlayerAttributes
    {
        public int Firepower { get; set; }

        public int Enginepower { get; set; }

        public int CrewCount { get; set; }

        public int StorageSize { get; set; }

        public int Batteries { get; set; }

        public PlayerAttributes() { }

        public PlayerAttributes(int firepower, int enginepower, int crewCount, int storageSize, int batteries)
        {
            Firepower = firepower;
            Enginepower = enginepower;
            CrewCount = crewCount;
            StorageSize = storageSize;
            Batteries = batteries;
        }

        public override string ToString()
        {
            return $",{Firepower},{Enginepower},{CrewCount},{StorageSize},{Batteries}";
        }
    }
}

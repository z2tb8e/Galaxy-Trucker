using GalaxyTrucker.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Documents;

namespace TestHelper
{
    class Program
    {
        static void Main(string[] args)
        {
            string line;
            using StreamReader sr = new StreamReader("Cards.txt");
            while((line = sr.ReadLine()) != null)
            {
                if(line != line.ToCardEvent().ToString())
                {
                    Console.WriteLine($"eredeti: {line}, konv: {line.ToCardEvent()}");
                }
            }
        }
    }
}

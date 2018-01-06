using System;
using System.IO;

namespace MapDiffGenerator
{
    class Program
    {
        static DiffGenerator DiffGenerator = new DiffGenerator();

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                throw new ArgumentException("Invalid number of arguments");
            }

            string customMapPath = args[0];
            string referenceMapPath = args[1];

            string cwd = Directory.GetCurrentDirectory();

            DiffGenerator.GenerateDiff(customMapPath, referenceMapPath);
            return;
        }
    }
}

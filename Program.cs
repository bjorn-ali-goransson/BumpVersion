using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace BumpVersion
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0 && File.Exists("csproj-test.xml"))
            {
                args = new string[] { "csproj-test.xml" };
            }

            if(args.Length == 0 || args.Length > 2)
            {
                Console.WriteLine("Usage: BumpVersion [csproj-file] [version-segment=3]");
                return;
            }

            var segmentNumber = args.Length == 2 ? int.Parse(args[1]) : 3;

            var file = XElement.Load(args[0]);

            foreach (var node in file.Descendants("AssemblyVersion"))
            {
                Console.Write($"{node.Name}: {node.Value} became ");
                node.Value = Bump(node.Value, segmentNumber);
                Console.WriteLine(node.Value);
            }

            foreach (var node in file.Descendants("Version"))
            {
                Console.Write($"{node.Name}: {node.Value} became ");
                node.Value = Bump(node.Value, segmentNumber);
                Console.WriteLine(node.Value);
            }

            file.Save(args[0]);
        }

        static string Bump(string version, int segmentNumber)
        {
            var segments = version.Split('.');

            segments[segmentNumber - 1] = (int.Parse(segments[segmentNumber - 1]) + 1).ToString();

            return string.Join(".", segments);
        }
    }
}

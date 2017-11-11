using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BumpVersion
{
    public static class Extensions
    {
        public static Version Bump(this Version version, int segmentNumber)
        {
            var segments = version.ToString().Split('.');

            segments[segmentNumber - 1] = (int.Parse(segments[segmentNumber - 1]) + 1).ToString();

            return new Version(string.Join(".", segments));
        }
    }
}

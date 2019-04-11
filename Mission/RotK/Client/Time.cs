using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public struct Duration
    {
        private int duration;

        public static implicit operator Duration(int i)
        {
            return new Duration { duration = i };
        }

        public static implicit operator int(Duration d)
        {
            return d.duration;
        }
    }

    public struct Interval
    {
        private int interval;

        public static implicit operator Interval(int i)
        {
            return new Interval { interval = i };
        }

        public static implicit operator int(Interval i)
        {
            return i.interval;
        }
    }

}

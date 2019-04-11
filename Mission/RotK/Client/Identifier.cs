using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public readonly struct Identifier
    {
        public string Title { get; }
        public string Name { get; }

        public Identifier(string Title, string Name)
        {
            this.Title = Title;
            this.Name = Name;
        }
    }
}

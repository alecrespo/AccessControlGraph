using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccessControlGraph.Tests
{
    class TestNode : NodeBase
    {
        public readonly int Id;
        public string Testdata;

        public TestNode(int id)
        {
            Id = id;
        }

        public override bool Equals(object obj)
        {
            return Id == ((TestNode) obj).Id;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }
}

using System.ComponentModel;
using System.Runtime.CompilerServices;
using AccessControlGraph.Tests.Annotations;

namespace AccessControlGraph.Tests
{
    class TestNode : NodeBase, INotifyPropertyChanged
    {
        public readonly int Id;

        private string _testData;
        public string Testdata {
            get
            {
                return _testData;
            }
            set
            {
                _testData = value;
                OnPropertyChanged();
            } 
        }

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

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

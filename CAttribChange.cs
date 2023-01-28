using BuzzGUI.Interfaces;

namespace Snapshot
{
    internal class CAttribChange
    {
        internal readonly IAttribute Attribute;
        private readonly int targetValue;

        public bool Finished { get; private set; }

        public CAttribChange(IAttribute attr, int value)
        {
            Finished = false;
            Attribute = attr;
            targetValue = value;
        }

        public void Work()
        {
            if (Finished) return;
            Attribute.Value = targetValue;
            Finished = true;
        }
    }
}
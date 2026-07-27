namespace EOS.Modules.Tweaks.TerminalTweak
{
    public struct TerminalState
    {
        public bool enabled = true;

        public bool approached = false;

        private int _data = 0;
        private byte _length = 0;

        public TerminalState()
        {
            _data = 0;
            _length = 0;
        }

        public TerminalState(bool[] arr)
        {
            Value = arr;
        }

        public bool[] Value
        {
            readonly get
            {
                bool[] arr = new bool[_length];
                for (int i = 0; i < _length; i++)
                {
                    arr[i] = (_data & (1 << i)) != 0;
                }
                return arr;
            }
            set
            {
                if (value.Length > 32) return;
                _data = 0;
                _length = (byte)value.Length;
                for (int i = 0; i < value.Length; i++)
                {
                    if (value[i]) 
                    { 
                        _data |= 1 << i; 
                    }
                }
            }
        }
    }
}

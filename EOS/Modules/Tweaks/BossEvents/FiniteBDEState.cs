namespace EOS.Modules.Tweaks.BossEvents
{
    public struct FiniteBDEState
    {
        public int applyToHibernateCount = int.MaxValue;

        public int applyToWaveCount = int.MaxValue;

        public FiniteBDEState() { }

        public FiniteBDEState(FiniteBDEState other)
        {
            applyToHibernateCount = other.applyToHibernateCount;
            applyToHibernateCount = other.applyToWaveCount;
        }

        public FiniteBDEState(int hibernateCount, int waveCount)
        {
            applyToHibernateCount = hibernateCount;
            applyToWaveCount = waveCount;
        }
    }
}

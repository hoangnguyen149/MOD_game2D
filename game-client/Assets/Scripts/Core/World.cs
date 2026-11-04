using System;
namespace OwnedGame
{
    public sealed class World
    {
        public float X { get; private set; }
        public float Y { get; private set; }
        public int Hp { get; private set; } = 100;
        public int Mp { get; private set; } = 50;
        public float Speed { get; private set; } = 4;
        public bool Paused { get; set; } = true;
        public void Move(float x, float y, float seconds)
        {
            if (Paused) return;
            if (float.IsNaN(x) || float.IsNaN(y) || float.IsNaN(seconds) || float.IsInfinity(x) || float.IsInfinity(y) || float.IsInfinity(seconds)) return;
            var length = (float)Math.Sqrt(x*x+y*y);
            if (length > 1) { x/=length; y/=length; }
            seconds=Math.Max(0,Math.Min(.1f,seconds));
            X=Math.Max(-9,Math.Min(9,X+x*Speed*seconds));
            Y=Math.Max(-5,Math.Min(5,Y+y*Speed*seconds));
        }
        public void ApplySnapshot(float x, float y, int hp, int mp)
        {
            if(float.IsNaN(x)||float.IsNaN(y)||float.IsInfinity(x)||float.IsInfinity(y)||Math.Abs(x)>9||Math.Abs(y)>5||hp<0||hp>100||mp<0||mp>50)throw new ArgumentException("Invalid snapshot");
            X=x;Y=y;Hp=hp;Mp=mp;
        }
        public void Damage(int amount) { Hp=Math.Max(0,Hp-Math.Max(0,amount)); }
        public bool Cast() { if (Paused || Mp<5) return false; Mp-=5; return true; }
#if UNITY_EDITOR || DEVELOPMENT_BUILD || LOCAL_TESTS
        public void DebugSpeed(float speed) { if (!float.IsNaN(speed) && !float.IsInfinity(speed)) Speed=Math.Max(1,Math.Min(12,speed)); }
        public void DebugHeal() { Hp=100; Mp=50; }
#endif
    }
    // Explicit compiled-plugin contract; no assembly injection or reflection loading.
    public interface IMod { string Id { get; } void Configure(IModHost host); }
    public interface IModHost
    {
        void SetTheme(string theme);
        void ShowFps(bool enabled);
        void Bind(string action, string key);
        void SetHudTitle(string title);
    }
    public sealed class SampleMod : IMod
    {
        public string Id { get { return "sample.hud"; } }
        public void Configure(IModHost host)
        {
            host.SetTheme("ocean"); host.ShowFps(true); host.Bind("cast","Space"); host.SetHudTitle("LOCAL ADVENTURE · MOD");
        }
    }
}

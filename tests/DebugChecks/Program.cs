using OwnedGame;
var w=new World();var count=0;
void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);count++;}
w.DebugSpeed(100);Check(w.Speed==12,"debug upper speed bound");
w.DebugSpeed(-5);Check(w.Speed==1,"debug lower speed bound");
w.DebugSpeed(float.NaN);Check(w.Speed==1,"debug nonfinite speed rejected");
w.Paused=false;w.Damage(95);w.Cast();w.DebugHeal();Check(w.Hp==100 && w.Mp==50,"debug restores HP and MP");
Console.WriteLine($"{count} debug checks passed");

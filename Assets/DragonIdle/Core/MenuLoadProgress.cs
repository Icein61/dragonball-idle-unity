using System;
namespace DragonIdle {
 // A generation token prevents late asynchronous completions from stealing a newer page.
 public sealed class MenuLoadProgress {
  public int Generation {get;private set;}
  public int Total {get;private set;}
  public int Completed {get;private set;}
  public int Missing {get;private set;}
  public bool Active {get;private set;}
  public double Fraction {get{return Total==0?0:(double)Completed/Total;}}
  public int Begin(int total){if(total<1)throw new ArgumentOutOfRangeException("total");Generation++;Total=total;Completed=Missing=0;Active=true;return Generation;}
  public bool IsCurrent(int generation){return Active&&generation==Generation;}
  public bool Advance(int generation,bool missing){if(!IsCurrent(generation)||Completed>=Total)return false;Completed++;if(missing)Missing++;return true;}
  public bool Finish(int generation){if(!IsCurrent(generation)||Completed!=Total)return false;Active=false;return true;}
  public void Cancel(){Generation++;Active=false;}
 }
}

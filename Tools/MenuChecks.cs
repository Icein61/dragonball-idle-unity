using System;
using DragonIdle;
static class MenuChecks {
 public static void Run(Action<bool,string> check){
  var p=new MenuLoadProgress();int first=p.Begin(3);
  check(p.Active&&p.Completed==0&&p.Fraction==0,"M01 loading starts from actual zero completed resources");
  check(!p.Finish(first),"M02 cannot leave loading before work completes");
  p.Advance(first,false);check(Math.Abs(p.Fraction-1.0/3)<.000001,"M03 progress advances exactly once per completed asset");
  p.Advance(first,true);check(p.Missing==1&&p.Completed==2,"M04 missing optional resource completes with diagnostic");
  p.Advance(first,false);check(p.Fraction==1&&p.Finish(first)&&!p.Active,"M05 completed load commits once");
  check(!p.Advance(first,false)&&!p.Finish(first)&&p.Completed==3,"M06 late duplicate completion cannot overflow or commit twice");
  int old=p.Begin(2);p.Cancel();check(!p.Advance(old,false)&&!p.Finish(old)&&!p.Active,"M07 cancelled load cannot navigate after a late completion");
  int current=p.Begin(1);check(!p.Advance(old,true)&&p.Completed==0&&p.Missing==0,"M08 old generation cannot contaminate a newer load");
  p.Advance(current,false);check(p.Finish(current)&&p.Missing==0&&p.Total==1,"M09 new generation replaces prior counters");
  bool invalid=false;try{p.Begin(0);}catch(ArgumentOutOfRangeException){invalid=true;}check(invalid,"M10 invalid task count rejected without invented progress");
 }
}

namespace DragonIdle {
 public partial class DemoGame {
  int bagKind,bagOwner,bagLock;
  string CompareItem(Item item,Item current){
   double attack=(item.kind==1?0:item.Bonus)+(item.gem?.008:0)-(current==null?0:(current.kind==1?0:current.Bonus)+(current.gem?.008:0));
   double defense=(item.kind==1?item.Bonus:0)-(current!=null&&current.kind==1?current.Bonus:0);
   return "攻 "+(attack*100).ToString("+0.#;-0.#;0")+"% 防 "+(defense*100).ToString("+0.#;-0.#;0")+"%";
  }
 }
}

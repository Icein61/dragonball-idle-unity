using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using DragonIdle;
class CharacterDesignExport {
 static void Main(string[] args){
  var rows=Data.Cards.Select(c=>new {cardId=c.Id,name=c.DisplayName,personId=c.PersonId,stage=c.Stage,form=c.Form,rarity=c.Rarity,tags=c.Tags,keywords=c.KeywordText,hp=c.HP,attack=c.Attack,defense=c.Defense,speed=c.Speed,cooldown=c.Cooldown,basic="普攻：单体1.00倍攻击；正常行动，默认可暴击/闪避。",special=c.SpecialText,ultimate=c.UltimateText,passive=c.PassiveText,captain=c.LeaderText,captainTags=c.Captain.Tags,captainHP=c.Captain.Health,captainAttack=c.Captain.Attack,captainDefense=c.Captain.Defense}).ToArray();
  File.WriteAllText(args[0],new JavaScriptSerializer().Serialize(new {version=Data.Version,source="CharacterCatalog.cs / CharacterDesign.cs / SaiyanKits.cs",cards=rows}),new UTF8Encoding(false));
 }
}

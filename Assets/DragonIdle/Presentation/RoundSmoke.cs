using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEngine;
using UI=UnityEngine.UI;
namespace DragonIdle {
 public partial class DemoGame {
  IEnumerator RunRoundSmoke(string[] args){
   int o=Array.IndexOf(args,"-smokeOut");string dir=o>=0&&o+1<args.Length?args[o+1]:Path.GetDirectoryName(savePath);
   Directory.CreateDirectory(dir);
   BeginShellLoad("主页");
   float deadline=Time.realtimeSinceStartup+20;
   while(shellLoad.Active&&Time.realtimeSinceStartup<deadline)yield return null;
   try{
    AssertSmoke(args.Contains("-savePath")&&saveError==""&&Application.productName==Data.GameTitle,"Packaged title/save initialization");
    AssertSmoke(page=="主页"&&!shellLoad.Active&&shellLoad.Missing==0&&shellLoad.Completed==13,"Startup art resources not loaded");
    AssertSmoke(homeRoot.GetComponentsInChildren<UI.Button>().Count(b=>b.name.StartsWith("HomeNav"))==2&&FeatureRoutes.SequenceEqual(new[]{"角色","编队"}),"Obsolete home route visible");
    foreach(string route in new[]{"挑战","养成","装备","召唤"}){NavigateGallery(route);AssertSmoke(page=="主页","Obsolete route entered: "+route);}
    GalleryTestButton("HomeSettings").onClick.Invoke();
    AssertSmoke(dialogRoot!=null&&!GalleryTestButton("HomeNav角色").interactable,"Settings lost modal isolation");
    CloseGalleryDialog();
    AssertSmoke(silentAudio&&!menuMusic.isPlaying,"Smoke must run silent");
    var rules=RoundRules();var boards=RoundBoards().boards;
    AssertSmoke(boards.Select(b=>b.positions.Count(id=>!string.IsNullOrEmpty(id))).SequenceEqual(new[]{5,7,9}),"Board assets missing difficulty sizes");
    AssertSmoke(rules.cards.Length==9&&Data.Rarities.All(rank=>RarityBadge(rank)!=null),"Card configuration or badges missing");
    AssertSmoke(Enumerable.Range(0,Data.Cards.Length).All(i=>PortraitTexture(i)!=null),"Portrait loading regressions");
    GalleryTestButton("HomeNav角色").onClick.Invoke();
    AssertSmoke(page=="角色"&&cardButtons.Count==8,"Owned gallery was not reachable");
    allTab.onClick.Invoke();AssertSmoke(cardButtons.Count==9,"Full gallery list inaccurate");
    int unowned=Array.FindIndex(profile.Owned,owns=>!owns);
    AssertSmoke(unowned>=0&&!cardButtons[unowned].transform.Find("CharacterPower").gameObject.activeSelf,"Unowned power leak");
    cardButtons[0].onClick.Invoke();
    AssertSmoke(galleryDetailOpen&&!galleryListRoot.gameObject.activeSelf,"Character detail was not reachable");
    AssertSmoke(!detailBody.GetComponentsInChildren<UI.Text>().Any(t=>t.text.Contains("速度")),"Old speed displayed");
    GalleryTestButton("DetailTab1").onClick.Invoke();
    var sections=detailBody.GetComponentsInChildren<UI.Text>().Where(t=>t.name=="SectionTitle").Select(t=>t.text).ToArray();
    AssertSmoke(sections.Any(t=>t.StartsWith("追打"))&&sections.Any(t=>t.StartsWith("被动型技能"))&&sections.Any(t=>t.StartsWith("行动型技能")),"Round kit not rendered");
    GalleryTestButton("NextCard").onClick.Invoke();AssertSmoke(selectedCard!=0,"Detail next failed");
    GalleryTestButton("PreviousAvatar").onClick.Invoke();AssertSmoke(selectedCard==0,"Detail previous failed");
    GalleryTestButton("ChooseSlot").onClick.Invoke();
    GalleryTestButton("RoundPick9").onClick.Invoke();
    AssertSmoke(profile.RoundFormation()[8]==0,"Nine-cell detail placement failed");
    GalleryTestButton("BackToCards").onClick.Invoke();NavigateGallery("编队");
    var tile1=(RectTransform)GalleryTestButton("RoundSlot1").transform;
    var tile2=(RectTransform)GalleryTestButton("RoundSlot2").transform;
    var tile4=(RectTransform)GalleryTestButton("RoundSlot4").transform;
    AssertSmoke(tile1.anchoredPosition.x>tile4.anchoredPosition.x&&tile1.anchoredPosition.y>tile2.anchoredPosition.y&&
     roundFormationRoot.GetComponentsInChildren<UI.Button>().Count(b=>b.name.StartsWith("RoundSlot"))==9,"Formation UI lost 3x3 numbering");
    GalleryTestButton("RoundSlot7").onClick.Invoke();
    GalleryTestButton("RoundReserve0").onClick.Invoke();
    AssertSmoke(profile.RoundFormation()[6]==0,"Moving existing card failed at capacity");
    Load();AssertSmoke(saveError==""&&profile.RoundFormation()[6]==0&&profile.Team.Length==6,"Stable-ID grid save roundtrip failed");
    int materials=profile.Materials, wins=profile.Wins, crystals=profile.Crystals;
    GalleryTestButton("RoundTest2").onClick.Invoke();
    AssertSmoke(page==RoundPreviewPage&&roundBattle.Order.Count==14,"Nine-enemy preview did not start");
    AssertSmoke(roundBattle.Order[0].Side==0&&roundBattle.Order[1].Side==1,"Round preview disagrees on opening initiative");
    var p1=(RectTransform)roundPreviewRoot.Find("Field0/BattleCell1");
    var e1=(RectTransform)roundPreviewRoot.Find("Field1/BattleCell1");
    AssertSmoke(p1.anchoredPosition.x>e1.anchoredPosition.x,"Enemy board did not mirror player-side columns");
    GalleryTestButton("StepRound").onClick.Invoke();
    AssertSmoke(roundPaused&&roundLast.Move==RoundMove.Basic&&roundLast.Actor.Side==0,"Preview manual advance failed");
    int events=1;
    while(roundBattle.Result==0&&events++<1000)roundBattle.Step();
    AssertSmoke(roundBattle.Result!=0&&profile.Materials==materials&&profile.Wins==wins&&profile.Crystals==crystals,"Preview mutated rewards or failed to terminate");
    GalleryTestButton("LeaveRoundPreview").onClick.Invoke();AssertSmoke(page=="编队"&&roundBattle==null,"Preview exit retained battle");
    File.WriteAllText(Path.Combine(dir,"smoke-result.json"),"{\"roundCombat\":true,\"formationSave\":true,\"twoRoutes\":true,\"galleryDetails\":true,\"startupArt\":true,\"noRewards\":true,\"soundSilent\":true,\"renderVerified\":false,\"actions\":"+events+"}");
    Application.Quit(0);
   }catch(Exception ex){File.WriteAllText(Path.Combine(dir,"smoke-failure.txt"),ex.ToString());Debug.LogError(ex);Application.Quit(1);}
  }
 }
}

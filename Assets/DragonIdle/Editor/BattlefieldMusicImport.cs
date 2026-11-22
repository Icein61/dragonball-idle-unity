using System;
using UnityEditor;
using UnityEngine;

namespace DragonIdle.Editor {
 sealed class BattlefieldMusicImport:AssetPostprocessor {
  void OnPreprocessAudio(){
   if(!assetPath.StartsWith("Assets/DragonIdle/Resources/Audio/V0141/BGM/",StringComparison.Ordinal))return;
   var importer=(AudioImporter)assetImporter;
   var settings=importer.defaultSampleSettings;
   settings.loadType=AudioClipLoadType.Streaming;
   settings.compressionFormat=AudioCompressionFormat.Vorbis;
   settings.quality=.75f;
   importer.defaultSampleSettings=settings;
  }
 }
}

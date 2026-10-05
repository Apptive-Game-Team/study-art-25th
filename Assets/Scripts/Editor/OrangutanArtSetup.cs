using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
public static class OrangutanArtSetup
{
 [MenuItem("Art Workshop/Apply Orangutan and Banana Art")]
 public static void Apply()
 {
  const string art="Assets/Art/Orangutan/";
  foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{art.TrimEnd('/')})) {
   string path=AssetDatabase.GUIDToAssetPath(guid);
   var importer=(TextureImporter)AssetImporter.GetAtPath(path);
   importer.textureType=TextureImporterType.Sprite; importer.spriteImportMode=SpriteImportMode.Single;
   var ts=new TextureImporterSettings(); importer.ReadTextureSettings(ts); ts.spriteAlignment=9; ts.spritePivot=new Vector2(.5f,.03f); importer.SetTextureSettings(ts); importer.spritePixelsPerUnit=64; importer.filterMode=FilterMode.Point; importer.textureCompression=TextureImporterCompression.Uncompressed;
   importer.alphaIsTransparency=true; importer.mipmapEnabled=false; importer.SaveAndReimport();
  }
  var frames=new Dictionary<string,string[]> {
   {"Idle",new[]{"뜬 눈","감은 눈","뜬 눈"}}, {"Move",new[]{"걷기 1","걷기 2","걷기 1"}},
   {"Hit",new[]{"윽 1","윽 2","윽 1"}}, {"Attack",new[]{"걷기 1","바나나킥","바나나킥","걷기 1"}}, {"Death",new[]{"주금","주금"}}
  };
  const string animDir="Assets/Animations/Orangutan";
  if(!AssetDatabase.IsValidFolder(animDir)) AssetDatabase.CreateFolder("Assets/Animations","Orangutan");
  var baseController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animations/CharacterPrototype.controller");
  var controller=new AnimatorOverrideController(baseController);
  var overrides=new List<KeyValuePair<AnimationClip,AnimationClip>>();controller.GetOverrides(overrides);
  for(int i=0;i<overrides.Count;i++) {
   string state=overrides[i].Key.name; if(!frames.ContainsKey(state))continue;
   string clipPath=animDir+"/"+state+".anim";
   var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
   if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,clipPath);}
   clip.frameRate=8;
   var keys=new ObjectReferenceKeyframe[frames[state].Length];
   for(int j=0;j<keys.Length;j++) keys[j]=new ObjectReferenceKeyframe{time=j*.22f,value=AssetDatabase.LoadAssetAtPath<Sprite>(art+frames[state][j]+".png")};
   AnimationUtility.SetObjectReferenceCurve(clip,new EditorCurveBinding{path="",type=typeof(SpriteRenderer),propertyName="m_Sprite"},keys);
   AnimationUtility.SetEditorCurve(clip,new EditorCurveBinding{path="",type=typeof(Transform),propertyName="m_LocalPosition.y"},AnimationCurve.Constant(0,(keys.Length-1)*.22f,-.75f));
   var settings=AnimationUtility.GetAnimationClipSettings(clip);settings.loopTime=state=="Idle"||state=="Move";AnimationUtility.SetAnimationClipSettings(clip,settings);
   overrides[i]=new KeyValuePair<AnimationClip,AnimationClip>(overrides[i].Key,clip);EditorUtility.SetDirty(clip);
  }
  controller.ApplyOverrides(overrides);
  const string ctrlPath=animDir+"/Orangutan.overrideController";
  var saved=AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ctrlPath);
  if(saved==null)AssetDatabase.CreateAsset(controller,ctrlPath);else{EditorUtility.CopySerialized(controller,saved);Object.DestroyImmediate(controller);controller=saved;}
  var root=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Player.prefab");
  try{var visual=root.transform.Find("Visual");visual.GetComponent<Animator>().runtimeAnimatorController=controller;visual.GetComponent<SpriteRenderer>().sprite=AssetDatabase.LoadAssetAtPath<Sprite>(art+"뜬 눈.png");
   var face=root.transform.Find("Face");if(face!=null)face.gameObject.SetActive(false);
   PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Player.prefab");
  }finally{PrefabUtility.UnloadPrefabContents(root);}
  var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Workshop/BananaHit.mat");var texture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/UI/Banana.png");
  if(mat.HasProperty("_BaseMap"))mat.SetTexture("_BaseMap",texture);if(mat.HasProperty("_MainTex"))mat.SetTexture("_MainTex",texture);EditorUtility.SetDirty(mat);
  AssetDatabase.SaveAssets();Debug.Log("Orangutan animations and banana particle texture applied.");
 }
}

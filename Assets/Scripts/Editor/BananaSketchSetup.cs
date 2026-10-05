using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public static class BananaSketchSetup {
 static Sprite[] Slice(string path,bool banner){
 AssetDatabase.ImportAsset(path);var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Multiple;ti.maxTextureSize=8192;ti.isReadable=true;ti.filterMode=FilterMode.Point;ti.mipmapEnabled=false;ti.alphaIsTransparency=true;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.SaveAndReimport();var t=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var px=t.GetPixels32();int x0=t.width,y0=t.height,x1=0,y1=0;
 for(int y=0;y<t.height;y++)for(int x=0;x<t.width;x++)if(px[y*t.width+x].a>32){x0=Mathf.Min(x0,x);x1=Mathf.Max(x1,x);y0=Mathf.Min(y0,y);y1=Mathf.Max(y1,y);}
 float w=x1-x0+1,h=y1-y0+1;var rects=new SpriteRect[banner?3:1];var pairs=new SpriteNameFileIdPair[rects.Length];
 for(int i=0;i<rects.Length;i++){string n=banner?new[]{"Left","Center","Right"}[i]:"Button";Rect r=!banner?new Rect(x0,y0,w,h):i==0?new Rect(x0,y0,w*.22f,h):i==1?new Rect(x0+w*.22f,y0,w*.56f,h):new Rect(x0+w*.78f,y0,w*.22f,h);rects[i]=new SpriteRect{name=n,rect=r,pivot=new Vector2(.5f,.5f),alignment=SpriteAlignment.Center,spriteID=GUID.Generate(),border=i==1&&banner?new Vector4(0,h*.2f,0,h*.2f):Vector4.zero};pairs[i]=new SpriteNameFileIdPair(n,rects[i].spriteID);}
 var f=new SpriteDataProviderFactories();f.Init();var d=f.GetSpriteEditorDataProviderFromObject(ti);d.InitSpriteEditorDataProvider();d.SetSpriteRects(rects);d.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(pairs);d.Apply();ti.SaveAndReimport();var result=new Sprite[rects.Length];foreach(var o in AssetDatabase.LoadAllAssetsAtPath(path))if(o is Sprite s)for(int i=0;i<rects.Length;i++)if(s.name==rects[i].name)result[i]=s;return result;
 }
 [MenuItem("Art Workshop/Apply Sketch Banana UI")]
 public static void Apply(){var skin=Slice("Assets/Art/UI/BananaSketchPanel.png",true);var button=Slice("Assets/Art/UI/BananaLyingButton.png",false)[0];
 var root=PrefabUtility.LoadPrefabContents("Assets/Prefabs/WorkshopCanvas.prefab");try{
 foreach(var im in root.GetComponentsInChildren<Image>(true)){
 if(!im)continue;
 if(im.name!="InputPanel"&&im.name!="BattleInfo"&&im.name!="CardBackground"&&im.name!="ReturnToTownButton"&&im.name!="DefeatPanel")continue;
 foreach(string old in new[]{"BananaScrollLeft","BananaScrollRight"}){var child=im.transform.Find(old);if(child)Object.DestroyImmediate(child.gameObject);}
 if(im.name=="ReturnToTownButton"){im.sprite=button;im.type=Image.Type.Simple;im.preserveAspect=true;im.color=Color.white;im.rectTransform.sizeDelta=new Vector2(380,94);continue;}
 im.sprite=skin[1];im.type=Image.Type.Sliced;im.color=Color.white;im.preserveAspect=false;im.pixelsPerUnitMultiplier=5;
 var rt=im.rectTransform;float height=rt.rect.height;if(im.name=="InputPanel"){rt.sizeDelta=new Vector2(830,64);height=64;}if(im.name=="BattleInfo"){rt.sizeDelta=new Vector2(680,100);height=100;}
 if(im.name=="DefeatPanel"){rt.sizeDelta=new Vector2(640,240);height=240;}
 float endHeight=im.name=="CardBackground"?height*.65f:height;float endWidth=endHeight*skin[0].rect.width/skin[0].rect.height;
 for(int i=0;i<2;i++){string name=i==0?"BananaCurveLeft":"BananaCurveRight";var old=im.transform.Find(name);var go=old?old.gameObject:new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(rt,false);var r=go.GetComponent<RectTransform>();r.anchorMin=r.anchorMax=new Vector2(i,.5f);r.pivot=new Vector2(i,.5f);r.anchoredPosition=Vector2.zero;r.sizeDelta=new Vector2(endWidth,endHeight);var image=go.GetComponent<Image>();image.sprite=skin[i==0?0:2];image.color=Color.white;image.preserveAspect=true;image.raycastTarget=false;}
 }
 var title=root.transform.Find("DefeatPanel/DefeatTitle").GetComponent<TextMeshProUGUI>();title.text="오늘은 여기까지! 가족에게 돌아가자";title.rectTransform.sizeDelta=new Vector2(420,60);title.rectTransform.anchoredPosition=new Vector2(0,65);title.color=new Color(.25f,.12f,.025f,1);title.enableAutoSizing=true;title.fontSizeMin=16;title.fontSizeMax=24;
 var label=root.transform.Find("DefeatPanel/ReturnToTownButton/ReturnLabel").GetComponent<TextMeshProUGUI>();label.text="가족에게 돌아가기";label.rectTransform.anchoredPosition=new Vector2(0,-12);label.color=new Color(.25f,.12f,.025f,1);label.enableAutoSizing=true;label.fontSizeMin=14;label.fontSizeMax=20;
 PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/WorkshopCanvas.prefab");}finally{PrefabUtility.UnloadPrefabContents(root);}AssetDatabase.SaveAssets();Debug.Log("Sketch-inspired curved banana UI and lying banana button applied");}
}

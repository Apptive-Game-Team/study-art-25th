using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public static class BananaStretchSetup {
 static Sprite[] Import(){
  const string path="Assets/Art/UI/BananaStretchPanel.png";AssetDatabase.ImportAsset(path);
  var ti=(TextureImporter)AssetImporter.GetAtPath(path);ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Multiple;ti.maxTextureSize=8192;ti.isReadable=true;ti.filterMode=FilterMode.Point;ti.mipmapEnabled=false;ti.alphaIsTransparency=true;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.SaveAndReimport();
  var t=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var px=t.GetPixels32();int x0=t.width,y0=t.height,x1=0,y1=0;
  for(int y=0;y<t.height;y++)for(int x=0;x<t.width;x++)if(px[y*t.width+x].a>32){x0=Mathf.Min(x0,x);x1=Mathf.Max(x1,x);y0=Mathf.Min(y0,y);y1=Mathf.Max(y1,y);}
  float w=x1-x0+1,h=y1-y0+1;var names=new[]{"LeftBanana","RightBanana","BeigeCenter","TopCurl","BottomCurl"};
  // Keep the full vertical coordinate range in every piece so the border lines
  // meet at exactly the same height. The transparent margins are intentional.
  var regions=new[]{new Rect(x0,y0,w*.18f,h),new Rect(x0+w*.82f,y0,w*.18f,h),new Rect(x0+w*.26f,y0,w*.1f,h),new Rect(x0+w*.4f,y0+h*.70f,w*.2f,h*.30f),new Rect(x0+w*.4f,y0,w*.2f,h*.30f)};
  var rects=new SpriteRect[5];var pairs=new SpriteNameFileIdPair[5];for(int i=0;i<5;i++){rects[i]=new SpriteRect{name=names[i],rect=regions[i],pivot=new Vector2(.5f,.5f),alignment=SpriteAlignment.Center,spriteID=GUID.Generate(),border=i<2?new Vector4(0,h*.45f,0,h*.45f):i==2?new Vector4(0,h*.30f,0,h*.30f):Vector4.zero};pairs[i]=new SpriteNameFileIdPair(names[i],rects[i].spriteID);}
  var f=new SpriteDataProviderFactories();f.Init();var d=f.GetSpriteEditorDataProviderFromObject(ti);d.InitSpriteEditorDataProvider();d.SetSpriteRects(rects);d.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(pairs);d.Apply();ti.SaveAndReimport();var result=new Sprite[5];foreach(var o in AssetDatabase.LoadAllAssetsAtPath(path))if(o is Sprite s)for(int i=0;i<5;i++)if(s.name==names[i])result[i]=s;return result;
 }
 static Image Child(Transform parent,string name,Sprite sprite){var child=parent.Find(name);var go=child?child.gameObject:new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var image=go.GetComponent<Image>();image.sprite=sprite;image.color=Color.white;image.raycastTarget=false;image.preserveAspect=true;go.transform.SetAsFirstSibling();return image;}
 [MenuItem("Art Workshop/Apply Stretch Banana Panels")]
 public static void Apply(){var skin=Import();var root=PrefabUtility.LoadPrefabContents("Assets/Prefabs/WorkshopCanvas.prefab");try{
  foreach(var im in root.GetComponentsInChildren<Image>(true)){
   if(!im)continue;if(im.name!="InputPanel"&&im.name!="BattleInfo"&&im.name!="CardBackground"&&im.name!="DefeatPanel")continue;
   foreach(string old in new[]{"BananaCurveLeft","BananaCurveRight","BananaScrollLeft","BananaScrollRight"}){var child=im.transform.Find(old);if(child)Object.DestroyImmediate(child.gameObject);}
   im.sprite=null;im.color=Color.clear;var rt=im.rectTransform;
   float height=rt.rect.height;
   // All panels use the same pixel scale: preserve banana thickness and
   // curved ends, stretch only the narrow straight middle section vertically.
   float edgeScale=64/skin[0].rect.height;
   float capWidth=skin[0].rect.width*edgeScale;
   // Equal vertical scale for cap ends, body border and curls; only their
   // middle sections grow vertically on tall cards.
   float multiplier=100/(skin[0].pixelsPerUnit*edgeScale);
   for(int i=0;i<2;i++){var image=Child(rt,i==0?"FixedBananaLeft":"FixedBananaRight",skin[i]);image.type=Image.Type.Sliced;image.preserveAspect=false;image.pixelsPerUnitMultiplier=multiplier;var r=image.rectTransform;r.anchorMin=new Vector2(i,0);r.anchorMax=new Vector2(i,1);r.pivot=new Vector2(i,.5f);r.anchoredPosition=Vector2.zero;r.sizeDelta=new Vector2(capWidth,0);}
   for(int i=0;i<2;i++){var image=Child(rt,i==0?"FixedGoldCurlTop":"FixedGoldCurlBottom",skin[3+i]);var r=image.rectTransform;r.anchorMin=r.anchorMax=new Vector2(.5f,1-i);r.pivot=new Vector2(.5f,1-i);r.anchoredPosition=Vector2.zero;r.sizeDelta=new Vector2(skin[3+i].rect.width*edgeScale,skin[3+i].rect.height*edgeScale);}
   var center=Child(rt,"StretchBeigeCenter",skin[2]);center.type=Image.Type.Sliced;center.preserveAspect=false;center.pixelsPerUnitMultiplier=multiplier;var cr=center.rectTransform;cr.anchorMin=Vector2.zero;cr.anchorMax=Vector2.one;cr.offsetMin=new Vector2(capWidth*.9f-1,0);cr.offsetMax=new Vector2(-capWidth*.9f+1,0);
   if(im.name=="InputPanel"){rt.anchorMin=new Vector2(0,rt.anchorMin.y);rt.anchorMax=new Vector2(1,rt.anchorMax.y);rt.sizeDelta=new Vector2(-450,64);}
   if(im.name=="BattleInfo"){rt.anchorMin=new Vector2(0,rt.anchorMin.y);rt.anchorMax=new Vector2(1,rt.anchorMax.y);rt.sizeDelta=new Vector2(-600,100);}
  }
  foreach(var text in root.GetComponentsInChildren<TextMeshProUGUI>(true)){text.color=Color.black;text.faceColor=Color.black;text.fontStyle|=FontStyles.Bold;}
  PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/WorkshopCanvas.prefab");
 }finally{PrefabUtility.UnloadPrefabContents(root);}AssetDatabase.SaveAssets();Debug.Log("Fixed banana caps, centered gold curls, stretch beige panels and black text applied.");}
}

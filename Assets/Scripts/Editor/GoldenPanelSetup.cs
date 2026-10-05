using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class GoldenPanelSetup {
 [MenuItem("Art Workshop/Apply Beige Gold Panels")]
 public static void Apply() {
  const string path="Assets/Art/UI/BeigeGoldPanel.png";
  AssetDatabase.ImportAsset(path);
  var ti=(TextureImporter)AssetImporter.GetAtPath(path);
  ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Multiple;
  ti.maxTextureSize=8192;ti.isReadable=true;ti.filterMode=FilterMode.Point;
  ti.mipmapEnabled=false;ti.alphaIsTransparency=true;
  ti.textureCompression=TextureImporterCompression.Uncompressed;ti.spritePixelsPerUnit=500;
  ti.SaveAndReimport();
  var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(path);var pixels=texture.GetPixels32();
  int x0=texture.width,y0=texture.height,x1=0,y1=0;
  for(int y=0;y<texture.height;y++)for(int x=0;x<texture.width;x++)if(pixels[y*texture.width+x].a>32){x0=Mathf.Min(x0,x);x1=Mathf.Max(x1,x);y0=Mathf.Min(y0,y);y1=Mathf.Max(y1,y);}
  float h=y1-y0+1,border=h*.28f;
  var rect=new SpriteRect{name="BeigeGold",rect=new Rect(x0,y0,x1-x0+1,h),pivot=new Vector2(.5f,.5f),alignment=SpriteAlignment.Center,spriteID=GUID.Generate(),border=new Vector4(border,border,border,border)};
  var factories=new SpriteDataProviderFactories();factories.Init();var provider=factories.GetSpriteEditorDataProviderFromObject(ti);provider.InitSpriteEditorDataProvider();
  provider.SetSpriteRects(new[]{rect});provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(new[]{new SpriteNameFileIdPair(rect.name,rect.spriteID)});provider.Apply();ti.SaveAndReimport();
  Sprite sprite=null;foreach(var obj in AssetDatabase.LoadAllAssetsAtPath(path))if(obj is Sprite s)sprite=s;
  var root=PrefabUtility.LoadPrefabContents("Assets/Prefabs/WorkshopCanvas.prefab");
  try {
   foreach(var image in root.GetComponentsInChildren<Image>(true)) {
    if(!image)continue;
    if(image.name!="InputPanel"&&image.name!="BattleInfo"&&image.name!="CardBackground"&&image.name!="DefeatPanel")continue;
    foreach(string old in new[]{"BananaCurveLeft","BananaCurveRight","BananaScrollLeft","BananaScrollRight"}){var child=image.transform.Find(old);if(child)Object.DestroyImmediate(child.gameObject);}
    image.sprite=sprite;image.type=Image.Type.Sliced;image.preserveAspect=false;image.color=Color.white;image.pixelsPerUnitMultiplier=1;
    if(image.name=="InputPanel")image.rectTransform.sizeDelta=new Vector2(830,64);
    if(image.name=="BattleInfo")image.rectTransform.sizeDelta=new Vector2(680,100);
    if(image.name=="DefeatPanel")image.rectTransform.sizeDelta=new Vector2(640,240);
    foreach(var text in image.GetComponentsInChildren<TextMeshProUGUI>(true))text.color=new Color(.23f,.13f,.045f,1);
   }
   PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/WorkshopCanvas.prefab");
  } finally {PrefabUtility.UnloadPrefabContents(root);}
  AssetDatabase.SaveAssets();Debug.Log("Beige panels with fixed-size gold spiral corners applied.");
 }
}

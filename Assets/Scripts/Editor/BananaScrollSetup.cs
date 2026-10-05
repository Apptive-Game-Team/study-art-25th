using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public static class BananaScrollSetup {
 [MenuItem("Art Workshop/Apply Banana Scroll Layout")]
 public static void Apply(){
 const string path="Assets/Art/UI/BananaScrollEnd.png";AssetDatabase.ImportAsset(path);var ti=(TextureImporter)AssetImporter.GetAtPath(path);
 ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Single;ti.filterMode=FilterMode.Point;ti.mipmapEnabled=false;ti.alphaIsTransparency=true;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.SaveAndReimport();var end=AssetDatabase.LoadAssetAtPath<Sprite>(path);
 var root=PrefabUtility.LoadPrefabContents("Assets/Prefabs/WorkshopCanvas.prefab");try{
 foreach(var im in root.GetComponentsInChildren<Image>(true)){
 if(im.name!="InputPanel"&&im.name!="BattleInfo"&&im.name!="CardBackground"&&im.name!="ReturnToTownButton")continue;
 im.sprite=null;im.color=new Color(1,.94f,.76f,1);im.type=Image.Type.Simple;im.preserveAspect=false;
 var rt=im.rectTransform;if(im.name=="BattleInfo")rt.sizeDelta=new Vector2(680,100);if(im.name=="InputPanel")rt.sizeDelta=new Vector2(830,48);
 float capWidth=im.name=="CardBackground"?14:24;
 for(int i=0;i<2;i++){string n=i==0?"BananaScrollLeft":"BananaScrollRight";var existing=im.transform.Find(n);GameObject go=existing?existing.gameObject:new GameObject(n,typeof(RectTransform),typeof(Image));go.transform.SetParent(im.transform,false);var r=go.GetComponent<RectTransform>();r.anchorMin=new Vector2(i,0);r.anchorMax=new Vector2(i,1);r.pivot=new Vector2(.5f,.5f);r.anchoredPosition=new Vector2(i==0?capWidth*.5f:-capWidth*.5f,0);r.sizeDelta=new Vector2(capWidth,6);r.localScale=new Vector3(i==0?1:-1,1,1);var image=go.GetComponent<Image>();image.sprite=end;image.color=Color.white;image.preserveAspect=true;image.raycastTarget=false;}
 foreach(var txt in im.GetComponentsInChildren<TextMeshProUGUI>(true)){txt.color=new Color(.24f,.12f,.035f,1);if(im.name=="BattleInfo"||im.name=="InputPanel"||im.name=="ReturnToTownButton"){txt.enableAutoSizing=true;txt.fontSizeMin=14;txt.fontSizeMax=20;}}
 }
 var input=root.transform.Find("InputText");if(input)input.GetComponent<TextMeshProUGUI>().color=new Color(.24f,.12f,.035f,1);
 PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/WorkshopCanvas.prefab");
 }finally{PrefabUtility.UnloadPrefabContents(root);}AssetDatabase.SaveAssets();Debug.Log("Banana scroll: independent fixed ends, flexible cream center applied");
 }
}

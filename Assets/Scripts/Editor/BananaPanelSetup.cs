using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public static class BananaPanelSetup {
 [MenuItem("Art Workshop/Apply Sliced Banana Panels")]
 public static void Apply(){
 const string p="Assets/Art/UI/BananaPanel.png";
 AssetDatabase.ImportAsset(p);var ti=(TextureImporter)AssetImporter.GetAtPath(p);
 ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Multiple;ti.isReadable=true;ti.filterMode=FilterMode.Point;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.mipmapEnabled=false;ti.SaveAndReimport();
 var t=AssetDatabase.LoadAssetAtPath<Texture2D>(p);var pixels=t.GetPixels32();int x0=t.width,y0=t.height,x1=0,y1=0;
 for(int y=0;y<t.height;y++)for(int x=0;x<t.width;x++)if(pixels[y*t.width+x].a>32){x0=Mathf.Min(x0,x);y0=Mathf.Min(y0,y);x1=Mathf.Max(x1,x);y1=Mathf.Max(y1,y);}
 #pragma warning disable 0618
 ti.spritesheet=new[]{new SpriteMetaData{name="BananaPanel",rect=new Rect(x0,y0,x1-x0+1,y1-y0+1),pivot=new Vector2(.5f,.5f),alignment=0,border=new Vector4((x1-x0)*.18f,(y1-y0)*.23f,(x1-x0)*.18f,(y1-y0)*.23f)}};
 #pragma warning restore 0618
 ti.spritePixelsPerUnit=100;ti.SaveAndReimport();Sprite sprite=null;foreach(var a in AssetDatabase.LoadAllAssetsAtPath(p))if(a is Sprite sp)sprite=sp;
 if(sprite==null)throw new System.Exception("Banana panel sprite import failed");
 var root=PrefabUtility.LoadPrefabContents("Assets/Prefabs/WorkshopCanvas.prefab");try{
 foreach(var im in root.GetComponentsInChildren<Image>(true))if(im.name=="InputPanel"||im.name=="ReturnToTownButton") {im.sprite=sprite;im.type=Image.Type.Sliced;im.color=Color.white;im.pixelsPerUnitMultiplier=10; if(im.name=="InputPanel") {var rt=im.rectTransform;rt.sizeDelta=new Vector2(rt.sizeDelta.x,64);}}
 foreach(var txt in root.GetComponentsInChildren<TextMeshProUGUI>(true))if(txt.transform.IsChildOf(root.transform.Find("InputPanel"))||txt.transform.IsChildOf(root.transform.Find("BattleInfo"))||txt.name=="ReturnLabel")txt.color=new Color(.23f,.12f,.045f,1);
 foreach(var im in root.GetComponentsInChildren<Image>(true))if(im.name=="BananaIcon")im.gameObject.SetActive(false);
 PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/WorkshopCanvas.prefab");
 }finally{PrefabUtility.UnloadPrefabContents(root);}AssetDatabase.SaveAssets();Debug.Log("Sliced banana panels applied");
 }
}

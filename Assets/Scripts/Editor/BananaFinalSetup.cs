using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor.U2D.Sprites;
public static class BananaFinalSetup {
 [MenuItem("Art Workshop/Apply Banana Transparency and Variants")]
 public static void Apply(){
 var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Workshop/BananaHit.mat");
 mat.SetFloat("_Surface",1);mat.SetFloat("_SrcBlend",5);mat.SetFloat("_DstBlend",10);mat.SetFloat("_ZWrite",0);mat.SetOverrideTag("RenderType","Transparent");mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");mat.renderQueue=3000;EditorUtility.SetDirty(mat);
 const string hit="Assets/Prefabs/HitEffect1.prefab",attack="Assets/Prefabs/BananaAttack.prefab";
 if(AssetDatabase.LoadAssetAtPath<GameObject>(attack)==null)AssetDatabase.CopyAsset(hit,attack);
 var a=PrefabUtility.LoadPrefabContents(attack);try{var ps=a.GetComponent<ParticleSystem>();var main=ps.main;main.startSize=.28f;main.startLifetime=.22f;main.startSpeed=1.2f;var emission=ps.emission;emission.SetBursts(new[]{new ParticleSystem.Burst(0,3)});PrefabUtility.SaveAsPrefabAsset(a,attack);}finally{PrefabUtility.UnloadPrefabContents(a);}
 foreach(string path in new[]{"Assets/Prefabs/Player.prefab","Assets/Prefabs/Enemy.prefab"}){
 var root=PrefabUtility.LoadPrefabContents(path);try{var combat=root.GetComponent<ArtUnityWorkshop.Combatant>();var so=new SerializedObject(combat);so.FindProperty("hitParticlePrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(hit).GetComponent<ParticleSystem>();if(path.Contains("Player"))so.FindProperty("attackParticlePrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<GameObject>(attack).GetComponent<ParticleSystem>();so.ApplyModifiedPropertiesWithoutUndo();PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}}
 const string img="Assets/Art/UI/BananaVariants.png";AssetDatabase.ImportAsset(img);var ti=(TextureImporter)AssetImporter.GetAtPath(img);if(ti==null){AssetDatabase.SaveAssets();return;}
 ti.textureType=TextureImporterType.Sprite;ti.spriteImportMode=SpriteImportMode.Multiple;ti.isReadable=true;ti.filterMode=FilterMode.Point;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.mipmapEnabled=false;ti.SaveAndReimport();var t=AssetDatabase.LoadAssetAtPath<Texture2D>(img);var px=t.GetPixels32();var metas=new SpriteMetaData[2]; int cut=t.width/2,best=0,start=0; for(int x=t.width/5;x<t.width*3/4;x++){bool filled=false;for(int y=0;y<t.height;y++)if(px[y*t.width+x].a>32){filled=true;break;}if(filled)start=x+1;else if(x-start>best){best=x-start;cut=(start+x)/2;}}
 for(int k=0;k<2;k++){int lo=k==0?0:cut,hi=k==0?cut:t.width,x0=hi,y0=t.height,x1=lo,y1=0;for(int y=0;y<t.height;y++)for(int x=lo;x<hi;x++)if(px[y*t.width+x].a>32){x0=Mathf.Min(x0,x);x1=Mathf.Max(x1,x);y0=Mathf.Min(y0,y);y1=Mathf.Max(y1,y);}
 metas[k]=new SpriteMetaData{name=k==0?"BananaCard":"BananaInfo",rect=new Rect(x0,y0,x1-x0+1,y1-y0+1),alignment=0,pivot=new Vector2(.5f,.5f)};}
 #pragma warning disable 0618
 var factory=new SpriteDataProviderFactories();factory.Init();var provider=factory.GetSpriteEditorDataProviderFromObject(ti);provider.InitSpriteEditorDataProvider();var rects=new SpriteRect[2];for(int j=0;j<2;j++)rects[j]=new SpriteRect{name=metas[j].name,rect=metas[j].rect,pivot=new Vector2(.5f,.5f),alignment=SpriteAlignment.Center,spriteID=GUID.Generate()};provider.SetSpriteRects(rects);provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(new[]{new SpriteNameFileIdPair(rects[0].name,rects[0].spriteID),new SpriteNameFileIdPair(rects[1].name,rects[1].spriteID)});provider.Apply();
 #pragma warning restore 0618
 ti.SaveAndReimport();Sprite card=null,info=null;foreach(var obj in AssetDatabase.LoadAllAssetsAtPath(img))if(obj is Sprite sp){if(sp.name=="BananaCard")card=sp;else info=sp;}
 var canvas=PrefabUtility.LoadPrefabContents("Assets/Prefabs/WorkshopCanvas.prefab");try{foreach(var im in canvas.GetComponentsInChildren<Image>(true))if(im.name=="CardBackground"||im.name=="BattleInfo") {im.sprite=im.name=="CardBackground"?card:info;im.color=Color.white;im.type=Image.Type.Simple;im.preserveAspect=true;}PrefabUtility.SaveAsPrefabAsset(canvas,"Assets/Prefabs/WorkshopCanvas.prefab");}finally{PrefabUtility.UnloadPrefabContents(canvas);}AssetDatabase.SaveAssets();Debug.Log("Banana panel variants and transparent attack/hit effects applied");
 }
}

// Unity CLI eval_file ile edit modda çalıştırılır. Kaynak PNG korunur.
var source="Assets/Corrigible/Desk/Art/Sade/Textures/tx_lamp_desk.png";
var destination="Assets/Corrigible/Desk/Art/Sade/Textures/tx_lamp_desk_linear.png";
var texture=new UnityEngine.Texture2D(2,2,UnityEngine.TextureFormat.RGBA32,false);
try
{
    texture.LoadImage(System.IO.File.ReadAllBytes(source));
    var pixels=texture.GetPixels32();
    for(int i=0;i<pixels.Length;i++)
    {
        UnityEngine.Color color=pixels[i];
        if(color.a<=0 || color.a>=1)continue;
        color.a=Corrigible.Desk.DeskMath.LinearOverlayAlpha(color,color.a,.75f);
        pixels[i]=color;
    }
    texture.SetPixels32(pixels);
    System.IO.File.WriteAllBytes(destination,texture.EncodeToPNG());
}
finally { UnityEngine.Object.DestroyImmediate(texture); }
UnityEditor.AssetDatabase.ImportAsset(destination,UnityEditor.ImportAssetOptions.ForceUpdate);
return destination;

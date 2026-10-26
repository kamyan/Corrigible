using TMPro;
using UnityEngine;

namespace Corrigible.Desk.EditorTools
{
    /// <summary>Sağ CRT masası: tüplü monitör, klavye, tüplü telsiz (room.js › CRT desk).</summary>
    public static partial class SiginakRoomBuilder
    {
        static void CrtDesk(Transform root, Refs refs)
        {
            var cd = Group(root, "CRT desk", 1.50f, 0, 1.72f, 35);
            Box(cd, "Top", 0.95f, 0.04f, 0.58f, M("olive"), 0, 0.74f, 0);
            Box(cd, "Pedestal", 0.34f, 0.70f, 0.54f, M("steel"), 0.28f, 0.36f, 0);
            foreach (var (x, z) in new[] { (-0.44f, -0.25f), (-0.44f, 0.25f) }) Box(cd, "Leg", 0.035f, 0.72f, 0.035f, M("steelDark"), x, 0.36f, z);

            var crt = Node(cd, "CRT", new Vector3(-0.10f, 0.76f, 0.04f), Quaternion.identity);
            Box(crt, "Body", 0.42f, 0.34f, 0.36f, M("crtCase"), 0, 0.17f, 0);
            Box(crt, "Rear", 0.30f, 0.26f, 0.18f, M("crtCase"), 0, 0.16f, 0.26f);
            Box(crt, "Bezel", 0.34f, 0.26f, 0.01f, M("crtBezel"), 0, 0.185f, -0.181f);
            refs.CrtScreen = Node(crt, "Screen", new Vector3(0, 0.19f, -0.187f), Quaternion.identity);
            Plane(refs.CrtScreen, "Glass", 0.30f, 0.225f, M("crtScreen"), 0, 0, 0);
            // Ekran içeriği CrtScreen'dir (Aşama 7, BuildCrtScreen)

            PlateWithText(crt, "Plate", 0.20f, 0.026f, new Vector3(0, 0.035f, -0.1815f), Quaternion.identity, (SiginakRoomWords.CrtPlate, 0.53f, 0.58f));
            Sphere(crt, "Power LED", 0.006f, M("crtLed"), 0.17f, 0.035f, -0.184f, cast: false);
            foreach (var kx in new[] { 0.13f, 0.155f }) Cyl(crt, "Knob", 0.008f, 0.012f, M("bakelite"), kx - 0.27f, 0.035f, -0.186f, rx: PI / 2, seg: 14);

            var kb = Node(cd, "Keyboard", new Vector3(-0.10f, 0.776f, -0.27f), R(5 * D2R, 0, 0));
            Box(kb, "Body", 0.42f, 0.028f, 0.15f, M("crtCase"), 0, 0, 0);
            Plane(kb, "Keys", 0.42f, 0.15f, SiginakMaterials.Textured("Keyboard keys", "tx_keyboard", "keyboard"), 0, 0.0141f, 0, rx: -PI / 2);

            var radio = Node(cd, "Radio", new Vector3(0.30f, 0.76f, -0.06f), Quaternion.identity);
            refs.Radio = radio;
            Box(radio, "Body", 0.30f, 0.20f, 0.20f, M("radioBody"), 0, 0.10f, 0);
            Plane(radio, "Grille", 0.12f, 0.10f, M("radioGrille"), -0.07f, 0.115f, -0.1005f);
            Plane(radio, "Dial", 0.12f, 0.024f, SiginakMaterials.Glow("Radio dial", "tx_radio_dial", "materialsExtra.radioDial.emissive",
                T.NumberOf("materialsExtra.radioDial.emissiveIntensity", 0.9f)), 0.07f, 0.14f, -0.1005f);
            foreach (var kx in new[] { 0.04f, 0.10f }) Cyl(radio, "Knob", 0.02f, 0.02f, M("bakelite"), kx, 0.07f, -0.11f, rx: PI / 2, r2: 0.018f, seg: 18);
            Cyl(radio, "Antenna", 0.002f, 0.35f, M("chrome"), 0.12f, 0.36f, 0.06f, rz: -0.3f, seg: 6);
        }
    }
}

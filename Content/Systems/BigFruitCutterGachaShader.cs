using Microsoft.Xna.Framework.Graphics;
using Terraria.ModLoader;

namespace BigFruitMunch.Content.Systems
{
    public sealed class BigFruitCutterGachaShader : ModSystem
    {
        [VaultLoaden("BigFruitMunch/Assets/Effects/")]
        public static Effect BigFruitCutterGachaPanel { get; set; }
    }
}

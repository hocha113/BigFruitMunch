global using InnoVault;
global using Microsoft.Xna.Framework;
using BigFruitMunch.Content.Systems;
using System.IO;
using Terraria.ModLoader;

namespace BigFruitMunch
{
    public class BigFruitMunch : Mod
    {
        public override void HandlePacket(BinaryReader reader, int whoAmI) {
            PalmShakeMessage message = (PalmShakeMessage)reader.ReadByte();
            switch (message) {
                case PalmShakeMessage.ShakeRequest:
                    PalmShake.HandleShakeRequest(reader, whoAmI);
                    break;
                case PalmShakeMessage.ShakeVisual:
                    PalmShake.HandleShakeVisual(reader);
                    break;
            }
        }
    }
}

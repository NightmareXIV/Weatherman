using Lumina.Data;

namespace Weatherman
{
    public class LvbFile : FileResource //from titleedit https://github.com/lmcintyre/TitleEditPlugin
    {
        public ushort[] weatherIds;

        public override void LoadFile()
        {
            weatherIds = new ushort[32];

            int pos = 0xC;
            if (Data[pos] != 'S' || Data[pos + 1] != 'C' || Data[pos + 2] != 'N' || Data[pos + 3] != '1')
                pos += 0x14;
            int sceneChunkStart = pos;
            pos += 0x10;
            int settingsStart = sceneChunkStart + 8 + BitConverter.ToInt32(Data, pos);
            pos = settingsStart + 0x40;
            int weatherTableStart = settingsStart + BitConverter.ToInt32(Data, pos);
            pos = weatherTableStart;
            for (int i = 0; i < 32; i++)
                weatherIds[i] = BitConverter.ToUInt16(Data, pos + i * 2);
        }
    }
}
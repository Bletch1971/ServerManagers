using System;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace ArkData
{
    internal partial class Parser
    {
        private static ulong GetId(byte[] data)
        {
            byte[] bytes1 = Encoding.Default.GetBytes("PlayerDataID");
            byte[] bytes2 = Encoding.Default.GetBytes("UInt64Property");
            int offset = Extensions.LocateFirst(data, bytes1, 0);
            int num = Extensions.LocateFirst(data, bytes2, offset);

            return BitConverter.ToUInt64(data, num + bytes2.Length + 9);
        }

        private static string GetPlatformId(byte[] data)
        {
            byte[] bytes1 = Encoding.Default.GetBytes("UniqueNetIdRepl");
            int num = Extensions.LocateFirst(data, bytes1, 0);

            byte[] bytes2 = new byte[9];
            Array.Copy(data, num + bytes1.Length, bytes2, 0, 9);

            var length = BitConverter.ToUInt32(bytes2, 5) - 1;
            byte[] bytes3 = new byte[length];

            Array.Copy(data, num + bytes1.Length + bytes2.Length, bytes3, 0, length);
            return Encoding.Default.GetString(bytes3);
        }

        public static PlayerData ParsePlayer(string fileName)
        {
            FileInfo fileInfo = new FileInfo(fileName);
            if (!fileInfo.Exists)
                return null;
            byte[] data = File.ReadAllBytes(fileName);

            var tribeId = Helpers.GetInt(data, "TribeId");

            return new PlayerData()
            {
                //PlayerId = GetPlatformId(data),
                PlayerId = Path.GetFileNameWithoutExtension(fileInfo.Name),
                PlayerName = Helpers.GetString(data, "PlayerName"),
                CharacterId = Convert.ToInt64(GetId(data)),
                CharacterName = Helpers.GetString(data, "PlayerCharacterName"),
                TribeId = tribeId > -1 ? tribeId : Helpers.GetInt(data, "TribeID"),
                Level = (short)(1 + Convert.ToInt32(Helpers.GetUInt16(data, "CharacterStatusComponent_ExtraCharacterLevel"))),

                File = fileName,
                Filename = fileInfo.Name,
                FileCreated = fileInfo.CreationTime,
                FileUpdated = fileInfo.LastWriteTime
            };
        }

        public static Task<PlayerData> ParsePlayerAsync(string fileName)
        {
            return Task.Run(() => ParsePlayer(fileName)); 
        }
    }
}

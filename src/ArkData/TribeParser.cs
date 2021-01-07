using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace ArkData
{
    internal partial class Parser
    {
        private static uint GetOwnerId(byte[] data)
        {
            byte[] bytes1 = Encoding.Default.GetBytes("OwnerPlayerDataID");
            byte[] bytes2 = Encoding.Default.GetBytes("UInt32Property");
            int offset = data.LocateFirst(bytes1, 0);
            int num = data.LocateFirst(bytes2, offset);
            return BitConverter.ToUInt32(data, num + bytes2.Length + 9);
        }

        public static TribeData ParseTribe(string fileName)
        {
            FileInfo fileInfo = new FileInfo(fileName);
            if (!fileInfo.Exists)
                return null;

            byte[] data = File.ReadAllBytes(fileName);

            var tribeId = Helpers.GetInt(data, "TribeId");

            return new TribeData()
            {
                Id = tribeId > -1 ? tribeId : Helpers.GetInt(data, "TribeID"),
                Name = Helpers.GetString(data, "TribeName"),
                OwnerId = (int?)GetOwnerId(data),

                File = fileName,
                Filename = fileInfo.Name,
                FileCreated = fileInfo.CreationTime,
                FileUpdated = fileInfo.LastWriteTime
            };
        }

        public static Task<TribeData> ParseTribeAsync(string fileName)
        {
            return Task.Run<TribeData>(() =>
            {
                return ParseTribe(fileName);
            });
        }
    }
}

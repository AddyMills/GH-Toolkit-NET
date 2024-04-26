using GH_Toolkit_Core.QB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static GH_Toolkit_Core.QB.QB;

namespace GH_Toolkit_Core.PS360
{
    public class FileCreation
    {
        public static void AddToSonglistGh3(List<QBItem> qbItems, QBStruct.QBStructData songlistData)
        {
            string checksum = (string)songlistData["checksum"];
            foreach (QBItem item in qbItems)
            {
                if (item.Name.ToLower() == "gh3_download_songs")
                {
                    var itemData = item.Data as QBStruct.QBStructData;
                    var tier1 = itemData["tier1"] as QBStruct.QBStructData;
                    var songs = tier1["songs"] as QBArray.QBArrayNode;
                    songs.AddQbkeyToArray(checksum);
                }
                else if (item.Name.ToLower() == "download_songlist")
                {
                    var itemData = item.Data as QBArray.QBArrayNode;
                    itemData.AddQbkeyToArray(checksum);
                }
                else if (item.Name.ToLower() == "download_songlist_props")
                {
                    var itemData = item.Data as QBStruct.QBStructData;
                    itemData.AddStructToStruct(checksum, songlistData);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Remoting.Metadata.W3cXsd2001;

namespace AssemblyCSharp.TOOL;

public class Boss
{
    public string NameBoss;

    public string MapName;

    public int zoneId = -1;

    public int MapId;

    public DateTime AppearTime;

    public static readonly List<string> strBossHasBeenKilled = new List<string> { " mọi người đều ngưỡng mộ.", " everyone admired.", " semua orang mengagumi.", " đã đánh bại và nhận được cải trang thành ", " killed and receive disguise of ", " membunuh Dan menerima disguise ", ": Đã tiêu diệt được ", ": defeated ", ": mengalahkan " };

    public static readonly List<string> strBossAppeared = new List<string> { "BOSS ", " vừa xuất hiện tại ", " appear at ", " muncul di ", " khu vực ", " zone ", " zona " };

    public Boss()
    {
    }

    public Boss(string chatVip)
    {
        chatVip = chatVip.Replace("BOSS ", "").Replace(" vừa xuất hiện tại ", "|").Replace(" appear at ", "|");
        string[] array = chatVip.Split('|');
        NameBoss = array[0].Trim();
        MapName = array[1].Trim();
        MapId = GetMapIdByMapName(NameBoss, MapName);
    }

    private int GetMapIdByMapName(string name, string map)
    {
        if (map == "Trạm tàu vũ trụ" && name.StartsWith("Tiểu đội") )
        {
            return 25;
        }else if (map == "Vách núi Moori")
        {
            return 43;
        }
        return GetMapID(map);
    }

    private static int GetMapID(string mapName)
    {
        

        for (int i = 0; i < TileMap.mapNames.Length; i++)
        {
            if (TileMap.mapNames[i].Equals(mapName))
            {
                return i;
            }
        }
        return -1;
    }

    public void Paint(mGraphics g, int x, int y, int align)
    {
        TimeSpan timeSpan = DateTime.Now.Subtract(AppearTime);
        int num = (int)timeSpan.TotalSeconds;
        mFont mFont = mFont.tahoma_7_yellow;
        if (TileMap.mapID == MapId)
        {
            mFont = mFont.tahoma_7_red;
            for (int i = 0; i < GameScr.vCharInMap.size(); i++)
            {
                if (((Char)GameScr.vCharInMap.elementAt(i)).cName.Equals(NameBoss))
                {
                    mFont = mFont.tahoma_7b_red;
                    break;
                }
            }
        }
        mFont.drawString(g, NameBoss + " - " + MapName + " - " + ((num < 60) ? (num + "s") : (timeSpan.Minutes + "ph")) + " trước", x, y, align);
    }
}

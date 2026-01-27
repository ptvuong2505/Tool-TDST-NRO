using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssemblyCSharp.Xmap
{
    public class XmapUtils
    {
        public static System.Random random = new System.Random();

        public static void moveToStartMap()
        {
            Char mychar = Char.myCharz();
            var x = TileMap.tmw + 5;
            var y = TileMap.tmh * 2;
            Service.gI().charMove();
            mychar.cx = x;
            mychar.cy = y;
            Service.gI().charMove();
            mychar.cx = x;
            mychar.cy = y + 1;
            Service.gI().charMove();
            mychar.cx = x;
            mychar.cy = y;
            Service.gI().charMove();
        }

        public static int getX(sbyte type)
        {
            for (int i = 0; i < TileMap.vGo.size(); i++)
            {
                Waypoint waypoint = (Waypoint)TileMap.vGo.elementAt(i);
                if (waypoint.maxX < 60 && type == 0)
                {
                    return 15;
                }
                if (waypoint.minX > TileMap.pxw - 60 && type == 2)
                {
                    return TileMap.pxw - 15;
                }
            }
            return 0;
        }

        public static int getY(sbyte type)
        {
            for (int i = 0; i < TileMap.vGo.size(); i++)
            {
                Waypoint waypoint = (Waypoint)TileMap.vGo.elementAt(i);
                if (waypoint.maxX < 60 && type == 0)
                {
                    return waypoint.maxY;
                }
                if (waypoint.minX > TileMap.pxw - 60 && type == 2)
                {
                    return waypoint.maxY;
                }
            }
            return 0;
        }

        public static Waypoint findWaypoint(int idMap)
        {
            Waypoint waypoint;
            string textPopup;
            for (int i = 0; i < TileMap.vGo.size(); i++)
            {
                waypoint = (Waypoint)TileMap.vGo.elementAt(i);
                textPopup = getTextPopup(waypoint.popup);
                if (textPopup.Equals(TileMap.mapNames[idMap]))
                {
                    return waypoint;
                }
            }
            return null;
        }
        public static string getTextPopup(PopUp popUp)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < popUp.says.Length; i++)
            {
                stringBuilder.Append(popUp.says[i]);
                stringBuilder.Append(" ");
            }
            return stringBuilder.ToString().Trim();
        }
    }
}
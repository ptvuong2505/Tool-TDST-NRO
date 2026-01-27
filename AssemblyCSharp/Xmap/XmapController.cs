using System.Collections.Generic;
using System.IO;

namespace AssemblyCSharp.Xmap
{
    public class XmapController : IActionListener
    {
        private const int TIME_DELAY_NEXTMAP = 350;
        private const int TIME_DELAY_RENEXTMAP = 750;
        private const int ID_ITEM_CAPSULE_VIP = 194;
        private const int ID_ITEM_CAPSULE = 193;
        private const int ID_ICON_ITEM_TDLT = 4387;

        private static readonly XmapController _Instance = new XmapController();

        private static int IdMapEnd;
        private static List<int> WayXmap;
        private static int IndexWay;
        private static bool IsNextMapFailed;
        private static bool IsWait;
        private static long TimeStartWait;
        private static long TimeWait;
        private static bool IsWaitNextMap;

        public static void Update()
        {
            if (IsWaiting())
                return;

            if (XmapData.Instance().IsLoading)
                return;

            if (IsWaitNextMap)
            {
                Wait(TIME_DELAY_NEXTMAP);
                IsWaitNextMap = false;
                return;
            }

            if (IsNextMapFailed)
            {
                XmapData.Instance().MyLinkMaps = null;
                WayXmap = null;
                IsNextMapFailed = false;
                return;
            }

            if (WayXmap == null)
            {
                GameScr.info1.addInfo("Đi đến: " + TileMap.mapNames[IdMapEnd], 0);
                if (XmapData.Instance().MyLinkMaps == null)
                {
                    XmapData.Instance().LoadLinkMaps();
                    return;
                }
                WayXmap = XmapAlgorithm.FindWay(TileMap.mapID, IdMapEnd);
                IndexWay = 0;
                if (WayXmap == null)
                {
                    GameScr.info1.addInfo("Không thể tìm thấy đường đi", 0);
                    FinishXmap();
                    return;
                }
            }

            if (TileMap.mapID == WayXmap[WayXmap.Count - 1] && !XmapData.IsMyCharDie())
            {
                FinishXmap();
                return;
            }

            if (TileMap.mapID == WayXmap[IndexWay])
            {
                if (XmapData.IsMyCharDie())
                {
                    Service.gI().returnTownFromDead();
                    IsWaitNextMap = IsNextMapFailed = true;
                }
                else if (XmapData.CanNextMap())
                {
                    NextMap(WayXmap[IndexWay + 1]);
                    IsWaitNextMap = true;
                }
                Wait(TIME_DELAY_RENEXTMAP);
                return;
            }

            if (TileMap.mapID == WayXmap[IndexWay + 1])
            {
                IndexWay++;
                return;
            }

            IsNextMapFailed = true;
        }

        public void perform(int idAction, object p)
        {
            switch (idAction)
            {
                case 1:
                    List<int> idMaps = (List<int>)p;
                    ShowPanelXmap(idMaps);
                    break;
            }
            Char.chatPopup = null;
        }

        private static void Wait(int time)
        {
            IsWait = true;
            TimeStartWait = mSystem.currentTimeMillis();
            TimeWait = time;
        }

        private static bool IsWaiting()
        {
            if (IsWait && mSystem.currentTimeMillis() - TimeStartWait >= TimeWait)
                IsWait = false;
            return IsWait;
        }

        #region Thao tác của xmap
        public static void ShowXmapMenu()
        {
            XmapData.Instance().LoadGroupMapsFromFile("TextData\\GroupMapsXmap.txt");
            MyVector myVector = new MyVector();
            foreach (var groupMap in XmapData.Instance().GroupMaps)
                myVector.addElement(new Command(groupMap.NameGroup, _Instance, 1, groupMap.IdMaps));
            GameCanvas.menu.startAt(myVector, 3);
            ChatPopup.addChatPopup($"Map hiện tại: {TileMap.mapName}, ID: {TileMap.mapID}\nVui lòng chọn nơi muốn đến", 100000, new Npc(5, 0, -100, 100, 5, 7333));
        }

        public static void ShowPanelXmap(List<int> idMaps)
        {
            Pk9rXmap.IsMapTransAsXmap = true;
            int len = idMaps.Count;
            GameCanvas.panel.mapNames = new string[len];
            GameCanvas.panel.planetNames = new string[len];
            for (int i = 0; i < len; i++)
            {
                string nameMap = TileMap.mapNames[idMaps[i]];
                GameCanvas.panel.mapNames[i] = idMaps[i] + ": " + nameMap;
                //GameCanvas.panel.planetNames[i] = "Xmap by Phucprotein";
            }
            GameCanvas.panel.setTypeMapTrans();
            GameCanvas.panel.show();
        }

        public static void StartRunToMapId(int idMap)
        {
            IdMapEnd = idMap;
            Pk9rXmap.IsXmapRunning = true;
        }

        public static void FinishXmap()
        {
            Pk9rXmap.IsXmapRunning = false;
            IsNextMapFailed = false;
            XmapData.Instance().MyLinkMaps = null;
            WayXmap = null;
        }

        public static void SaveIdMapCapsuleReturn()
        {
            Pk9rXmap.IdMapCapsuleReturn = TileMap.mapID;
        }

        private static void NextMap(int idMapNext)
        {
            List<MapNext> mapNexts = XmapData.Instance().GetMapNexts(TileMap.mapID);
            if (mapNexts != null)
            {
                foreach (MapNext mapNext in mapNexts)
                {
                    if (mapNext.MapID == idMapNext)
                    { 
                        NextMap(mapNext);
                        return;
                    }
                }
            }
            GameScr.info1.addInfo("Lỗi tại dữ liệu", 0);
        }

        private static void NextMap(MapNext mapNext)
        {
            switch (mapNext.Type)
            {
                case TypeMapNext.AutoWaypoint:
                    NextMapAutoWaypoint(mapNext);
                    break;
                case TypeMapNext.NpcMenu:
                    NextMapNpcMenu(mapNext);
                    break;
                case TypeMapNext.NpcPanel:
                    NextMapNpcPanel(mapNext);
                    break;
                case TypeMapNext.Position:
                    NextMapPosition(mapNext);
                    break;
                case TypeMapNext.Capsule:
                    NextMapCapsule(mapNext);
                    break;
            }
        }

        private static void NextMapAutoWaypoint(MapNext mapNext)
        {
            Waypoint waypoint = XmapData.FindWaypoint(mapNext.MapID);
            if (waypoint != null)
            {
                int x = XmapData.GetPosWaypointX(waypoint);
                int y = XmapData.GetPosWaypointY(waypoint);
                // fix tọa độ cho map 16 đi 26
                if (TileMap.mapID==16 && mapNext.MapID == 26)
                {
                    x -= 15;
                }
                MoveMyChar(x, y);
                RequestChangeMap(waypoint);
            }
        }

        public static void NextMapNpcMenu(MapNext mapNext)
        {
            var npcId = mapNext.Info[0];
            if (npcId == 38)
            {
                var flag = false;
                int vNpcSize = GameScr.vNpc.size();
                for (int i = 0; i < vNpcSize; i++)
                {
                    var npc = (Npc)GameScr.vNpc.elementAt(i);
                    if (npc.template.npcTemplateId == npcId)
                    {
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                {
                    // Optimization for maps 27, 28, 29 navigation
                    if (TileMap.mapID == 27)
                    {
                        // From map 27, first try map 28 (since it's the middle point)
                        NextMap(28);
                    }
                    else if (TileMap.mapID == 29)
                    {
                        // From map 29, first try map 28 (since it's the middle point)
                        NextMap(28);
                    }
                    else if (TileMap.mapID == 28)
                    {
                        // From map 28, try both 27 and 29 with priority
                        // First check which one we haven't visited recently
                        if (WayXmap != null && WayXmap.Count > 1 && IndexWay > 0)
                        {
                            // If we came from map 27, go to 29
                            if (WayXmap[IndexWay - 1] == 27)
                                NextMap(29);
                            // If we came from map 29, go to 27
                            else if (WayXmap[IndexWay - 1] == 29)
                                NextMap(27);
                            else
                            {
                                // If no history, use random selection
                                if (XmapUtils.random.Next(2) == 0)
                                    NextMap(27);
                                else
                                    NextMap(29);
                            }
                        }
                        else
                        {
                            // If no history, use random selection
                            if (XmapUtils.random.Next(2) == 0)
                                NextMap(27);
                            else
                                NextMap(29);
                        }
                    }
                    else
                    {
                        // From other maps, prioritize going to map 28 (middle point)
                        // as it gives access to both 27 and 29
                        if (mapNext.MapID == 28)
                        {
                            NextMap(28);
                        }
                        else if (mapNext.MapID == 27 || mapNext.MapID == 29)
                        {
                            NextMap(mapNext.MapID);
                        }
                        else
                        {
                            // If not directly targeting maps 27, 28, or 29,
                            // choose a random entry point between 27 and 29
                            if (XmapUtils.random.Next(2) == 0)
                                NextMap(27);
                            else
                                NextMap(29);
                        }
                    }
                }
            }
            Service.gI().openMenu(npcId);
            for (int i = 1; i < mapNext.Info.Length; i++)
            {
                int select = mapNext.Info[i];
                Service.gI().confirmMenu((short)npcId, (sbyte)select);
            }
        }

        private static void NextMapNpcPanel(MapNext mapNext)
        {
            int idNpc = mapNext.Info[0];
            int selectMenu = mapNext.Info[1];
            int selectPanel = mapNext.Info[2];
            Service.gI().openMenu(idNpc);
            Service.gI().confirmMenu((short)idNpc, (sbyte)selectMenu);
            Service.gI().requestMapSelect(selectPanel);
        }

        private static void NextMapPosition(MapNext mapNext)
        {
            int xPos = mapNext.Info[0];
            int yPos = mapNext.Info[1];
            MoveMyChar(xPos, yPos);
            Service.gI().requestChangeMap();
            Service.gI().getMapOffline();
        }

        private static void NextMapCapsule(MapNext mapNext)
        {
            SaveIdMapCapsuleReturn();
            int index = mapNext.Info[0];
            Service.gI().requestMapSelect(index);
        }
        #endregion

        #region Thao tác với game
        public static void UseCapsuleNormal()
        {
            Pk9rXmap.IsShowPanelMapTrans = false;
            Service.gI().useItem(0, 1, -1, ID_ITEM_CAPSULE);
        }

        public static void UseCapsuleVip()
        {
            Pk9rXmap.IsShowPanelMapTrans = false;
            Service.gI().useItem(0, 1, -1, ID_ITEM_CAPSULE_VIP);
        }
        
        public static void HideInfoDlg()
        {
            InfoDlg.hide();
        }

        public static void MoveMyChar(int x, int y)
        {
            Char.myCharz().cx = x;
            Char.myCharz().cy = y;
            Service.gI().charMove();

            if (ItemTime.isExistItem(ID_ICON_ITEM_TDLT))
                return;

            Char.myCharz().cx = x;
            Char.myCharz().cy = y + 1;
            Service.gI().charMove();
            Char.myCharz().cx = x;
            Char.myCharz().cy = y;
            Service.gI().charMove();
        }

        private static void RequestChangeMap(Waypoint waypoint)
        {
            if (waypoint.isOffline)
            {
                Service.gI().getMapOffline();
                return;
            }
            Service.gI().requestChangeMap();
        }
        #endregion
    }
}

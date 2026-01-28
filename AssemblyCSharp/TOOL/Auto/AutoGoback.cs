using AssemblyCSharp.TOOL;
using AssemblyCSharp.TOOL.ToolHelper;
using AssemblyCSharp.Xmap;
using System;
using TOOl;

namespace TOOl.Auto
{
    public class AutoGoback
    {
        // State variables
        public static BossHuntState currentState = BossHuntState.Idle;

        // Timing variables
        private static long lastTimeGoBack;
        private static long lastReturnTownTime;
        private static long stateStartTime;
        private static long lastZoneCheckTime;
        
        // Boss hunting variables
        private static int targetMapId = -1;
        private static int targetZoneId = -1;
        private static int currentCheckingZone = -1;
        private static int maxZone = 0;
        private static bool foundBossSent = false;
        
        // Configuration
        public static readonly int ID_MAP_WAITTING = 11;
        public static InfoGoBack goingBackTo = new InfoGoBack(ID_MAP_WAITTING, MainTool.myZone % 15, 200, 384);

        public enum BossHuntState
        {
            Idle,
            WaitingInZone,// Đang chờ boss tại khu chỉ định
            RunningToMapBoss,       // Đang chạy tới map boss
            WaitingXmapComplete, // Đợi xmap chạy xong
            FindingBossZone,        // Đang tìm zone có boss
            ChangingToZone,         // Đang đổi zone
            WaitingZoneChange,      // Đợi đổi zone hoàn tất
            WaitingBeforeCheck,// Đợi trước khi check boss
            GoingBack,       // Đang quay lại khu chờ
            GoingToBossZone         // Đang vào khu có boss
        }

        public static void Waiting()
        {
            changeState(BossHuntState.WaitingInZone);
        }

        private static void changeState(BossHuntState newState)
        {
            if (currentState != newState)
            {
                //GameScr.info1.addInfo($"State: {currentState} -> {newState}", 0);
                currentState = newState;
                stateStartTime = mSystem.currentTimeMillis();
            }
        }

        public static void update()
        {
            if (!Utilities.isFrameMultipleOf(10))
               return;

            // Check for boss in waiting zone và gửi thông báo ngay lập tức
            checkBossInWaitingZone();

            switch (currentState)
            {
                case BossHuntState.Idle:
                    break;

                case BossHuntState.WaitingInZone:
                        updateWaitingInZone();
                    break;

                case BossHuntState.RunningToMapBoss:
                        updateRunningToMapBoss();
                    break;

                case BossHuntState.WaitingXmapComplete:
                        updateWaitingXmapComplete();
                    break;

                case BossHuntState.FindingBossZone:
                        updateFindingBossZone();
                    break;

                case BossHuntState.ChangingToZone:
                        updateChangingToZone();
                    break;

                case BossHuntState.WaitingZoneChange:
                        updateWaitingZoneChange();
                    break;

                case BossHuntState.WaitingBeforeCheck:
                        updateWaitingBeforeCheck();
                    break;

                case BossHuntState.GoingBack:
                        updateGoingBack();
                    break;

                case BossHuntState.GoingToBossZone:
                        updateGoingToBossZone();
                    break;
            }

            // Handle death nếu không có boss trong zone
            if (Char.myCharz().meDead && !MainTool.IsBossInZone())
            {
                 handleDeath();
            }
        }

        // Check boss trong zone chờ và gửi thông báo ngay
        private static void checkBossInWaitingZone()
        {
            if (TileMap.mapID == ID_MAP_WAITTING && TileMap.zoneID == goingBackTo.zoneID &&
                !foundBossSent)
            {
                if (MainTool.IsBossInZone())
                {
                    foundBossSent = true;
                    MainTool.zoneBoss = TileMap.zoneID;
                    SocketClient.gI.sendMessage(new
                    {
                        action = "foundBoss",
                        zone = TileMap.zoneID,
                        mapId = TileMap.mapID
                    });
                    GameScr.info1.addInfo($"Boss xuất hiện tại zone chờ {TileMap.zoneID}", 0);
                }
            }
        }

        private static void updateWaitingInZone()
        {
            // Nếu không ở đúng map/zone chờ thì quay lại
            if ((TileMap.mapID != goingBackTo.mapID || TileMap.zoneID != goingBackTo.zoneID) && !Utilities.isHome())
            {
                changeState(BossHuntState.GoingBack);
                return;
            }

            // Nếu ở home thì goback lại sau 1s
            if (mSystem.currentTimeMillis() - lastReturnTownTime > 1000 && Utilities.isHome())
            {
                changeState(BossHuntState.GoingBack);
            }
        }

        private static void updateRunningToMapBoss()
        {
            // Đợi xmap bắt đầu
            if (Pk9rXmap.IsXmapRunning)
            {
                changeState(BossHuntState.WaitingXmapComplete);
            }
        }

        private static void updateWaitingXmapComplete()
        {
            // Đợi xmap chạy xong
            if (!Pk9rXmap.IsXmapRunning)
            {
                if (TileMap.mapID == targetMapId)
                {
                    Utilities.teleportMyChar(Char.myCharz().cx, Char.myCharz().cy - 10);
                    startFindingBoss();
                }
                else
                {
                    GameScr.info1.addInfo("Không chạy được tới map boss", 0);
                    changeState(BossHuntState.GoingBack);
                }
            }
        }

        private static void startFindingBoss()
        {
            Service.gI().openUIZone();
            maxZone = GameScr.gI().zones.Length;
            
            // Điều chỉnh myZone nếu vượt quá số zone
            if (maxZone == 15 && MainTool.myZone >= 15)
            {
                MainTool.myZone = 2;
            }
            
            currentCheckingZone = MainTool.myZone;
            changeState(BossHuntState.FindingBossZone);
        }

        private static void updateFindingBossZone()
        {
            // Check zone hiện tại
            if (currentCheckingZone >= maxZone)
            {
                GameScr.info1.addInfo("Không tìm thấy boss trong các zone", 0);
                //changeState(BossHuntState.GoingBack);
                changeState(BossHuntState.Idle);
                return;
            }

            // Bắt đầu đổi zone
            changeState(BossHuntState.ChangingToZone);
        }

        private static void updateChangingToZone()
        {
            if (Utilities.isHome())
            {
                changeState(BossHuntState.GoingBack);
                return;
            }

            // Gửi request đổi zone
            if (GameScr.gI().numPlayer[currentCheckingZone] < GameScr.gI().maxPlayer[currentCheckingZone])
            {
                GameScr.info1.addInfo($"Đang chuyển sang zone {currentCheckingZone}", 0);
                Service.gI().requestChangeZone(currentCheckingZone, -1);
                lastZoneCheckTime = mSystem.currentTimeMillis();
                changeState(BossHuntState.WaitingZoneChange);
            }
        }

        private static void updateWaitingZoneChange()
        {
            long elapsed = mSystem.currentTimeMillis() - lastZoneCheckTime;

            // Check xem đã đổi zone thành công chưa
            if (TileMap.zoneID == currentCheckingZone)
            {
                SocketClient.gI.sendMessage(new
                {
                    action = "setStatus",
                    status = "InZone",
                    message = $"account {MainTool.id} changed to {currentCheckingZone}"
                });
                changeState(BossHuntState.WaitingBeforeCheck);
                return;
            }

            if (elapsed > 500 && GameCanvas.gameTick % 20 == 0)
            {
                // Retry đổi zone nếu quá thời gian
                changeState(BossHuntState.ChangingToZone);
            }
        }

        private static void updateWaitingBeforeCheck()
        {
            long elapsed = mSystem.currentTimeMillis() - stateStartTime;
    
            // Đợi 2 giây để đảm bảo tất cả client đã vào map
            if (elapsed > 2000)
            {
                if (MainTool.IsBossInZone())
                {
                    GameScr.info1.addInfo($"Tìm thấy boss tại zone {TileMap.zoneID}", 0);
                    MainTool.zoneBoss = TileMap.zoneID;
                    foundBossSent = true;
                    SocketClient.gI.sendMessage(new
                    {
                        action = "foundBoss",
                        zone = TileMap.zoneID,
                        mapId = TileMap.mapID
                    });
                    changeState(BossHuntState.Idle);
                }
                else
                {
                    // Không có boss, check zone tiếp theo
                    currentCheckingZone += MainTool.jump;
                    changeState(BossHuntState.FindingBossZone);
                }
            }
        }

        private static void updateGoingBack()
        {
            if (TileMap.mapID != goingBackTo.mapID)
            {
                // Chạy về map chờ
                if (!Pk9rXmap.IsXmapRunning)
                {
                    XmapController.StartRunToMapId(goingBackTo.mapID);
                }
            }
            else if (TileMap.mapID == goingBackTo.mapID)
            {
                if (TileMap.zoneID != goingBackTo.zoneID)
                {
                    // Đổi về zone chờ
                    Service.gI().requestChangeZone(goingBackTo.zoneID, -1);
                }
                else
                {
                    // Đã về đúng map và zone, teleport
                    Utilities.teleportMyChar(Char.myCharz().cx, Char.myCharz().cy-10);
                    changeState(BossHuntState.WaitingInZone);
                }
            }
        }

        private static void updateGoingToBossZone()
        {
            if (TileMap.mapID != targetMapId)
            {
                // Chạy về map boss
                if (!Pk9rXmap.IsXmapRunning)
                {
                    XmapController.StartRunToMapId(targetMapId);
                }
            }
            else if (TileMap.zoneID != targetZoneId)
            {
                // Đã đến map boss, đổi vào zone boss
                long elapsed = mSystem.currentTimeMillis() - lastZoneCheckTime;
         
                if (elapsed > 500)
                {
                    
                    lastZoneCheckTime = mSystem.currentTimeMillis();
                    if(GameScr.gI().numPlayer[targetZoneId] < GameScr.gI().maxPlayer[targetZoneId])
                    {
                        Service.gI().requestChangeZone(targetZoneId, -1);
                    }
                }
            }
            else
            {
                // Đã vào đúng map và zone boss
                SocketClient.gI.sendMessage(new
                {
                    action = "setStatus",
                    status = "InZone",
                    message = $"{MainTool.username} changed to {TileMap.zoneID}"
                });
                Utilities.teleportMyChar(Char.myCharz().cx, Char.myCharz().cy - 10);
                changeState(BossHuntState.Idle);
            }
    }

        private static void handleDeath()
        {
             long now = mSystem.currentTimeMillis();
             long timeSinceDeath = now - lastTimeGoBack;

              if (timeSinceDeath > 4000)
              {
                 lastTimeGoBack = now;
                 return;
              }

              if (timeSinceDeath > 2000)
              {
                lastReturnTownTime = now;
                Service.gI().returnTownFromDead();
              }
        }

        // Public methods để trigger state changes từ bên ngoài
        public static void startRunToMapBoss(int mapId)
        {
            // Boss ra tại map chờ
            if (mapId == ID_MAP_WAITTING)
            {
                if (MainTool.IsBossInZone() && TileMap.mapID == ID_MAP_WAITTING)
                {
                     foundBossSent = true;
                     MainTool.zoneBoss = TileMap.zoneID;
                     SocketClient.gI.sendMessage(new
                     {
                        action = "foundBoss",
                        zone = TileMap.zoneID,
                        mapId = TileMap.mapID
                     });
                }
                return;
            }

            targetMapId = mapId;
            foundBossSent = false;
            XmapController.FinishXmap();
            XmapController.StartRunToMapId(mapId);
            
            SocketClient.gI.sendMessage(new
            {
                action = "setStatus",
                status = "Running to map boss"
            });

            changeState(BossHuntState.RunningToMapBoss);
        }

        public static void goToBossZone()
        {
            // Sử dụng mapBoss và zoneBoss đã được set từ message
            targetZoneId = MainTool.zoneBoss;
            targetMapId = MainTool.mapBoss; 
            foundBossSent = true;
   
            // Nếu chưa ở đúng map hoặc zone
            if (TileMap.mapID != targetMapId || TileMap.zoneID != targetZoneId)
            {
                changeState(BossHuntState.GoingToBossZone);
            }
        }

        public static void resetToWaitingZone()
        {
            foundBossSent = false;
            targetMapId = -1;
            targetZoneId = -1;
            MainTool.zoneBoss = -1;
            MainTool.mapBoss = -1;
            changeState(BossHuntState.GoingBack);
        }

        public struct InfoGoBack
        {
            public int mapID;
            public int zoneID;
            public int x;
            public int y;

            public InfoGoBack(int mapId, int zoneId, int x, int y)
            {
                mapID = mapId;
                zoneID = zoneId;
                   this.x = x;
                    this.y = TileMap.tileTypeAt(x, y, 2) ? y : Utilities.getYGround(x);
              }
    
            public InfoGoBack(int mapId, int zoneId, IMapObject mapObject)
            {
                mapID = mapId;
                zoneID = zoneId;
                x = mapObject.getX();
                y = TileMap.tileTypeAt(x, mapObject.getY(), 2) ? mapObject.getY() : Utilities.getYGround(x);
            }
        }
    }
}

using AssemblyCSharp.TOOL.Auto;
using AssemblyCSharp.TOOL.ToolHelper;
using AssemblyCSharp.Xmap;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using TOOl;
using TOOl.Auto;
using UnityEngine;
using static TOOl.Auto.AutoGoback;

namespace AssemblyCSharp.TOOL;

public class MainTool
{
    public static int id;
    public static int myZone;
    public static bool isUseProxy;
    public static string proxyString = "";
    public static string username;
    public static string password;
    public static bool isPick = true;
    public static int server;
    public static int jump = 1;

    public static bool isFindBoss;
    public static int mapBoss = -1;
    public static int zoneBoss = -1;
    public static long lastTimeFocusBoss = mSystem.currentTimeMillis();
    public static bool isUseTDLT = false;


    public static bool quitGame = false;
    public static bool isDownloading;


    public static readonly int ID_ITEM_CN = 381;
    public static readonly int ID_ITEM_BH = 382;
    public static readonly int ID_ITEM_TDLT = 521;
    public static readonly List<int> ID_DO_AN = new List<int> { 663, 664, 665, 666, 667 };

    public static MainTool _instance;
    public static MainTool Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new MainTool();
            }
            return _instance;
        }
    }

    public static void ClearMap()
    {
        if (GameScr.vMob.size() > 0)
        {
            GameScr.vMob.removeAllElements();
        }
        if (GameScr.vCharInMap.size() <= 0)
        {
            return;
        }
        for (int i = GameScr.vCharInMap.size() - 1; i >= 0; i--)
        {
            Char ch = (Char)GameScr.vCharInMap.elementAt(i);
            if (ch != null && !ch.me && !ch.cName.Contains("Số") && !ch.cName.Contains("Tiểu"))
            {
                GameScr.vCharInMap.removeElementAt(i);
            }
        }
    }

    public static void Update()
    {
        //GameScr.info1.addInfo((Char.myCharz().charID == Char.myCharz().itemFocus?.playerId) + $", id: {Char.myCharz().itemFocus?.template.id}, count: {Char.myCharz().itemFocus?.countAutoPick} " ,0);
        MainThreadDispatcher.update();
        AutoGoback.update();

        if (GameCanvas.gameTick % 10 == 0)
        {
            ClearMap();
        }
        if(Char.myCharz().meDead)
        {
            return;
        }

        if(GameCanvas.gameTick % 10 == 0 && !Utilities.isHome())
            Service.gI().openUIZone();

        AutoPick.Update();
        AutoSkill.Update();
        AutoFocussBoss();
        AutoTeleBoss.Update();
       
        if ((Char.myCharz().cHP <= Char.myCharz().cHPFull * 20 / 100 || (Char.myCharz().cMP <= Char.myCharz().cMPFull * 20 / 100)) && GameCanvas.gameTick % 20 == 0)
        {
            Utilities.doUsePean();
        }
        if (!AutoSkill.isAutoSendAttack && GameCanvas.gameTick % 50 == 0)
        {
            if (Char.myCharz().cgender == 0)
            {
                Utilities.Tdhs();
            }
            else if (Char.myCharz().cgender == 1 && Char.myCharz().cMP >= Char.myCharz().cMPFull * (long)20 / 100)
            {
                Utilities.buffMe();
            }
            else if (Char.myCharz().cgender == 2)
            {
                Utilities.Ttnl();
            }
            if (!Utilities.isUsingTDLT())
            {
                if (!isUseTDLT)
                {
                    isUseTDLT = true;
                    AutoBatTDLT();
                }
            }
        }
        //if (TileMap.zoneID == zoneBoss && TileMap.mapID == mapBoss && !IsBossInZone() && !quitGame)
        //{
        //    quitGame = true;
        //    Thread thread = new Thread((ThreadStart)delegate
        //    {
        //        try
        //        {
        //            Thread.Sleep(8000); // đợi 8 giây
        //            // chỉ gửi khi vẫn ở cùng zone boss và boss vẫn không có
        //            if (TileMap.zoneID == zoneBoss && TileMap.mapID == mapBoss && !IsBossInZone())
        //            {
        //                ThreadAction<SocketClient>.gI.sendMessage(new
        //                {
        //                    action = "done"
        //                });
        //            }
        //        }
        //        catch (Exception)
        //        {
        //            // ignore
        //        }
        //        finally
        //        {
        //            quitGame = false;
        //        }
        //    });
        //    thread.IsBackground = true;
        //    thread.Start();
        //}
        if (TileMap.mapID == mapBoss && TileMap.zoneID == zoneBoss && !IsBossInZone() && !quitGame)
        {
            timeCheckBoss += Time.unscaledDeltaTime;

            if(timeCheckBoss>=8f && !quitGame)
            {
                quitGame = true; 
                SocketClient.gI.sendMessage(new
                {
                    action = "done"
                });
            }
        }
        else
        {
            timeCheckBoss = 0f;
        }
    }

    public static float timeCheckBoss;

    public static void RuntoMap(int idMap)
    {
        Thread thread = new Thread((ThreadStart)delegate
        {
            XmapController.FinishXmap();
            XmapController.StartRunToMapId(idMap);
            ThreadAction<SocketClient>.gI.sendMessage(new
            {
                action = "setStatus",
                status = "Running"
            });
            while (Pk9rXmap.IsXmapRunning)
            {
                Thread.Sleep(100);
            }
            ThreadAction<SocketClient>.gI.sendMessage(new
            {
                action = "setStatus",
                status = $"Standing in map id: {TileMap.mapID}"
            });
        });
        thread.IsBackground = true;
        thread.Start();
    }

    public static void OffTDLT()
    {
        if (!GameScr.canAutoPlay)
        {
            return;
        }
        for (sbyte indexIem = 0; indexIem < Char.myCharz().arrItemBag.Length; indexIem++)
        {
            Item item = Char.myCharz().arrItemBag[indexIem];
            if (item != null && item.template.id == ID_ITEM_TDLT)
            {
                Service.gI().useItem(0, 1, indexIem, -1);
                break;
            }
        }
    }

    public static void AutoFocussBoss()
    {
        if (Char.myCharz().itemFocus != null || mSystem.currentTimeMillis() < lastTimeFocusBoss || Char.myCharz().meDead)
        {
            return;
        }
        for (int i = 0; i < GameScr.vCharInMap.size(); i++)
        {
            Char ch = (Char)GameScr.vCharInMap.elementAt(i);
            if (ch!= null && ch.cTypePk == 5 && (ch.cName.Contains("Số") || ch.cName.Contains("Tiểu")) && ch.cHP>0 && Char.myCharz().isMeCanAttackOtherPlayer(ch))
            {
                Char.myCharz().npcFocus = null;
                Char.myCharz().charFocus = ch;
                Char.myCharz().mobFocus = null;
                break;
            }
        }
        lastTimeFocusBoss = mSystem.currentTimeMillis() + 500;
    }

    public static void onGameStarting()
    {
        LoadDataLogin();
        SocketClient.gI.initSender();
    }

    public static void GetInfor(string chatVip)
    {
        if (chatVip.Contains("Núi khỉ đen") || chatVip.Contains("Hang khỉ đen") || chatVip.Contains("Núi khỉ đỏ") || !chatVip.Contains("Tiểu đội trưởng"))
        {
            return;
        }
        try
        {
            //Chỉ thực hiện khi đang ở waiting, hoặc idle và không có boss trong map
            if(AutoGoback.currentState == BossHuntState.WaitingInZone || (AutoGoback.currentState == BossHuntState.Idle && !IsBossInZone()))
            {
                Boss boss = new Boss(chatVip);
                zoneBoss = -1;
                mapBoss = boss.MapId;
                AutoGoback.startRunToMapBoss(boss.MapId);
            }
        }
        catch (Exception ex)
        {
            GameScr.info1.addInfo("Error processing boss info: " + ex.Message, 0);
        }
    }

    public static void ChangeToZoneBoss(int zone)
    {
        AutoGoback.goToBossZone(zone);
    }

    public static void AutoUseItemBuffSD()
    {
        AutoUseCN();
        AuToUseDoAn();
        AutoHopThe();
        AutoBatTDLT();
    }

    private static void AutoBatTDLT()
    {
        if (!GameScr.canAutoPlay)
        {
            UseItem(ID_ITEM_TDLT);
        }
    }

    public static void AutoHopThe()
    {
        Thread thread = new Thread((ThreadStart)delegate
        {
            int num = -1;
            for (int i = 0; i < Char.myCharz().arrItemBag.Length; i++)
            {
                if (Char.myCharz().arrItemBag[i].template.id == 921)
                {
                    num = 921;
                    break;
                }
                if (Char.myCharz().arrItemBag[i].template.id == 454)
                {
                    num = 454;
                    break;
                }
            }
            if (num != -1)
            {
                while (!Char.myCharz().isNhapThe)
                {
                    UseItem(num);
                    Thread.Sleep(1000);
                }
            }
        });
        thread.IsBackground = true;
        thread.Start();
    }

    public static void AutoUseCN()
    {
        for (int i = 0; i < Char.myCharz().arrItemBag.Length; i++)
        {
            Item item = Char.myCharz().arrItemBag[i];
            if (item != null && item.template.id == ID_ITEM_CN)
            {
                Service.gI().useItem(0, 1, (sbyte)i, -1);
                break;
            }
        }
    }

    public static void AuToUseDoAn()
    {
        for (int i = 0; i < Char.myCharz().arrItemBag.Length; i++)
        {
            Item item = Char.myCharz().arrItemBag[i];
            if (item != null && ID_DO_AN.Contains(item.template.id))
            {
                Service.gI().useItem(0, 1, (sbyte)i, -1);
                break;
            }
        }
    }

    public static bool IsBossInZone()
    {
        for (int i = 0; i < GameScr.vCharInMap.size(); i++)
        {
            Char ch = (Char)GameScr.vCharInMap.elementAt(i);
            if (ch.cName.Contains("Số") || ch.cName.Contains("Tiểu"))
            {
                return true;
            }
        }
        return false;
    }

    public static void Init()
    {
        UpdateState.gI.toggle((bool?)true);
    }

    public static void LoadDataLogin()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 1; i < args.Length - 1; i++)
        {
            if (args[i] == "--id")
            {
                id = int.Parse(args[i + 1]);
                myZone = id + 2;
            }
            else if (args[i] == "--username")
            {
                username = args[i + 1];
            }
            else if (args[i] == "--password")
            {
                password = args[i + 1];
            }
            else if (args[i] == "--server")
            {
                server = int.Parse(args[i + 1]);
            }
            else if (args[i] == "--isPick")
            {
                isPick = bool.Parse(args[i + 1]);
            }
            else if (args[i] == "--proxyString")
            {
                proxyString = args[i + 1];
                isUseProxy = true;
            }
            else if (args[i] == "--jump")
            {
                jump = int.Parse(args[i + 1]);
            }
        }
        Thread thread = new Thread(Login);
        thread.IsBackground = true;
        thread.Start();
    }

    public static void AutoLogin()
    {
        Thread.Sleep(32000);
        LoadDataLogin();
        Thread.Sleep(500);
        Login();
    }

    public static void Login()
    {
        while (isDownloading)
        {
            Thread.Sleep(100);
        }
        while (!ServerListScreen.loadScreen)
        {
            Thread.Sleep(10);
        }
        Thread.Sleep(500);
        Rms.saveRMSString("acc", username);
        Rms.saveRMSString("pass", password);
        if (ServerListScreen.ipSelect != server)
        {
            Rms.saveRMSInt("svSelect", server);
            ServerListScreen.ipSelect = server;
            GameCanvas.serverScreen.selectServer();
            while (!ServerListScreen.loadScreen)
            {
                Thread.Sleep(10);
            }
            while (!Session_ME.gI().isConnected())
            {
                Thread.Sleep(100);
            }
            Thread.Sleep(100);
            while (!ServerListScreen.loadScreen)
            {
                Thread.Sleep(10);
            }
        }
        Thread.Sleep(1000);
        if (GameCanvas.loginScr == null)
        {
            GameCanvas.loginScr = new LoginScr();
        }
        GameCanvas.loginScr.switchToMe();
        //GameCanvas.serverScreen.perform(3, null);
        GameCanvas.loginScr.doLogin();

        // Exit when login fails
        Thread.Sleep(10000);
        if (string.IsNullOrEmpty(Char.myCharz()?.cName))
        {
            Main.exit();
        }
    }

    public static bool PressKey(int _)
    {
        if (GameCanvas.keyAsciiPress == Hotkeys.X)
        {
            Pk9rXmap.Chat("xmp");
            return true;
        }
        return false;
    }

    public static void ThaoGiapLuyenTap()
    {
        if (Char.myCharz().arrItemBody[6] != null)
        {
            Service.gI().getItem(5, 6);
        }
    }

    public static void UseTDLT()
    {
        if (GameScr.canAutoPlay)
        {
            return;
        }
        for (sbyte indexIem = 0; indexIem < Char.myCharz().arrItemBag.Length; indexIem++)
        {
            Item item = Char.myCharz().arrItemBag[indexIem];
            if (item != null && item.template.id == ID_ITEM_TDLT)
            {
                Service.gI().useItem(0, 1, indexIem, -1);
                break;
            }
        }
    }

    public static void UseItem(int id)
    {
        for (sbyte indexIem = 0; indexIem < Char.myCharz().arrItemBag.Length; indexIem++)
        {
            Item item = Char.myCharz().arrItemBag[indexIem];
            if (item != null && item.template.id == id)
            {
                Service.gI().useItem(0, 1, indexIem, -1);
                break;
            }
        }
    }

    public static void Paint(mGraphics g)
    {
        mFont.tahoma_7.drawString(g, "Map: " + TileMap.mapNames[TileMap.mapID] + " [" + TileMap.zoneID + "]", 25, GameCanvas.h - 120, 0);
        int num = GameCanvas.h - 110;
        if (isPick)
        {
            mFont.tahoma_7.drawString(g, "Pick: on", 25, num, 0);
            num += 10;
        }
        if (AutoSkill.isAutoSendAttack)
        {
            mFont.tahoma_7.drawString(g, "Tự đánh: on", 25, num, 0);
            num += 10;
        }
    }

    public static void onGameClosing()
    {
        SocketClient.gI.close();
    }


    public static void onDownloadScreen()
    {
        isDownloading = true;
        GameCanvas.serverScreen.perform(2, null);
    }

    public static void TeleportToFocus()
    {
        if (Char.myCharz().charFocus != null)
        {
            TeleportTo(Char.myCharz().charFocus.cx, Char.myCharz().charFocus.cy);
        }
    }

    public static void TeleportTo(int x, int y)
    {
        Char.myCharz().cx = x;
        Char.myCharz().cy = y;
        Service.gI().charMove();
        Char.myCharz().cx = x;
        Char.myCharz().cy = y + 1;
        Service.gI().charMove();
        Char.myCharz().cx = x;
        Char.myCharz().cy = y;
        Service.gI().charMove();
    }

    private static Skill GetSkillById(int id)
    {
        for (int i = 0; i < GameScr.keySkill.Length; i++)
        {
            Skill skill = GameScr.keySkill.ElementAt(i);
            if (skill.template.id == id)
            {
                return skill;
            }
        }
        return null;
    }

    private static int GetSkillIndexById(int idSkill)
    {
        for (int i = 0; i < GameScr.keySkill.Length; i++)
        {
            if (GameScr.keySkill.ElementAt(i).template.id == idSkill)
            {
                return i;
            }
        }
        return 0;
    }

    public static void WriteDate()
    {
        WriteHongNgoc();
    }

    public static void WriteHongNgoc()
    {
        if (string.IsNullOrEmpty(username))
        {
            return;
        }
        string folderName = ((!(DateTime.Now > DateTime.Today.AddHours(5.0))) ? DateTime.Now.AddDays(-1.0).ToString("dd-MM-yyyy") : DateTime.Now.ToString("dd-MM-yyyy"));
        string folderPath = "HongNgoc/" + folderName;
        if (!Directory.Exists(folderPath))
        {
            return;
        }
        string filePath = Path.Combine(folderPath, username + ".txt");
        int hongNgoc = Utilities.GetHongNgoc();
        try
        {
            if (!File.Exists(filePath) || new FileInfo(filePath).Length == 0L)
            {
                File.WriteAllText(filePath, hongNgoc.ToString());
            }
            else
            {
                File.AppendAllText(filePath, $",{hongNgoc}");
            }
        }
        catch (Exception)
        {
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace TOOl;

public class Utilities
{
    public const string ManifestModuleName = "Assembly-CSharp.dll";

    public const string PathChatCommand = "ModData\\chatCommands.json";

    public const string PathChatHistory = "ModData\\chat.txt";

    public const string PathHotkeyCommand = "ModData\\hotkeyCommands.json";

    public const sbyte ID_SKILL_BUFF = 7;

    public const sbyte ID_SKILL_TTNL = 8;

    public const sbyte ID_SKILL_TDHS = 6;

    public const int ID_ICON_ITEM_TDLT = 4387;

    public const short ID_NPC_MOD_FACE = 7333;

    private const BindingFlags PUBLIC_STATIC_VOID = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod;

    public static string status;

    public static long lastUsePean;

    public static int speedRun;

    public static Waypoint waypointLeft;

    public static Waypoint waypointMiddle;

    public static Waypoint waypointRight;

    public static string username;

    public static string password;

    public static Utilities gI { get; }

    private Utilities()
    {
    }

    static Utilities()
    {
        status = "Đã kết nối";
        gI = new Utilities();
        speedRun = 8;
        username = "";
        password = "";
    }

    public static bool isFrameMultipleOf(int multiple)
    {
        return GameCanvas.gameTick % (multiple * Time.timeScale) == 0;
    }

    public static MethodInfo[] getMethods(string typeFullName)
    {
        return AppDomain.CurrentDomain.GetAssemblies().First((Assembly x) => x.ManifestModule.Name == "Assembly-CSharp.dll").GetTypes()
            .FirstOrDefault((Type x) => x.FullName.ToLower() == typeFullName.ToLower())
            .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod);
    }

    public static IEnumerable<MethodInfo> GetMethods()
    {
        return (from x in AppDomain.CurrentDomain.GetAssemblies().First((Assembly x) => x.ManifestModule.Name == "Assembly-CSharp.dll").GetTypes()
                where x.IsClass
                select x).SelectMany((Type x) => x.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.InvokeMethod));
    }

    public static MyVector getMyVectorMe()
    {
        MyVector myVector = new MyVector();
        myVector.addElement(Char.myCharz());
        return myVector;
    }

    public static int GetHongNgoc()
    {
        return Char.myCharz().luongKhoa;
    }

    public static bool canBuffMe(out Skill skillBuff)
    {
        skillBuff = Char.myCharz().getSkill(new SkillTemplate
        {
            id = ID_SKILL_BUFF
        });
        if (skillBuff == null || skillBuff.paintCanNotUseSkill)
        {
            return false;
        }
        return true;
    }



    public static bool canSkillUse(sbyte idSkill)
    {
        if (Char.myCharz().getSkill(new SkillTemplate
        {
            id = idSkill
        }) == null)
        {
            return false;
        }
        return true;
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

    public static bool isUsingTDLT()
    {
        return ItemTime.isExistItem(4387);
    }

    public static int getXWayPoint(Waypoint waypoint)
    {
        if (waypoint.maxX >= 60)
        {
            if (waypoint.minX <= TileMap.pxw - 60)
            {
                return waypoint.minX + 30;
            }
            return TileMap.pxw - 15;
        }
        return 15;
    }

    public static int getYWayPoint(Waypoint waypoint)
    {
        return waypoint.maxY;
    }

    public static sbyte getIndexItemBag(params short[] templatesId)
    {
        Char myChar = Char.myCharz();
        int length = myChar.arrItemBag.Length;
        for (sbyte i = 0; i < length; i++)
        {
            Item item = myChar.arrItemBag[i];
            if (item != null && templatesId.Contains(item.template.id))
            {
                return i;
            }
        }
        return -1;
    }

    public static void teleToNpc(Npc npc)
    {
        teleportMyChar(npc.cx, npc.ySd - npc.ySd % 24);
        Char.myCharz().npcFocus = npc;
    }

    public static void requestChangeMap(Waypoint waypoint)
    {
        if (waypoint.isOffline)
        {
            Service.gI().getMapOffline();
        }
        else
        {
            Service.gI().requestChangeMap();
        }
    }

    public static void setWaypointChangeMap(Waypoint waypoint)
    {
        int cMapID = TileMap.mapID;
        string textPopup = getTextPopup(waypoint.popup);
        if (cMapID != 27 || !(textPopup == "Tường thành 1"))
        {
            if ((cMapID == 70 && textPopup == "Vực cấm") || (cMapID == 73 && textPopup == "Vực chết") || (cMapID == 110 && textPopup == "Rừng tuyết"))
            {
                waypointLeft = waypoint;
            }
            else if (((cMapID == 106 || cMapID == 107) && textPopup == "Hang băng") || ((cMapID == 105 || cMapID == 108) && textPopup == "Rừng băng") || (cMapID == 109 && textPopup == "Cánh đồng tuyết"))
            {
                waypointMiddle = waypoint;
            }
            else if (cMapID == 70 && textPopup == "Căn cứ Raspberry")
            {
                waypointRight = waypoint;
            }
            else if (waypoint.maxX < 60)
            {
                waypointLeft = waypoint;
            }
            else if (waypoint.minX > TileMap.pxw - 60)
            {
                waypointRight = waypoint;
            }
            else
            {
                waypointMiddle = waypoint;
            }
        }
    }

    public static void updateWaypointChangeMap()
    {
        waypointLeft = (waypointMiddle = (waypointRight = null));
        int vGoSize = TileMap.vGo.size();
        for (int i = 0; i < vGoSize; i++)
        {
            setWaypointChangeMap((Waypoint)TileMap.vGo.elementAt(i));
        }
    }

    public static void setSpeedRun(int speed)
    {
        speedRun = speed;
        GameScr.info1.addInfo("Tốc độ chạy: " + speed, 0);
    }

    public static void setSpeedGame(float speed)
    {
        Time.timeScale = speed;
        GameScr.info1.addInfo("Tốc độ game: " + speed, 0);
    }

    public static void buffMe()
    {
        if (canBuffMe(out var skillBuff))
        {
            //Service.gI().selectSkill(ID_SKILL_BUFF);
            //Service.gI().sendPlayerAttack(new MyVector(), getMyVectorMe(), -1);
            //Service.gI().selectSkill(Char.myCharz().myskill.template.id);
            //skillBuff.lastTimeUseThisSkill = mSystem.currentTimeMillis();
            GameScr.gI().doSelectSkill(skillBuff, true);
            Service.gI().sendPlayerAttack(new MyVector(), getMyVectorMe(), -1);
        }
    }

    public static void Ttnl()
    {
        if(canTtnl(out Skill skillTtnl))
        {
            GameScr.gI().doUseSkillNotFocus(skillTtnl);
        }
    }

    public static void Tdhs()
    {
        if (canTdhs(out Skill skillTdhs))
        {
            GameScr.gI().doUseSkillNotFocus(skillTdhs);
        }
    }

    private static bool canTtnl(out Skill skillTtnl)
    {
        skillTtnl = Char.myCharz().getSkill(new SkillTemplate
        {
            id = ID_SKILL_TTNL
        });
        if (skillTtnl == null || skillTtnl.paintCanNotUseSkill)
        {
            return false;
        }
        return true;
    }

    private static bool canTdhs(out Skill skillTdhs)
    {
        skillTdhs = Char.myCharz().getSkill(new SkillTemplate
        {
            id = ID_SKILL_TDHS
        });
        if (skillTdhs == null || skillTdhs.paintCanNotUseSkill)
        {
            return false;
        }
        return true;
    }

    public static void doUsePean()
    {
        if (!Char.myCharz().stone && !Char.myCharz().blindEff && Char.myCharz().holdEffID <= 0)
        {
            long num = mSystem.currentTimeMillis();
            if (num - lastUsePean >= 10000 && Char.myCharz().doUsePotion())
            {
                lastUsePean = num;
            }
        }
    }

    public static void autoAttackSkillLong(MyVector myVector, MyVector myVector1)
    {
        sbyte[] skill = new sbyte[5] { 23, 5, 18, 3, 6 };
        for (int i = 0; i < skill.Length; i++)
        {
            if (canSkillUse(skill[i]))
            {
                Service.gI().selectSkill(skill[i]);
                Service.gI().sendPlayerAttack(myVector, myVector1, -1);
                Service.gI().selectSkill(Char.myCharz().myskill.template.id);
                break;
            }
        }
    }

    public static void teleportMyChar(int x, int y)
    {
        Char.myCharz().currentMovePoint = null;
        Char.myCharz().cx = x;
        Char.myCharz().cy = y;
        Service.gI().charMove();
        if (!GameScr.canAutoPlay)
        {
            Char.myCharz().cx = x;
            Char.myCharz().cy = y + 1;
            Service.gI().charMove();
            Char.myCharz().cx = x;
            Char.myCharz().cy = y;
            Service.gI().charMove();
        }
    }

    public static void useCapsule()
    {
        sbyte index = getIndexItemBag(193, 194);
        if (index == -1)
        {
            GameScr.info1.addInfo("Không tìm thấy capsule", 0);
        }
        else
        {
            Service.gI().useItem(0, 1, index, -1);
        }
    }

    public static void usePorata()
    {
        sbyte index = getIndexItemBag(921, 454);
        if (index == -1)
        {
            GameScr.info1.addInfo("Không tìm thấy bông tai", 0);
        }
        else
        {
            Service.gI().useItem(0, 1, index, -1);
        }
    }

    public static void changeMapLeft()
    {
        Waypoint waypoint = waypointLeft;
        if (waypoint != null)
        {
            teleportMyChar(getXWayPoint(waypoint), getYWayPoint(waypoint));
            requestChangeMap(waypoint);
        }
    }

    public static void changeMapMiddle()
    {
        Waypoint waypoint = waypointMiddle;
        if (waypoint != null)
        {
            if (TileMap.mapID == 16)
            {
                teleportMyChar(getXWayPoint(waypoint) - 15, getYWayPoint(waypoint));
            }
            else
            {
                teleportMyChar(getXWayPoint(waypoint), getYWayPoint(waypoint));
            }
            requestChangeMap(waypoint);
        }
    }

    public static void changeMapRight()
    {
        Waypoint waypoint = waypointRight;
        if (waypoint != null)
        {
            teleportMyChar(getXWayPoint(waypoint), getYWayPoint(waypoint));
            requestChangeMap(waypoint);
        }
    }

    public static void sendGiaoDichToCharFocusing()
    {
        Char charFocus = Char.myCharz().charFocus;
        if (charFocus == null)
        {
            GameScr.info1.addInfo("Trỏ vào nhân vật để giao dịch", 0);
            return;
        }
        Service.gI().giaodich(0, charFocus.charID, -1, -1);
        GameScr.info1.addInfo("Đã gửi lời mời giao dịch đến " + charFocus.cName, 0);
    }

    public static void openUiZone()
    {
        Service.gI().openUIZone();
    }

    public static void changeZone(int zone)
    {
        Service.gI().requestChangeZone(zone, -1);
    }

    public static bool isMeInNRDMap()
    {
        if (TileMap.mapID >= 85)
        {
            return TileMap.mapID <= 91;
        }
        return false;
    }

    public static void ResetTF()
    {
        ChatTextField.gI().strChat = "Chat";
        ChatTextField.gI().tfChat.name = "chat";
        ChatTextField.gI().tfChat.setIputType(TField.INPUT_TYPE_ANY);
        ChatTextField.gI().isShow = false;
    }

    public static void saveRMSInt(string name, int value)
    {
        if (!Directory.Exists("Data"))
        {
            Directory.CreateDirectory("Data");
        }
        FileStream fileStream = new FileStream("Data\\" + name, FileMode.Create);
        fileStream.Write(BitConverter.GetBytes(value), 0, 4);
        fileStream.Flush();
        fileStream.Close();
    }

    public static int loadRMSInt(string name)
    {
        FileStream fileStream = new FileStream("Data\\" + name, FileMode.Open);
        byte[] array = new byte[4];
        fileStream.Read(array, 0, 4);
        fileStream.Close();
        return BitConverter.ToInt32(array, 0);
    }

    public static void saveRMSBool(string name, bool status)
    {
        if (!Directory.Exists("Data"))
        {
            Directory.CreateDirectory("Data");
        }
        FileStream fileStream = new FileStream("Data\\" + name, FileMode.Create);
        fileStream.Write(new byte[1] { status ? ((byte)1) : ((byte)0) }, 0, 1);
        fileStream.Flush();
        fileStream.Close();
    }

    public static bool loadRMSBool(string name)
    {
        FileStream fileStream = new FileStream("Data\\" + name, FileMode.Open);
        byte[] array = new byte[1];
        fileStream.Read(array, 0, 1);
        fileStream.Close();
        return array[0] == 1;
    }

    public static string loadRMSString(string name)
    {
        FileStream fileStream = new FileStream("Data\\" + name, FileMode.Open);
        StreamReader streamReader = new StreamReader(fileStream);
        string result = streamReader.ReadToEnd();
        streamReader.Close();
        fileStream.Close();
        return result;
    }

    public static void saveRMSString(string name, string data)
    {
        if (!Directory.Exists("Data"))
        {
            Directory.CreateDirectory("Data");
        }
        FileStream fileStream = new FileStream("Data\\" + name, FileMode.Create);
        byte[] buffer = Encoding.UTF8.GetBytes(data);
        fileStream.Write(buffer, 0, buffer.Length);
        fileStream.Flush();
        fileStream.Close();
    }

    public static void teleportMyChar(IMapObject obj)
    {
        teleportMyChar(obj.getX(), obj.getY());
    }

    public static void teleportMyChar(int x)
    {
        teleportMyChar(x, getYGround(x));
    }

    [Obsolete("Không dùng nữa")]
    internal static int getWidth(GUIStyle gUIStyle, string s)
    {
        return (int)gUIStyle.CalcSize(new GUIContent(s)).x / mGraphics.zoomLevel + 30;
    }

    public static int getYGround(int x)
    {
        int y = 50;
        for (int i = 0; i < 30; i++)
        {
            y += 24;
            if (TileMap.tileTypeAt(x, y, 2))
            {
                if (y % 24 != 0)
                {
                    y -= y % 24;
                }
                return y;
            }
        }
        return -1;
    }

    public static int getDistance(IMapObject mapObject1, IMapObject mapObject2)
    {
        return Res.distance(mapObject1.getX(), mapObject1.getY(), mapObject2.getX(), mapObject2.getY());
    }

    public static void KhinhCong()
    {
        Char.myCharz().cy -= 50;
        Service.gI().charMove();
    }

    public static void DonTho()
    {
        Char.myCharz().cy += 50;
        Service.gI().charMove();
    }

    public static void DichTrai()
    {
        Char.myCharz().cx -= 50;
        Service.gI().charMove();
    }

    public static void DichPhai()
    {
        Char.myCharz().cx += 50;
        Service.gI().charMove();
    }

    public static short getNRSDId()
    {
        if (isMeInNRDMap())
        {
            return (short)(2400 - TileMap.mapID);
        }
        return 0;
    }

    public static bool isMeWearingActivationSet(int idSet)
    {
        int activateCount = 0;
        for (int i = 0; i < 5; i++)
        {
            Item item = Char.myCharz().arrItemBody[i];
            if (item == null)
            {
                return false;
            }
            if (item.itemOption == null)
            {
                return false;
            }
            for (int j = 0; j < item.itemOption.Length; j++)
            {
                if (item.itemOption[j].optionTemplate.id == idSet)
                {
                    activateCount++;
                    break;
                }
            }
        }
        return activateCount == 5;
    }

    public static bool isMeWearingTXHSet()
    {
        if (Char.myCharz().cgender == 0)
        {
            return isMeWearingActivationSet(141);
        }
        return false;
    }

    public static bool isMeWearingCadicSet()
    {
        if (Char.myCharz().cgender == 2)
        {
            return isMeWearingActivationSet(0);
        }
        return false;
    }

    public static bool isMeWearingPikkoroDaimaoSet()
    {
        if (Char.myCharz().cgender == 1)
        {
            return isMeWearingActivationSet(0);
        }
        return false;
    }

    public static void DoDoubleClickToObj(IMapObject mapObject)
    {
        typeof(GameScr).GetMethod("doDoubleClickToObj", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.InvokeMethod).Invoke(GameScr.gI(), new object[1] { mapObject });
    }

    public static bool isHome()
    {
        return TileMap.mapID == Char.myCharz().cgender + 21;
    }
}

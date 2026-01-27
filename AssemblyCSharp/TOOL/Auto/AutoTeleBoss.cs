using System.Collections.Generic;
using AssemblyCSharp.TOOL.ToolHelper;
using TOOl;

namespace AssemblyCSharp.TOOL.Auto;

public static class AutoTeleBoss
{
    public static long nextTeleTime = 0L;

    public static void Update()
    {
        if (Char.myCharz().itemFocus != null || mSystem.currentTimeMillis() < nextTeleTime || Char.myCharz().meDead)
            return;
        Char teleChar = null;
        for(int i =0; i< GameScr.vCharInMap.size(); i++)
        {
            Char obj = (Char)GameScr.vCharInMap.elementAt(i);
            if (obj!=null && isBoss(obj) && !Char.myCharz().meDead && obj.cHP > 0 && obj.cx > 10 && obj.cy < TileMap.pxh - 10 && obj.cx < TileMap.pxw - 10)
            {
                teleChar = obj;
                break;
            }
        }
        if (teleChar != null)
        {
            int disX = Math.abs(teleChar.getX() - Char.myCharz().getX());
            int disY = Math.abs(teleChar.getY() - Char.myCharz().getY());
            double distance = System.Math.Sqrt(disX * disX + disY * disY);
            if(distance > 30.0)
            {
                Utilities.teleportMyChar(teleChar.cx, GetClosestGroundY(teleChar.cx, teleChar.cy));
                nextTeleTime = mSystem.currentTimeMillis() + 2500L;
            }
        }

    }

    private static bool isBoss(Char obj)
    {
        if (obj.cTypePk == 5 && (obj.cName.Contains("Số") || obj.cName.Contains("Tiểu")) && Char.myCharz().isMeCanAttackOtherPlayer(obj))
        {
            return !obj.meDead;
        }
        return false;
    }

    public static int GetClosestGroundY(int x, int targetY)
    {
        List<int> groundYs = new List<int>();
        int stepSize = 24;
        int y = 50;
        for (int maxY = TileMap.pxh; y <= maxY; y += stepSize)
        {
            if (TileMap.tileTypeAt(x, y, 2))
            {
                if (y % 24 != 0)
                {
                    y -= y % 24;
                }
                groundYs.Add(y);
            }
        }
        if (groundYs.Count == 0)
        {
            GameScr.info1.addInfo($"Không tìm thấy mặt đất tại x={x}, sử dụng y={GetYGround(x)}", 0);
            return GetYGround(x);
        }
        int closestY = groundYs[0];
        int minDistance = Math.abs(groundYs[0] - targetY);
        foreach (int groundY in groundYs)
        {
            int distance = Math.abs(groundY - targetY);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestY = groundY;
            }
        }
        return closestY;
    }

    public static int GetYGround(int x)
    {
        int num = 50;
        int num2 = 0;
        while (num2 < 30)
        {
            num2++;
            num += 24;
            if (TileMap.tileTypeAt(x, num, 2))
            {
                if (num % 24 != 0)
                {
                    num -= num % 24;
                }
                break;
            }
        }
        return num;
    }
}

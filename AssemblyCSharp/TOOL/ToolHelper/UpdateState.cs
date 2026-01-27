using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using AssemblyCSharp.TOOL.ToolHelper;

namespace AssemblyCSharp.TOOL.ToolHelper
{
    public class UpdateState : ThreadActionUpdate<UpdateState>
    {
        public override int Interval => 100;

        protected override void update()
        {
            if (string.IsNullOrEmpty(Char.myCharz()?.cName))
                return;

            SocketClient.gI.sendMessage(new
            {
                action = "updateState",
                zone = TileMap.zoneID!=null ? TileMap.zoneID : -1,
                hongNgoc = Char.myCharz().luongKhoa != null ? Char.myCharz().luongKhoa : 0,
            });
        }
    }
}

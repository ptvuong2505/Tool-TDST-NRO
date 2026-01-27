using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QLTK_TOOL_TDST.Models
{
    public class Boss
    {
        public string NameBoss { get; set; }
        public int IdMap { get; set; }
        public int Zone { get; set; }
        public bool IsDied { get; set; }
        
        public Boss(string nameBoss, int mapId)
        {
            NameBoss = nameBoss ?? "";
            IdMap = mapId;  
            Zone = 0;
            IsDied = false;
        }

        public Boss(string nameBoss, int mapId, int zone, bool isDied)
        {
            NameBoss = nameBoss ?? "";
            IdMap = mapId;
            Zone = zone;
            IsDied = isDied;
        }
    }
}

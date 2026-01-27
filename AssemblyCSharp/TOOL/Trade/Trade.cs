using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace AssemblyCSharp.TOOL.Trade
{
    public static class Trade
    {
        public static int idCharTrade;
        public static bool isTraded;
        public static bool isTrading;
        public static int idTradeItem;
        public static int FindIndexItemById(int idItem)
        {
            for(int i =0; i<global::Char.myCharz().arrItemBag.Length; i++)
            {
                if(global::Char.myCharz().arrItemBag[i] != null && global::Char.myCharz().arrItemBag[i].template.id == idItem)
                {
                    return i;
                }
            }
            return -1;
        }

        public static void AutoCancelTrade()
        {
            Thread.Sleep(15000);
            if (isTrading)
            {
                Service.gI().giaodich(3, -1, -1, -1); //Cancel trade
            }
        }

        public static void CheckTrade(string infor)
        {
            if(infor.Contains("thành công"))
            {
                // Trade successful, Do something    
                isTraded = false;
                isTrading = false;
            }
            if (infor.Contains("hủy bỏ"))
            {
                isTraded = false;
                isTrading = false;
            }
        }

        public static void TradeGold()
        {
            Service.gI().giaodich(2, -1, -1, 1); // Trade 1 gold
            Service.gI().giaodich(5, -1, -1, -1); // Lock trade
            //Service.gI().giaodich(7, -1, -1, -1); // Confirm trade
            new Thread(AutoCancelTrade).Start(); // Start auto cancel trade after 15 seconds
        }

        public static void SelectTradeItems()
        {
            if (isTraded)
            {
                return;
            }
            Trade.isTraded = true;
            //string json = File.ReadAllText("Data/tradeObjects.json");
            //List<TradeObject> trades = JsonConvert.DeserializeObject<List<TradeObject>>(json);
            List<int> trades = new List<int> {7,152};
            foreach (var trade in trades)
            {
                  int indexItem = FindIndexItemById(trade);
                  if (indexItem != -1)
                  {
                      Service.gI().giaodich(2, -1, (sbyte)indexItem, 0);
                  }
            }
            Service.gI().giaodich(5, -1, -1, -1); // Lock trade
            Thread.Sleep(15000);
            if (isTrading)
            {
                Service.gI().giaodich(3, -1, -1, -1); //Cancel trade
            }
        }
    }
}

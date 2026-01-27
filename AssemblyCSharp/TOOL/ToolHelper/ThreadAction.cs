using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace AssemblyCSharp.TOOL.ToolHelper
{
    public abstract class ThreadAction<T> where T : ThreadAction<T>, new() 
    {
        public static T gI { get; } = new T();

        /// Kiểm tra hành động còn thực hiện.
        public bool IsActing => threadAction?.IsAlive == true;

        /// Thread sử dụng để thực thi hành động.
        protected Thread threadAction;

        /// Hành động cần thực hiện.
        protected abstract void action();

        /// Thực thi hành động bằng thread của instance.
        public void performAction()
        {
            if (IsActing)
                threadAction.Abort();

            executeAction();
        }

        /// Sử dụng thread của instance để thực thi hành động.
        protected void executeAction()
        {
            // Không thực hiện hành động trong luồng khác
            if (Thread.CurrentThread != threadAction)
            {
                threadAction = new Thread(executeAction)
                {
                    IsBackground = true,
                    Name = "ExecuteAction"
                };
                threadAction.Start();
                return;
            }
            action();
        }

    }
}

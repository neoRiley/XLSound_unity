using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XavierLab
{
    public class AsyncFactory : TimerFactory
    {
        public override BaseTimer Create() => new AsyncTimer();
    }
}

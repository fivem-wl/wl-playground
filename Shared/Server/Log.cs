using System;
using System.Collections.Generic;
using System.Text;

using CitizenFX.Core;


namespace Server
{
    /// <summary>
    /// Log.
    /// </summary>
    public static class Log
    {
        public static void Debug(string data)
        {
#if DEBUG
            CitizenFX.Core.Debug.WriteLine($"[DEBUG]{data}");
#endif
        }

        public static void Info(string data)
        {
            CitizenFX.Core.Debug.WriteLine($"[INFO]{data}");
        }
    }
}

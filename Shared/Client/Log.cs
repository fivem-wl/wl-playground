using System;
using System.Collections.Generic;
using System.Text;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;

namespace Client
{
    /// <summary>
    /// Log.
    /// </summary>
    public static class Log
    {
        public static void Debug(string data)
        {
            #if DEBUG
            CitizenFX.Core.Debug.WriteLine(@data);
            #endif
        }
    }
}

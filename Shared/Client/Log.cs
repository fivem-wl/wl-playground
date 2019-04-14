using System;
using System.Collections.Generic;
using System.Text;

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
            Debug.WriteLine(@data);
            #endif
        }
    }
}

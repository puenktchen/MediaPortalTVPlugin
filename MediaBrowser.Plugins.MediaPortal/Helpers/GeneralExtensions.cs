using System;
using System.Collections.Generic;
using System.Linq;

using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Plugins.MediaPortal.Services.Entities;

namespace MediaBrowser.Plugins.MediaPortal.Helpers
{
    public static class GeneralExtensions
    {
        public static String ToUrlDate(this DateTimeOffset value)
        {
            return value.ToString("s");
        }
    }
}

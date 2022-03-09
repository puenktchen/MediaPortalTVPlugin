using System;
using System.Collections.Generic;
using System.Text;

namespace MediaBrowser.Plugins.MediaPortal
{
    public class MediaPortalOptions
    {
        /// <summary>
        /// The user name for authenticating with MPExtended
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// The password for authenticating with MPExtended
        /// </summary>
        public string Password { get; set; }
    }
}

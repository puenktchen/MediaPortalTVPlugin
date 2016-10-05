using System;
using System.Collections.Generic;

using MediaBrowser.Model.Plugins;
using MediaBrowser.Plugins.MediaPortal.Entities;

namespace MediaBrowser.Plugins.MediaPortal.Configuration
{
    /// <summary>
    /// Class PluginConfiguration
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration" /> class.
        /// </summary>
        public PluginConfiguration()
        {
            ApiHostName = "localhost";
            ApiPortNumber = 4322;
            StreamingProfileName = "Direct";
            PreviewThumbnailOffsetMinutes = 5;
            MediaInfoDelay = 1500;
            EnableDirectPlay = true;
            EnableTimerCache = true;

            // Initialise this
            GenreMappings = new SerializableDictionary<string, List<string>>();
        }

        /// <summary>
        /// The url / ip address that MPExtended is hosted on
        /// </summary>
        public string ApiHostName { get; set; }

        /// <summary>
        /// The port number that MPExtended is hosted on
        /// </summary>
        public Int32 ApiPortNumber { get; set; }

        /// <summary>
        /// Indicates whether MPExtended requires authentication
        /// </summary>
        public bool RequiresAuthentication { get; set; }

        /// <summary>
        /// The user name for authenticating with MPExtended
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// The password for authenticating with MPExtended
        /// </summary>
        public string Password { get; set; }

        /// <summary>
        /// The default channel group to use in MB
        /// </summary>
        public Int32 DefaultChannelGroup { get; set; }

        /// <summary>
        /// The genre mappings, to map localised MP genres, to MB genres.
        /// </summary>
        public SerializableDictionary<String, List<String>> GenreMappings { get; set; }

        /// <summary>
        /// The default ordering of channels
        /// </summary>
        public ChannelSorting DefaultChannelSortOrder { get; set; }

        /// <summary>
        /// Enable channel index
        /// </summary>
        public bool ChannelByIndex { get; set; }

        /// <summary>
        /// The name of the MPExtended profile to use for streaming
        /// </summary>
        public String StreamingProfileName { get; set; }

        /// <summary>
        /// Delay reading of stream mediainfo in ms
        /// </summary>
        public Int32? MediaInfoDelay { get; set; }

        /// <summary>
        /// Enable direct play of streams
        /// </summary>
        public bool EnableDirectPlay { get; set; }

        /// <summary>
        /// Limit direct play up to 720p
        /// </summary>
        public bool LimitStreaming { get; set; }

        /// <summary>
        /// Enable direct access to recordings
        /// </summary>
        public bool EnableDirectAccess { get; set; }

        /// <summary>
        /// Enable Path Substitution
        /// </summary>
        public bool RequiresPathSubstitution { get; set; }

        /// <summary>
        /// The lokal recording folder of MediaPortal
        /// </summary>
        public string LocalFilePath { get; set; }

        /// <summary>
        /// The remote recording share of MediaPortal
        /// </summary>
        public string RemoteFilePath { get; set; }

        /// <summary>
        /// The number of minutes into a recorded program to grab the screenshot for previewing.
        /// </summary>
        public Int32 PreviewThumbnailOffsetMinutes { get; set; }

        /// <summary>
        /// Enable one time schedules caching
        /// </summary>
        public bool EnableTimerCache { get; set; }

        /// <summary>
        /// Enable ffrobe instead of mediainfo
        /// </summary>
        public bool EnableFFProbe{ get; set; }

        /// <summary>
        /// Enable additional logging
        /// </summary>
        public bool EnableLogging { get; set; }

        /// <summary>
        /// Validates the configuration
        /// </summary>
        /// <returns></returns>
        public ValidationResult Validate()
        {
            if (String.IsNullOrEmpty(ApiHostName))
            {
                return new ValidationResult(false, "Please specify an API HostName (the box MPExtended is installed on)");
            }

            if (ApiPortNumber < 1)
            {
                return new ValidationResult(false, "Please specify an API Port Number (usually 4322)");
            }

            if (RequiresAuthentication)
            {
                if (String.IsNullOrEmpty(UserName))
                {
                    return new ValidationResult(false, "Please specify a UserName (check MPExtended - Authentication");
                }

                if (String.IsNullOrEmpty(Password))
                {
                    return new ValidationResult(false, "Please specify an Password (check MPExtended - Authentication");
                }
            }

            if (!MediaInfoDelay.HasValue)
            {
                MediaInfoDelay = 1500;
            }

            if (RequiresPathSubstitution)
            {
                if (String.IsNullOrEmpty(LocalFilePath))
                {
                    return new ValidationResult(false, "Please specify MediaPortals local recording folder");
                }

                if (String.IsNullOrEmpty(RemoteFilePath))
                {
                    return new ValidationResult(false, "Please specify MediaPortals remote recording share");
                }
            }

            return new ValidationResult(true, String.Empty);
        }
    }

    public class ValidationResult
    {
        public ValidationResult(Boolean isValid, String summary)
        {
            IsValid = isValid;
            Summary = summary;
        }

        public Boolean IsValid { get; set; }
        public String Summary { get; set; }
    }
}

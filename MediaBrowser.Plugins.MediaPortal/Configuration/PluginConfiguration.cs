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
            ProgramImages = true;
            EnableProbing = true;
            StreamingProfileName = "Direct";
            StreamDelay = 0;
            EnableRecordingImport = true;
            PreviewThumbnailOffsetMinutes = 10;
            WeeklyEveryTimeOnThisChannel = true;

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
        /// The default tv channel group to use in Emby
        /// </summary>
        public Int32 TvChannelGroup { get; set; }

        /// <summary>
        /// The default radio channel group to use in Emby
        /// </summary>
        public Int32 RadioChannelGroup { get; set; }

        /// <summary>
        /// The genre mappings, to map localised MP genres, to Emby categories.
        /// </summary>
        public SerializableDictionary<String, List<String>> GenreMappings { get; set; }

        /// <summary>
        /// Enable program images
        /// </summary>
        public bool ProgramImages { get; set; }

        /// <summary>
        /// Enables streaming probing for live tv
        /// </summary>
        public bool EnableProbing { get; set; }

        /// <summary>
        /// Enable RTSP streaming for live tv
        /// </summary>
        public bool RtspStreaming { get; set; }

        /// <summary>
        /// The name of the MPExtended profile to use for streaming
        /// </summary>
        public String StreamingProfileName { get; set; }

        /// <summary>
        /// Delay reading of the stream in ms
        /// </summary>
        public Int32? StreamDelay { get; set; }

        /// <summary>
        /// Enable import of MediaPortal recordings
        /// </summary>
        public bool EnableRecordingImport { get; set; }

        /// <summary>
        /// Enable TMDB online lookup for recording posters
        /// </summary>
        public bool EnableTmdbLookup { get; set; }

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
        /// MediaPortal series timer type
        /// </summary>
        public bool EveryTimeOnThisChannel { get; set; }

        /// <summary>
        /// MediaPortal series timer type
        /// </summary>
        public bool EveryTimeOnEveryChannel { get; set; }

        /// <summary>
        /// MediaPortal series timer type
        /// </summary>
        public bool WeeklyEveryTimeOnThisChannel { get; set; }

        /// <summary>
        /// Skips timers if item is already in Emby library
        /// </summary>
        public bool SkipAlreadyInLibrary { get; set; }

        /// <summary>
        /// Skips timers method for items already in Emby library
        /// </summary>
        public String SkipAlreadyInLibraryProfile { get; set; }

        /// <summary>
        /// Autocreates timers based on missing episodes in Emby library
        /// </summary>
        public bool AutoCreateTimers { get; set; }

        public String SeriesTimerType()
        {
            if (EveryTimeOnThisChannel && !EveryTimeOnEveryChannel && !WeeklyEveryTimeOnThisChannel)
                return "3";
            else if (!EveryTimeOnThisChannel && EveryTimeOnEveryChannel && !WeeklyEveryTimeOnThisChannel)
                return "4";
            else if (!EveryTimeOnThisChannel && !EveryTimeOnEveryChannel && WeeklyEveryTimeOnThisChannel)
                return "7";
            else
                return null;
        }

        /// <summary>
        /// The number of minutes into a recorded program to grab the screenshot for previewing.
        /// </summary>
        public Int32 PreviewThumbnailOffsetMinutes { get; set; }

        /// <summary>
        /// Enable custom image processing
        /// </summary>
        public bool EnableImageProcessing { get; set; }

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

            if (!StreamDelay.HasValue)
            {
                StreamDelay = 0;
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

            if (String.IsNullOrEmpty(SeriesTimerType()))
            {
                return new ValidationResult(false, "Please specify MediaPortals default series timer type");
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

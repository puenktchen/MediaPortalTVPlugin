using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

using MediaBrowser.Plugins.MediaPortal.Helpers;
using MediaBrowser.Plugins.MediaPortal.Services;

namespace MediaBrowser.Plugins.MediaPortal
{
    public class Plugin : BasePlugin, IHasWebPages, IHasThumbImage, IHasTranslations
    {
        public static Plugin Instance { get; private set; }
        public static IConfigurationManager ConfigurationManager { get; set; }
        public static IFfmpegManager FfmpegManager { get; set; }
        public static IImageProcessor ImageProcessor { get; set; }
        public static ILiveTvManager LiveTvManager { get; set; }
        public static ILogger Logger { get; set; }
        public static ImageCreator ImageCreator { get; private set; }
        public static StreamingService StreamingService { get; private set; }
        public static TVService TVService { get; private set; }

        public Plugin
        (
            IHttpClient httpClient,
            IJsonSerializer jsonSerializer,
            IConfigurationManager configurationManager,
            IFfmpegManager ffmpegManager,
            IImageProcessor imageProcessor,
            ILiveTvManager liveTvManager,
            ILogger logger
        )
            : base()
        {
            Instance = this;

            ConfigurationManager = configurationManager;
            FfmpegManager = ffmpegManager;
            ImageProcessor = imageProcessor;
            LiveTvManager = liveTvManager;
            Logger = logger;

            ImageCreator = new ImageCreator();
            StreamingService = new StreamingService(httpClient, jsonSerializer);
            TVService = new TVService(httpClient, jsonSerializer);
        }

        public static string StaticName = "MediaPortal";

        public override string Name
        {
            get { return StaticName; }
        }

        public override string Description
        {
            get
            {
                return "Live tv plugin to use MediaPortal as a tuner source for Emby";
            }
        }

        public Stream GetThumbImage()
        {
            var type = GetType();
            return type.Assembly.GetManifestResourceStream(type.Namespace + ".thumb.png");
        }

        public ImageFormat ThumbImageFormat
        {
            get
            {
                return ImageFormat.Png;
            }
        }

        private Guid _id = new Guid("2c6a0219-7621-4b06-8a64-da3f7038b649");
        public override Guid Id
        {
            get { return _id; }
        }

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new PluginPageInfo[]
            {
                new PluginPageInfo
                {
                    Name = "mediaportal",
                    EmbeddedResourcePath = GetType().Namespace + ".web.mediaportal.html",
                    IsMainConfigPage = false
                },
                new PluginPageInfo
                {
                    Name = "mediaportaljs",
                    EmbeddedResourcePath = GetType().Namespace + ".web.mediaportal.js"
                }
            };
        }

        public TranslationInfo[] GetTranslations()
        {
            var basePath = GetType().Namespace + ".strings.";

            return GetType()
                .Assembly
                .GetManifestResourceNames()
                .Where(i => i.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                .Select(i => new TranslationInfo
                {
                    Locale = Path.GetFileNameWithoutExtension(i.Substring(basePath.Length)),
                    EmbeddedResourcePath = i

                }).ToArray();
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;

using MediaBrowser.Controller;

using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Plugins.MediaPortal.Services.Proxies;
using System.IO;
using MediaBrowser.Model.Drawing;

namespace MediaBrowser.Plugins.MediaPortal
{
    /// <summary>
    /// Class Plugin
    /// </summary>
    public class Plugin : BasePlugin, IHasWebPages, IHasThumbImage, IHasTranslations
    {
        public static TvServiceProxy TvProxy { get; private set; }
        public static StreamingServiceProxy StreamingProxy { get; private set; }

        public static ILogger Logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin" /> class.
        /// </summary>
        public Plugin(IHttpClient httpClient,
            IJsonSerializer jsonSerializer, ILogger logger)
            : base()
        {
            Instance = this;

            Logger = logger;

            // Create our shared service proxies
            StreamingProxy = new StreamingServiceProxy(httpClient, jsonSerializer);
            TvProxy = new TvServiceProxy(httpClient, jsonSerializer, StreamingProxy);
        }

        public static string StaticName = "MediaPortal";

        /// <summary>
        /// Gets the name of the plugin
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return StaticName; }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public override string Description
        {
            get
            {
                return "MediaPortal TV Plugin to enable Live TV streaming and scheduling.";
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

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <value>The instance.</value>
        public static Plugin Instance { get; private set; }

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
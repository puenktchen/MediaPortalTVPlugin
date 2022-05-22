using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using MediaBrowser.Model.Services;
using MediaBrowser.Plugins.MediaPortal.Entities;

namespace MediaBrowser.Plugins.MediaPortal.Services
{
    [Route("/MediaPortal/TranscoderProfiles", "GET", Summary = "Gets a list of streaming profiles", IsHidden = true)]
    public class GetTranscoderProfiles : IReturn<List<string>>
    {
    }

    [Route("/MediaPortal/TvChannelGroups", "GET", Summary = "Gets a list of tv channel groups", IsHidden = true)]
    public class GetTvChannelGroups : IReturn<List<ChannelGroup>>
    {
    }

    [Route("/MediaPortal/RadioChannelGroups", "GET", Summary = "Gets a list of radio channel groups", IsHidden = true)]
    public class GetRadioChannelGroups : IReturn<List<ChannelGroup>>
    {
    }

    public class MediaPortalServices : IService
    {
        public object Get(GetTranscoderProfiles request)
        {
            var profiles = new List<TranscoderProfile>();

            try
            {
                var tuner = Plugin.LiveTvManager.GetTunerHostInfos("mediaportal").FirstOrDefault();

                if (tuner == null)
                {
                    profiles = Plugin.StreamingService.GetTranscoderProfiles(@"http://localhost:4322", new MediaPortalOptions(), new CancellationToken()).Result;
                }
                else
                {
                    var config = MediaPortal1TvService.Instance.GetConfiguration(tuner);

                    profiles = Plugin.StreamingService.GetTranscoderProfiles(tuner.Url, config, new CancellationToken()).Result;
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.ErrorException("There was an issue retrieving transcoding profiles", exception);
            }

            return profiles;
        }

        public object Get(GetTvChannelGroups request)
        {
            var tvChannelGroups = new List<ChannelGroup>();

            try
            {
                var tuner = Plugin.LiveTvManager.GetTunerHostInfos("mediaportal").FirstOrDefault();

                if (tuner == null)
                {
                    tvChannelGroups = Plugin.TVService.GetTvChannelGroups(@"http://localhost:4322", new MediaPortalOptions(), new CancellationToken());
                }
                else
                {
                    var config = MediaPortal1TvService.Instance.GetConfiguration(tuner);

                    tvChannelGroups = Plugin.TVService.GetTvChannelGroups(tuner.Url, config, new CancellationToken());
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.ErrorException("There was an issue retrieving tv channel groups", exception);
            }

            return tvChannelGroups;
        }

        public object Get(GetRadioChannelGroups request)
        {
            var radioChannelGroups = new List<ChannelGroup>();

            try
            {
                var tuner = Plugin.LiveTvManager.GetTunerHostInfos("mediaportal").FirstOrDefault();

                if (tuner == null)
                {
                    radioChannelGroups = Plugin.TVService.GetRadioChannelGroups(@"http://localhost:4322", new MediaPortalOptions(), new CancellationToken());
                }
                else
                {
                    var config = MediaPortal1TvService.Instance.GetConfiguration(tuner);

                    radioChannelGroups = Plugin.TVService.GetRadioChannelGroups(tuner.Url, config, new CancellationToken());
                }
            }
            catch (Exception exception)
            {
                Plugin.Logger.ErrorException("There was an issue retrieving radio channel groups", exception);
            }

            return radioChannelGroups;
        }
    }
}
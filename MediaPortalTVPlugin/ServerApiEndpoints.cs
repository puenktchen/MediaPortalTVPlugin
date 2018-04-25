using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using MediaBrowser.Model.Services;
using MediaBrowser.Plugins.MediaPortal.Services.Entities;
using MediaBrowser.Plugins.MediaPortal.Services.Exceptions;

namespace MediaBrowser.Plugins.MediaPortal
{
    [Route("/MediaPortalPlugin/Profiles", "GET", Summary = "Gets a list of streaming profiles")]
    public class GetProfiles : IReturn<List<String>>
    {
    }

    [Route("/MediaPortalPlugin/TvChannelGroups", "GET", Summary = "Gets a list of tv channel groups")]
    public class GetTvChannelGroups : IReturn<List<ChannelGroup>>
    {
    }

    [Route("/MediaPortalPlugin/RadioChannelGroups", "GET", Summary = "Gets a list of radio channel groups")]
    public class GetRadioChannelGroups : IReturn<List<ChannelGroup>>
    {
    }

    [Route("/MediaPortalPlugin/SkipAlreadyInLibraryProfiles", "GET", Summary = "Gets a list of profiles to skip timers for Emy library items")]
    public class GetSkipAlreadyInLibraryProfiles : IReturn<List<String>>
    {
    }

    [Route("/MediaPortalPlugin/TestConnection", "GET", Summary = "Tests the connection to MP Extended")]
    public class GetConnection : IReturn<Boolean>
    {
    }

    public class ServerApiEndpoints : IService
    {
        public object Get(GetProfiles request)
        {
            var profiles = new List<string>();
            try
            {
                profiles = Plugin.StreamingProxy.GetTranscoderProfiles(new CancellationToken()).Select(p => p.Name).ToList();
            }
            catch (ServiceAuthenticationException)
            {
                // Do nothing, allow an empty list to be passed out
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
                tvChannelGroups = Plugin.TvProxy.GetTvChannelGroups(new CancellationToken());
            }
            catch (ServiceAuthenticationException)
            {
                // Do nothing, allow an empty list to be passed out
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
                radioChannelGroups = Plugin.TvProxy.GetRadioChannelGroups(new CancellationToken());
            }
            catch (ServiceAuthenticationException)
            {
                // Do nothing, allow an empty list to be passed out
            }
            catch (Exception exception)
            {
                Plugin.Logger.ErrorException("There was an issue retrieving radio channel groups", exception);
            }

            return radioChannelGroups;
        }

        public object Get(GetSkipAlreadyInLibraryProfiles request)
        {
            return new List<string>(new string[] { "Season and Episode Numbers", "Episode Name" });
        }

        public object Get(GetConnection request)
        {
            try
            {
                var result = Plugin.TvProxy.GetStatusInfo(new CancellationToken());
                return true;
            }
            catch (ServiceAuthenticationException)
            {
                // Do nothing, allow an empty list to be passed out
            }
            catch (Exception exception)
            {
                Plugin.Logger.ErrorException("There was an issue testing the API connection", exception);
            }

            return false;
        }
    }
}

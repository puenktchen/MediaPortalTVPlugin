﻿using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Plugins.MediaPortal.Services
{
    public abstract class ProxyService
    {
        protected ProxyService(IHttpClient httpClient, IJsonSerializer serialiser)
        {
            HttpClient = httpClient;
            Serialiser = serialiser;
        }

        protected IHttpClient HttpClient { get; private set; }
        public IJsonSerializer Serialiser { get; private set; }

        /// <summary>
        /// Gets the end point suffix.
        /// </summary>
        /// <value>
        /// The end point suffix.
        /// </value>
        /// <remarks>The value appended after "MPExtended" on the service url</remarks>
        protected abstract string EndPointSuffix { get; }

        /// <summary>
        /// Retrieves a URL for a given action.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        protected string GetUrl(string url, MediaPortalOptions configuration, string action, params object[] args)
        {
            return GetUrl(url, configuration, EndPointSuffix, action, args);
        }

        /// <summary>
        /// Retrieves a URL for a given action, allows the endpoint to be overriden
        /// </summary>
        /// <param name="endPointSuffixOverride">The endpoint .</param>
        /// <param name="action">The action.</param>
        /// <param name="args">The arguments.</param>
        /// <returns></returns>
        protected string GetUrl(string url, MediaPortalOptions configuration, string endPointSuffixOverride, string action, params object[] args)
        {
            var baseUrl = string.Format("{0}/MPExtended/{1}/", url.TrimEnd('/'), endPointSuffixOverride);

            if (!string.IsNullOrEmpty(configuration.UserName))
            {
                if (Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri uri))
                {
                    var builder = new UriBuilder(uri);
                    builder.UserName = configuration.UserName;
                    builder.Password = configuration.Password;

                    baseUrl = builder.Uri.ToString().TrimEnd('/') + "/";
                }
            }

            return string.Concat(baseUrl, string.Format(action, args));
        }

        protected async Task<TResult> GetFromServiceAsync<TResult>(string url, MediaPortalOptions configuration, CancellationToken cancellationToken, string action, params object[] args)
        {
            var request = CreateRequest(url, configuration, cancellationToken, action, args);

            using (var stream = await HttpClient.Get(request).ConfigureAwait(false))
            {
                return await Serialiser.DeserializeFromStreamAsync<TResult>(stream).ConfigureAwait(false);
            }
        }

        private HttpRequestOptions CreateRequest(string url, MediaPortalOptions configuration, string action, params object[] args)
        {
            var request = new HttpRequestOptions()
            {
                Url = GetUrl(url, configuration, action, args),
                RequestContentType = "application/json",
                LogErrorResponseBody = true,
                LogRequest = true,
            };

            if (!string.IsNullOrEmpty(configuration.UserName))
            {
                string authInfo = String.Format("{0}:{1}", configuration.UserName, configuration.Password ?? string.Empty);
                authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
                request.RequestHeaders["Authorization"] = "Basic " + authInfo;
            }

            return request;
        }

        private HttpRequestOptions CreateRequest(string url, MediaPortalOptions configuration, CancellationToken cancellationToken, string action, params object[] args)
        {
            var request = CreateRequest(url, configuration, action, args);
            request.CancellationToken = cancellationToken;
            return request;
        }
    }
}
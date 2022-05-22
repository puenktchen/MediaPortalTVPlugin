define(['globalize', 'loading', 'appRouter', 'formHelper', 'emby-input', 'emby-button', 'emby-checkbox', 'emby-select'], function (globalize, loading, appRouter, formHelper) {
    'use strict';

    function onBackClick() {
        appRouter.back();
    }

    function getTunerHostConfiguration(id) {

        if (id) {
            return ApiClient.getTunerHostConfiguration(id);
        } else {
            return ApiClient.getDefaultTunerHostConfiguration('mediaportal');
        }
    }

    function reload(view, providerInfo) {

        getTunerHostConfiguration(providerInfo.Id).then(function (info) {
            fillTunerHostInfo(view, info);
        });
    }

    function fillTunerHostInfo(view, info) {

        var providerOptions = JSON.parse(info.ProviderOptions || '{}');

        view.querySelector('.txtDevicePath').value = info.Url || '';
        view.querySelector('.txtFriendlyName').value = info.FriendlyName || '';

        view.querySelector('.txtUsername').value = providerOptions.Username || '';
        view.querySelector('.txtPassword').value = providerOptions.Password || '';

        view.querySelector('#chkImportRadioChannels').checked = providerOptions.ImportRadioChannels || false;

        selectTranscoderProfiles(view, providerOptions);
        selectTvChannelGroups(view, providerOptions);
        selectRadioChannelGroups(view, providerOptions);

        loadGenres(view, providerOptions);
    }

    function selectTranscoderProfiles(view, providerOptions) {
        fetch(ApiClient.getUrl("MediaPortal/TranscoderProfiles"), {
            method: "GET",
        }).then((resp) => resp.json())
            .then(function (profiles) {
                view.querySelector('#selectTranscoderProfile').innerHTML = profiles.map(function (profile) {
                    var selectedText = profile.Name == providerOptions.TranscoderProfile ? " selected" : "";
                    return '<option value="' + profile.Name + '"' + selectedText + '>' + profile.Name + '</option>';
                });
            });
    }

    function selectTvChannelGroups(view, providerOptions) {
        fetch(ApiClient.getUrl("MediaPortal/TvChannelGroups"), {
            method: "GET",
        }).then((resp) => resp.json())
            .then(function (groups) {
                view.querySelector('#selectTvChannelGroup', view).innerHTML = groups.map(function (group) {
                    var selectedText = group.Id == providerOptions.TvChannelGroup ? " selected" : "";
                    return '<option value="' + group.Id + '"' + selectedText + '>' + group.GroupName + '</option>';
                });
            });
    }

    function selectRadioChannelGroups(view, providerOptions) {
        fetch(ApiClient.getUrl("MediaPortal/RadioChannelGroups"), {
            method: "GET",
        }).then((resp) => resp.json())
            .then(function (groups) {
                view.querySelector('#selectRadioChannelGroup', view).innerHTML = groups.map(function (group) {
                    var selectedText = group.Id == providerOptions.RadioChannelGroup ? " selected" : "";
                    return '<option value="' + group.Id + '"' + selectedText + '>' + group.GroupName + '</option>';
                });
            });

    }

    function loadGenres(view, providerOptions) {

        if (providerOptions.GenreMappings) {

            if (providerOptions.GenreMappings["GENREEDUCATIONAL"] != null) {
                view.querySelector('#txtEducationalGenre').value = providerOptions.GenreMappings["GENREEDUCATIONAL"].join('|');
            }
            if (providerOptions.GenreMappings["GENREKIDS"] != null) {
                view.querySelector('#txtKidsGenre').value = providerOptions.GenreMappings["GENREKIDS"].join('|');
            }
            if (providerOptions.GenreMappings["GENRELIVE"] != null) {
                view.querySelector('#txtLiveGenre').value = providerOptions.GenreMappings["GENRELIVE"].join('|');
            }
            if (providerOptions.GenreMappings["GENREMOVIE"] != null) {
                view.querySelector('#txtMovieGenre').value = providerOptions.GenreMappings["GENREMOVIE"].join('|');
            }
            if (providerOptions.GenreMappings["GENRENEWS"] != null) {
                view.querySelector('#txtNewsGenre').value = providerOptions.GenreMappings["GENRENEWS"].join('|');
            }
            if (providerOptions.GenreMappings["GENRESPORT"] != null) {
                view.querySelector('#txtSportsGenre').value = providerOptions.GenreMappings["GENRESPORT"].join('|');
            }
        }
    }

    function alertText(options) {

        require(['alert']).then(function (responses) {
            responses[0](options);
        });
    }

    return function (view, params) {

        view.addEventListener('viewshow', function () {

            reload(view, {
                Id: params.id
            });
        });

        view.querySelector('.btnCancel').addEventListener("click", onBackClick);

        function submitForm(page) {

            loading.show();

            getTunerHostConfiguration(params.id).then(function (info) {

                var providerOptions = JSON.parse(info.ProviderOptions || '{}');

                providerOptions.Username = view.querySelector('.txtUsername').value;
                providerOptions.Password = view.querySelector('.txtPassword').value;

                providerOptions.ImportRadioChannels = view.querySelector('#chkImportRadioChannels').checked;

                providerOptions.TranscoderProfile = view.querySelector('#selectTranscoderProfile').value;
                providerOptions.TvChannelGroup = view.querySelector('#selectTvChannelGroup').value;
                providerOptions.RadioChannelGroup = view.querySelector('#selectRadioChannelGroup').value;

                providerOptions.GenreMappings = {                    
                    "GENREEDUCATIONAL": view.querySelector('#txtEducationalGenre').value.split("|"),
                    "GENREKIDS": view.querySelector('#txtKidsGenre').value.split("|"),
                    "GENRELIVE": view.querySelector('#txtLiveGenre').value.split("|"),
                    "GENREMOVIE": view.querySelector('#txtMovieGenre').value.split("|"),
                    "GENRENEWS": view.querySelector('#txtNewsGenre').value.split("|"),
                    "GENRESPORT": view.querySelector('#txtSportsGenre').value.split("|")
                };

                info.FriendlyName = page.querySelector('.txtFriendlyName').value || null;
                info.Url = page.querySelector('.txtDevicePath').value || null;

                info.ProviderOptions = JSON.stringify(providerOptions);

                ApiClient.saveTunerHostConfiguration(info).then(function (result) {

                    formHelper.handleConfigurationSavedResponse();

                    appRouter.show(appRouter.getRouteUrl('LiveTVSetup', {
                        SavedTunerHostId: (result || {}).Id || info.Id,
                        IsNew: params.id == null
                    }));

                }, function () {
                    loading.hide();

                    alertText({
                        text: globalize.translate('ErrorSavingTvProvider')
                    });
                });
            });
        }

        view.querySelector('form').addEventListener('submit', function (e) {
            e.preventDefault();
            e.stopPropagation();
            submitForm(view);
            return false;
        });
    };
});
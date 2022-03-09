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

                info.FriendlyName = page.querySelector('.txtFriendlyName').value || null;
                info.Url = page.querySelector('.txtDevicePath').value || null;

                info.ProviderOptions = JSON.stringify(providerOptions);

                ApiClient.saveTunerHostConfiguration(info).then(function (result) {

                    formHelper.handleConfigurationSavedResponse();

                    if (params.id) {
                        appRouter.show(appRouter.getRouteUrl('LiveTVSetup'));
                    } else {
                        appRouter.show(appRouter.getRouteUrl('LiveTVSetup'));
                    }

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
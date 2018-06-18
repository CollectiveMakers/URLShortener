const apiRoot = "https://<azureafunctionname>.azurewebsites.net/api/UrlIngest?code=<FunctionAPIkey>";

var app = new Vue({

    el: '#app',

    data: {
        url: '',
        medium: '',
        urls: [],
        busy: false,
        alertText: null,
        showAlert: false
    },

    created: function () {
        this.showAlert = false;
        this.busy = false;
    },

    watch: {
        'busyCount': function () {
            this.busy = this.busyCount > 0;
        }
    },

    methods: {
        // get the group list 
        shorten: function () {
            var _this = this;
            this.busyCount++;
            $.ajax({
                type: 'POST',
                url: apiRoot,
                data: JSON.stringify({
                    medium: _this.medium,
                    input: encodeURI(_this.url)
                }),
                contentType: 'application/json'
            })
                .done(function (data) {
                    _this.urls = data;
                }).fail(function (err) {
                    _this.showAlert = true;
                    _this.alertText = err;
                }).always(function () { _this.busyCount--; });
        }
    }

});
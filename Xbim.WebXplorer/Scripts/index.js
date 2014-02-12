requirejs.config({
    paths: {
        "jquery": "jquery-2.0.2",
        "bootstrap": "bootstrap"
    },
    shim: {
        "bootstrap": {
            deps: ["jquery"]
        }
    }
});

define(['jquery', 'bootstrap'], function ($, bootstrap) {
});
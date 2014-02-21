define([],function(){
    "use strict";
    var eventmanager = Object.create(Object, {
        Callbacks: { value: [] },

        RegisterCallback: {
            value: function (type, callback, id) {
                var event;

                if (type === "") {
                    return false;
                }

                event = this.Callbacks[type];
                if (event === undefined) {
                    this.Callbacks[type] = [];
                    event = this.Callbacks[type];
                }

                if (callback instanceof Function) {
                    event.push({ "callback": callback, "id": id });
                    return true;
                }
                return false;
            }
        },
        UnregisterCallback: {
            value: function (type, id) {
                var event, i = 0;

                if (type === "") {
                    return false;
                }

                event = this.Callbacks[type];
                if (event === undefined) {
                    return false;
                }

                for (i = 0; i < event.length; i += 1) {
                    if (event[i] !== undefined &&
                        event[i].id !== undefined &&
                        event[i].id === id) {
                        event.splice(i, 1);
                        return true;
                    }
                }
                return false;
            }
        },
        FireEvent: {
            value: function (type, event) {
                var callback, i = 0;

                callback = this.Callbacks[type];

                if (callback === undefined) {
                    this.Callbacks[type] = [];
                    return;
                }

                for (i = 0; i < callback.length; i += 1) {
                    if (callback[i] !== undefined &&
                        callback[i].callback !== undefined &&
                        callback[i].callback instanceof Function) {
                        callback[i].callback(event);
                    }
                }
            }
        }
    });
    return eventmanager;
});
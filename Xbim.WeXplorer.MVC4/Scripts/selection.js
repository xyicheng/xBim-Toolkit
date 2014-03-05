define(['jquery', 'scenejs', 'observables'], function ($, scenejs, obs) {
    var selection = observables.getOrCreateCollection['selection'];
    var pubSub = $({});
    var result = {
        _selection: selection,
        getLength: function () {
            return this._selection.items.length;
        },
        add: function (item) {
            if (typeof item === 'object' && item.items !== 'undefined')
                this._selection.add(item);
            if (typeof item === 'number') {
                var element = observables.getOrCreate(item);
            }
            throw "Unexpected type of element to be added to selection";
        },
        clear: function () {
            this._selection.clear();
        },
        remove: function (item) {
            if (typeof item === 'object' && item.items !== 'undefined')
                observables.remove(item.id);
            if (typeof item === 'number') {
                observables.remove(item);
            }
            throw "Unexpected type of element to be added to selection";
        }
    };



    return result;
});
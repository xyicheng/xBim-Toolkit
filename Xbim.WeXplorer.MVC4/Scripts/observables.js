var observables = [];
observables.getOrCreate = function (id) {
    var result = this[id];
    if (result) {
        return result;
    } else {
        //constructor will automaticaly register itself to the observables[]
        return new Observable(id);
    }
}
observables.getOrCreateCollection = function (id) {
    var result = this[id];
    if (result) {
        return result;
    } else {
        //constructor will automaticaly register itself to the observables[]
        return new ObservableCollection(id);
    }
}



function DataBinder(object_id) {
    // Use a jQuery object as simple PubSub
    var pubSub = jQuery({});

    // We expect a `data` element specifying the binding
    // in the form: data-bind-<object_id>="<property_name>"
    var data_attr = "bind-" + object_id,
	message = object_id + ":change";

    // Listen to change events on elements with the data-binding attribute and proxy
    // them to the PubSub, so that the change is "broadcasted" to all connected objects
    jQuery(document).on("change", "[data-" + data_attr + "]", function (evt) {
        var $input = jQuery(this);

        if ($input.is("input, textarea, select")) {
            pubSub.trigger(message, [$input.data(data_attr), $input.val()]);
        }

    });

    // PubSub propagates changes to all bound elements, setting value of
    // input tags or HTML content of other tags
    pubSub.on(message, function (evt, prop_name, new_val) {
        jQuery("[data-" + data_attr + "=" + prop_name + "]").each(function () {
            var $bound = jQuery(this);

            if ($bound.is("input, textarea, select")) {
                $bound.val(new_val);
            } else {
                $bound.html(new_val);
            }
        });
    });

    return pubSub;
}

function Observable(uid) {
    var This = this;
    this.id = uid;
    this._binder = new DataBinder(uid),
	observables[uid] = this;

    this.attributes = {};
    this.set = function (attr_name, val) {
        this.attributes[attr_name] = val;
        this._binder.trigger(this.id + ":change", [attr_name, val, this]);
    };
    this.get = function (attr_name) {
        var isNumArray = function (array) {
            var checker = {}.toString;
            if (checker.call(array) !== "[object Array]") {
                return false;
            }
            for (var i in array) {
                if (typeof array[i] !== "number") {
                    return false;
                }
            }
            return true;
        };
        var result = this.attributes[attr_name];
        if (isNumArray(result)) {
            var resultSet = [];
            for (var i in result) {
                resultSet.push(observables.getOrCreate(result[i]))
            }
            return resultSet;
        }
        return result;
    };

    this.fillFromObject = function (json) {
        for (var attr in json) {
            this.set(attr, json[attr]);
        }
    };

    // Subscribe to the PubSub
    this._binder.on(uid + ":change", function (evt, attr_name, new_val, initiator) {
        if (initiator !== This) {
            This.set(attr_name, new_val);
        }
    });
}

function CollectionBinder(collection_id) {
    // Use a jQuery object as simple PubSub
    var pubSub = jQuery({});

    // We expect a `data` element specifying the binding
    // in the form: data-collection-bind-<collection_id>
    var data_attr = "collection-bind-" + collection_id,
	message = collection_id + ":change";

    // PubSub propagates changes to all bound elements, setting value of
    // input tags or HTML content of other tags
    pubSub.on(message, function (evt, bindableItems) {
        jQuery("[data-" + data_attr + "]").each(function () {
            var bound = jQuery(this);

            var renderCollection = function (boundElement, collection) {
                //get template
                var template = boundElement.attr('data-template');
                var boundTemplate = boundElement.clone().removeAttr('id').removeAttr(data_attr);

                //clear actual content
                boundElement.empty();

                //render template
                for (var i in collection) {
                    //create specific element
                    var element = $(template);

                    //fill in data
                    element.find('[data-bind]').andSelf().each(function () {
                        var item = $(this);
                        var attribute = item.attr('data-bind');
                        item.attr('data-bind-' + i, attribute);
                        var value = collection[i].get(attribute);

                        if (item.is("input, textarea, select")) {
                            item.val(value);
                        } else {
                            item.html(value);
                        }
                    });
                    boundElement.append(element);

                    //get children if the children attribute is specified. This must be recursive.
                    var childAttr = boundElement.attr('data-children');
                    if (childAttr) {
                        var children = collection[i][childAttr];
                        renderCollection(boundTemplate, children);
                        boundElement.append(boundTemplate);
                    }
                }
            };
            renderCollection(bound, bindableItems);

            
        });
    });

    return pubSub;
}

function ObservableCollection(uid) {
    this._binder = new CollectionBinder(uid);
    this.id = uid;
    observables[uid] = this;

    this.items = [];

    this.add = function (item) {
        if (typeof item !== 'object' || item.constructor.name !== 'Observable')
            throw 'Only observable objects can be inserted';
        this.items[item.id] = item;
        this._binder.trigger(this.id + ":change", [this.items]);
    };

    this.get = function (item_id) {
        return this.items[item_id];
    };

    this.remove = function (item_id) {
        this.items.splice(item_id, 1);
        this._binder.trigger(this.id + ":change", [this.items]);
    };

    this.clear = function () {
        this.items = [];
        this._binder.trigger(this.id + ":change", [this.items]);
    };

}
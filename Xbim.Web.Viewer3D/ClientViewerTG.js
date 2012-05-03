// ------------------------- TransformGraph

XbimTransformGraph = function () {
    this.Root = new XbimTransformNode();
}

XbimTransformGraph.prototype = {
    isAlive: function () {
        return 'yes, TransformGraph is alive.';
    },
    findNode: function (entityLabel) {
        return this.Root.findNode(entityLabel);
    },
    getAllNodes: function () {
        var allnodes = [];
        this.Root.getAllNodes(allnodes);
        return allnodes;
    }
}

// ------------------------- Transofrm node
// statuses: 0=just defined; 1=bbox; 2=meshavailable
XbimTransformNode = function () {
    this.Nodes = [];
    this.Type = "";
    this.EntityLabel = "";
    this.Status = 0;
    this.Wtransform = null;
    this.materialOverride = null;
}

XbimTransformNode.prototype = {
    isAlive: function () {
        return 'yes, TransformNode is alive.';
    },
    doesMatch: function (filter) {
        if (filter == "all")
            return true;
        if (this.Type == filter)
            return true;
        if (this.EntityLabel == filter)
            return true;
//        if ($.isArray(filter)) {
//            if (filter.indexOf(this.Type) != -1)
//                return true;
//            if (filter.indexOf(this.EntityLabel) != -1)
//                return true;
//        }
        return false;
    },
    onMatchingNodes: function (filter, func, param) {
        if (this.doesMatch(filter))
            func(this, param);
        for (var i = 0; i < this.Nodes.length; i++) {
            var v = this.Nodes[i].onMatchingNodes(filter, func, param);
        }
    },
    getAllNodes: function (allnodes) {
        allnodes.push(this);
        for (var i = 0; i < this.Nodes.length; i++) {
            this.Nodes[i].getAllNodes(allnodes);
        }
    },
    findNode: function (entityLabel) {
        if (this.EntityLabel == entityLabel)
            return this;
        for (var i = 0; i < this.Nodes.length; i++) {
            var v = this.Nodes[i].findNode(entityLabel);
            if (v != null)
                return v;
        }
        return null;
    },
    getNodeLabelsByType: function (types, state) {
        if (state == null)
            state = true;
        if (types instanceof String)
            types = new Array(types);

        var thisa = [];
        var thisc = [];
        if (this.Type != null) {
            if ((types.indexOf(this.Type) != -1) == state)
                thisa.push(this.EntityLabel);
        }
        for (var i = 0; i < this.Nodes.length; i++) {
            thisc = thisc.concat(this.Nodes[i].getNodeLabelsByType(types, state));
        }
        if (thisa.length > 0 && thisc.length > 0)
            return thisa.concat(thisc);
        if (thisa.length > 0)
            return thisa;
        return thisc;
    },
    NodesToList: function (pattern) {
        var usepattern = pattern;
        if (usepattern == undefined)
            usepattern = "{label}<br>{children}"

        while (usepattern.indexOf("{label}") > 0)
            usepattern = usepattern.replace("{label}", this.EntityLabel)
        while (usepattern.indexOf("{dq}") > 0)
            usepattern = usepattern.replace("{dq}", '"')
        while (usepattern.indexOf("{type}") > 0)
            usepattern = usepattern.replace("{type}", this.Type)

        var tmp = "";
        for (var i = 0; i < this.Nodes.length; i++) {
            tmp = tmp + this.Nodes[i].NodesToList(pattern);
        }
        usepattern = usepattern.replace("{children}", tmp)
        return usepattern;
    }
}
// ------------------------- Finalise

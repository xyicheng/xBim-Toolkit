// ------------------------- XbimBinaryParser

XbimBinaryParser = function () {
    this.Root = new XbimTransformNode();
}

XbimBinaryParser.prototype = {
    isAlive: function () {
        return 'yes, XbimBinaryParser is alive.';
    }
}
/*global define*/
(function (factory, global) {
    "use strict";
    if (typeof define === 'function' && define.amd) {
        // AMD. Register as an anonymous module.
        define([], factory);
    } else {
        // Browser globals
        global.IndexReader = factory();
    }
}
(
function (undefined) {
    "use strict";
    function IndexReader(MaxSize, DataStream) {
        //default sizes
        this.sizebyte = 1;
        this.sizeshort = 2;
        this.sizeint = 4;

        this._IndexReaderByteSize = this.IndexSize(MaxSize);
        this._br = DataStream;
    }
    IndexReader.prototype.Size = function () {
        return this._IndexReaderByteSize;
    },
    IndexReader.prototype.IndexSize = function (MaxSize) {
        if (MaxSize <= 255) //we will use byte for indices
            return this.sizebyte;
        else if (MaxSize <= 65535)
            return this.sizeshort; //use  unsigned short int for indices
        else
            return this.sizeint; //use unsigned int for indices   
    },
    IndexReader.prototype.ReadIndex = function () {
        var index;
        switch (this._IndexReaderByteSize) {
            case this.sizebyte:
                index = this._br.getUint8();
                break;
            case this.sizeshort:
                index = this._br.getUint16();
                break;
            default:
                index = this._br.getUint32();
                break;
        }
        return index;
    }
    return IndexReader;
}
, this
));
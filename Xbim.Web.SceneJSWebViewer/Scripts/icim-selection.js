//----------------------element selection object------------------------------------
function ElementSelection() {
    this._selectedElements = new Array();
    this._type = null; //ElementType
    this._data = new ElementTypeList();
    this._totalCarbon = null;

    this._changeIfcNameHandlers = new Array();
    this._changeHandlers = new Array();
}

ElementSelection.prototype.IsEmpty = function () {
    if (this._selectedElements.length == 0) return true;
}

ElementSelection.prototype.Clear = function () {
    this._selectedElements = new Array();
    this._type = null;
    this._onChange();
    this._onIfcTypeNameChange();
    Console.Write('Selection cleared.');
}

ElementSelection.prototype.IsInSelection = function (label) {
    return this._selectedElements.indexOf(label) != -1;
}

ElementSelection.prototype.IsTypeCompatible = function (label) {
    if (!this._data.GetByElementLabel(label)) return false; //no type for the object
    if (this._type == null) return true; //selection if free
    if (this._type.Contains(label)) return true; //is in type's collection of element
    return false;
}

ElementSelection.prototype.Add = function (labels) {
    if (!labels) {
        Console.Error("Argument 'label' and 'type' must be specified.");
        throw "Argument 'label' and 'type' must be specified.";
    }
    if (!this._data) throw "Data for the selection must be specified. Use Init() function first.";
    this._add = function (label) {
        if (this.IsEmpty()) {
            this._type = this._data.GetByElementLabel(label);
            if (!this._type) return false;
            this._onIfcTypeNameChange();
        }
        else {
            if (!this.IsTypeCompatible(label)) {
                return false;
            }
        }
        this._selectedElements.push(label);
        Console.WriteLine('Selected >> label:' + label + ', type label:' + this._type._label + ', type name:' + this._type._name + '(' + this._type._ifcType + ')');
    }
    if (typeof labels == 'object') {
        for (var i = 0; i < labels.length; i++) {
            this._add(labels[i]);
        }
    }
    else {
        this._add(labels);
    }

    this._onChange();
}

ElementSelection.prototype.Remove = function (labels) {
    if (!labels) return;
    if (this.IsEmpty()) return;
    this._remove = function (label) {
        var i = this._selectedElements.indexOf(label);
        if (i != -1) {
            this._selectedElements.splice(i, 1);
            Console.WriteLine('Deselected >> label:' + label + ', type label:' + this._type._label + ', type name:' + this._type._name + '(' + this._type._ifcType + ')');
            if (this._selectedElements.length == 0) {
                this._type = null;
                this._onIfcTypeNameChange();
            }
            this._onChange();
        }
    }

    if (typeof labels == 'object') {
        for (var i = 0; i < labels.length; i++) {
            this._remove(labels[i]);
        }
    }
    else {
        this._remove(labels);
    }
}

ElementSelection.prototype._onChange = function () {
    var typeName = "Nothing selected";
    var typeLabel = null;
    var maxCount = 0;
    var actualCount = 0;
    var type = this._type;
    var labels = new Array();
    if (!this.IsEmpty()) {
        typeName = this._type.GetName();
        typeLabel = this._type.GetLabel();
        maxCount = this._type.Count();
        actualCount = this._selectedElements.length;
        labels = this._selectedElements;
    }

    for (var i = 0; i < this._changeHandlers.length; i++) {
        if (this._changeHandlers[i])
            this._changeHandlers[i](typeName, typeLabel, maxCount, actualCount, labels, type);
    }
}

ElementSelection.prototype._onIfcTypeNameChange = function () {
    var ifcTypeName = null;
    if (this._type) {
        ifcTypeName = this._type.GetIfcType();
    }

    for (var i = 0; i < this._changeIfcNameHandlers.length; i++) {
        if (this._changeIfcNameHandlers[i]) {
            var typeElement = this._type;
            this._changeIfcNameHandlers[i](ifcTypeName, typeElement);
        }
    }
    Console.WriteLine('Type of selection changed to: ' + ifcTypeName);
}

ElementSelection.prototype.AddChangeHandler = function (fn) {
    this._changeHandlers.push(fn);
}

ElementSelection.prototype.AddIfcTypeNameChangeHandler = function (fn) {
    this._changeIfcNameHandlers.push(fn);
}

ElementSelection.prototype.Init = function () {
    if (!this.IsEmpty())
        this.Clear();

    var elemenTypeList = new ElementTypeList();
    var types = $('.ifc-element-type');
    for (var i = 0; i < types.length; i++) {
        var typeName = $(types[i]).find('input.typeName').val();
        var label = $(types[i]).find('input.entityLabel').val();
        var ifcTypeName = $(types[i]).find('input.ifcTypeName').val();
        var elemLabelsVal = $(types[i]).find('input.instanceEntityLabels').val()
        var elementLabels = null;
        if (elemLabelsVal) elementLabels = elemLabelsVal.split(';');

        var measuresVal = $(types[i]).find('input.instanceMeasures').val()
        var elementMeasures = null;
        if (measuresVal) elementMeasures = measuresVal.split(';');
        var carbon = $(types[i]).find('input.carbonMax').val()
        var elementType = new ElementType(typeName, label, ifcTypeName, carbon);

        if (elementLabels && elementMeasures)
            if (elementLabels.length != elementMeasures.length)
                elementMeasures == null; //rather nothing than inconsistent values
        elementType.AddElements(elementLabels, elementMeasures);
        elemenTypeList.Add(elementType);
    }

    this._data = elemenTypeList;
    this._totalCarbon = $('input#totalCarbonContentMax').val();
}

var Selection = new ElementSelection();

//---------------------------------ElementType object-----------------------
function ElementType(name, label, ifcTypeName, carbon) {
    this._name = name;
    this._label = label;
    this._ifcType = ifcTypeName;
    this._elements = new Array();
    this._measures = new Array();
    this._carbonContentMax = carbon;
}

ElementType.prototype.Contains = function (label) {
    return this._elements.indexOf(label) != -1;
}

ElementType.prototype.AddElement = function (label) {
    if (!label) return false;
    if (!this.Contains(label)) {
        this._elements.push(label);
        return true;
    }
    return false;
}

ElementType.prototype.AddElements = function (labels, measures) {
    if (measures)
        if (measures.length != labels.length) {
            Console.Error("Different number of elements than their measures. This would cause inconsistentency.");
            throw "Different number of elements than their measures. This would cause inconsistentency.";
        }
    if (!labels) return;
    if (typeof labels != 'object') return;
    for (var i = 0; i < labels.length; i++) {
        var inserted = this.AddElement(labels[i]);
        if (measures && inserted) this._measures.push(measures[i]);
    }


}

ElementType.prototype.Count = function () {
    return this._elements.length;
}

ElementType.prototype.GetName = function () {
    return this._name;
}

ElementType.prototype.GetLabel = function () {
    return this._label;
}

ElementType.prototype.GetIfcType = function () {
    return this._ifcType;
}

ElementType.prototype.GetMeasureForElement = function (label) {
    if (!this._measures) return null;
    if (this._measures.length == 0) return null;
    var index = this._elements.indexOf(label);
    if (index < 0) return null;
    if (index > this._measures.length - 1) return null;
    return new Number(this._measures[index]);
}

ElementType.prototype.GetTypeMeasure = function () {
    if (!this._measures) return null;
    if (this._measures.length == 0) return null;
    if (this._measures.length != this._elements.length) return null;
    var measure = 0;
    for (var i = 0; i < this._elements.length; i++) {
        measure += new Number(this._measures[i]);
    }
    return measure;
}
//--------------------------------ElementTypeList object -----------------------
function ElementTypeList() {
    this._data = new Array();
}

ElementTypeList.prototype.Contains = function (label) {
    for (var i = 0; i < this._data.length; i++) {
        if (this._data[i]._label == label) return true;
    }
    return false;
}

ElementTypeList.prototype.GetByTypeLabel = function (label) {
    for (var i = 0; i < this._data.length; i++) {
        if (this._data[i]._label == label) return this._data[i];
    }
    return null;
}

ElementTypeList.prototype.GetByElementLabel = function (label) {
    for (var i = 0; i < this._data.length; i++) {
        if (this._data[i].Contains(label)) return this._data[i];
    }
    return null;
}

ElementTypeList.prototype.Add = function (elementType) {
    if (!this.Contains(elementType._label))
        this._data.push(elementType);
}

ElementTypeList.prototype.GetAllElementsOfType = function (ifcElementTypeName) {
    var result = new Array();
    for (var i = 0; i < this._data.length; i++) {
        if (this._data[i]._ifcType == ifcElementTypeName) {
            result = result.concat(this._data[i]._elements)
        }
    }
    return result;
}
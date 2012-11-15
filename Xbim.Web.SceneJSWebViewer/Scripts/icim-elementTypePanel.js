function ElementTypePanel() {

}
ElementTypePanel.prototype = {
    _log: '',
    _selectedOldTypeLabel: null,

    Init: function () {
        try {
            if (Selection) {
                Selection.AddIfcTypeNameChangeHandler(function (ifcType) {
                    if (ifcType) {
                        var actual = $('#hiddenElementTypeName').val();
                        if (actual == ifcType) return;
                        $('#hiddenElementTypeName').val(ifcType);
                    }
                })
            }
            if (Selection) {
                var This = this;
                Selection.AddChangeHandler(function (typeName, typeLabel, maxCount, actualCount, labels) {
                    $('#changeElementLabels').val(labels);
                    if (labels.length == 0) This.ClearLog();
                    if (This._selectedOldTypeLabel != typeLabel) {
                        This._selectedOldTypeLabel = typeLabel
                        var args = 'typeChanged:' + typeLabel;
                        ElementTypePanelPostback(args);
                    }
                });
            }
        } catch (e) {
            Console.Write('Element type panel initialization was not successfull: ' + e);
        }

        try {
            //set existing selection
            var existSel = Selection._selectedElements;
            $('#changeElementLabels').val(existSel);
        } catch (e) {

        }


        //link to change element type using iCIM configurator
        var vals = $('#configuratorKeys').val();
        if (vals) {
            try {
                var index = $('.ddlTypesAvailable').prop('selectedIndex');
                if (!index) index = 0;
                vals = vals.split(';');
                var key = parseInt(vals[index]);
                var callBase = $('#btnNewType').attr('href');

                if (key != Number.NaN && callBase) {
                    var call = 'editselpk=' + key;
                    var index = callBase.indexOf('?');
                    callBase = callBase.slice(0, index + 1);
                    $('#btnEditType').attr('href', callBase + call);
                }
                else {
                    $('#btnEditType').attr('href', $('#btnNewType').attr('href'));
                }

            } catch (e) {
                Console.Error('It was not possible co create link for custom type editing: ' + e);
            }
        }

        try {
            $('#btnNewType, #btnEditType').fancybox({
                'width': 950,
                'height': '75%',
                'autoScale': false,
                'transitionIn': 'elastic',
                'transitionOut': 'none',
                'type': 'iframe',
                'overlayShow': true
            });
        } catch (e) {
            Console.Error('Fancybox is not available to show iCIM configurator');
        }

        //update size on init
        this.UpdateSize();

        //create links from text links in the properties
        $('.typeData').find('span').each(function () {
            var text = $(this).text();
            if (text.match(/http:\/\/.+/)) {
                $(this).html(text.link(text));
                $(this).children('a').each(function () {
                    $(this).attr('target', '_blank');
                });
            }
        });

        //update impact piechart. Timeout is necessary for some reason...
        var This = this;
        setTimeout(This.UpdatePieChart, 300);
    },
    ApplyNewType: function () {
        try {
            var elements = $('#changeElementLabels').val();
            if (elements.length == 0) {
                Message('No element is selected. Select elements to change first.');
                return;
            }
            var selectedNewType = $('.ddlTypesAvailable').val();
            if (selectedNewType == "-1") {
                Message('New type is not selected. Select new element type first.');
                return;
            }

            Console.Write('Elements: ' + elements);
            Console.Write('Changed to type: ' + $('.ddlTypesAvailable').val());
            ElementTypePanelPostback('applyTypeChange');
            this.ClearLog();
        } catch (e) {
            Console.Error('Error during application of the new type: ' + e);
        }

    },
    CloseIframeAndRefresh: function () {
        $('#fancybox-overlay').click();
        ElementTypePanelPostback('refresh');
    },
    UpdateSize: function () {
        var size = $('#btn-change-type').data('panelSize');
        if (size) {
            $('.typeData').width(size.width);
            $('.typeData').height(size.height);
            $('.typeSelectBoxes').width(size.width);
        }
    },
    UpdatePieChart: function () {
        try {
            var labels = Selection._selectedElements;
            var type = Selection._type;
            var change = function (oldVal, newVal) {
                if (oldVal == NaN || newVal == NaN) return;
                pieChartManager.SetOldValue(oldVal); // AddChangeOld(oldVal);
                pieChartManager.SetNewValue(newVal); // AddChangeNew(newVal);
                //
            }
            if (!Selection._type || !Selection._selectedElements) {
                change(0, 0);
                return;
            }
            if (labels.length == 0) {
                change(0, 0);
                return;
            }

            var projContent = Selection._totalCarbon;
            var typeContent = type._carbonContentMax;
            var typeMeasure = type.GetTypeMeasure();
            var newTypeUnitImpact = $('input#typeUnitCarbon').val();

            var elementsMeasure = 0;
            for (var i = 0; i < labels.length; i++) {
                elementsMeasure += type.GetMeasureForElement(labels[i]);
            }

            var elementsImpact = typeContent / typeMeasure * elementsMeasure;
            var newImpact = elementsMeasure * newTypeUnitImpact;

            var oldPercent = elementsImpact * 100.0 / projContent;
            var newPercent = newImpact * 100.0 / projContent;
            if (newPercent > oldPercent) newPercent = oldPercent - newPercent;
            change(oldPercent, newPercent);

            //write to log
            if (newImpact > 0) {
                var index = $('.ddlTypesAvailable').prop('selectedIndex');
                if (index > 0) {
                    var alterTypeName = $($('.ddlTypesAvailable option').get(index)).text();
                    elementTypePanel.WriteLog(elementsImpact, alterTypeName, newImpact - elementsImpact);
                }
                else {
                    elementTypePanel.ClearLog();
                }
            }
        } catch (e) {
            Console.Error("It was not possible to update pie chart values: " + e);
        }
    },
    WriteLog: function (actualContent, alternativeType, carbonImprovement) {
        if (!this._log) {
            $('#element-change-log').show();
            this._log = '';
        }

        //round to int
        actualContent = Math.round(actualContent);
        carbonImprovement = Math.round(carbonImprovement);

        //set colour of the improvement
        if (carbonImprovement <= 0)
            carbonImprovement = '<span style="color: green">' + carbonImprovement + ' kg</span>';
        else
            carbonImprovement = '<span style="color: red">+' + carbonImprovement + ' kg</span>';

        var message = '<tr class="type-log-data"> <td>' + actualContent + ' kg </td><td>' + alternativeType + '</td><td style="text-align:right;padding: 0 5 0 5;">' + carbonImprovement + '</td></tr>';
        this._log += message;
        $('#element-change-log-table').append(message);
    },
    ClearLog: function () {
        this._log = '';
        $('.type-log-data').remove();
        $('#element-change-log').hide();
    }
}

//create instance that is available for all the time
var elementTypePanel = new ElementTypePanel();
function GroupViewControl()
{ }

GroupViewControl.prototype = {
    _classificationOn: false,
    _emptyGroupsHidden: false,

    Init: function () {
        $('.GroupTree').treeview({
            persist: 'cookie',
            collapsed: true
        });
        $('#icim-view-by').tabs();
        if (!Selection) {
            Selection = new ElementSelection();
        }
        Selection.Init();
        var This = this;
        Selection.AddChangeHandler(function (typeName, typeLabel, maxCount, actualCount, labels) {
            This.markAllNotSelected();
            This.markSelected(typeLabel);
        });

        //assign jQuery UI style
        $('.ifc-element-type').find('.icim-data-row').addClass('ui-selectee');

        $('.ifc-element-type').click(function () {
            //get labels
            alert('start');
            var labels = $(this).find('input.instanceEntityLabels').val();
            alert(labels);
            var labelList = labels.split(';');
            if (labelList.length == 0) return;
            if (labelList[0] == '') return;

            //check type compatibility with actual selection. Clear selection if it is not.
            if (!Selection.IsTypeCompatible(labelList[0])) {
                Selection.Clear();
            }

            //if already selected than deselect it
            if ($(this).find('.icim-data-row').hasClass('ui-selected')) {
                //remove elements from the selection
                Selection.Remove(labelList);
            }
            //else add all elements to the selection
            else {
                //add elements to the selection
                success = Selection.Add(labelList);

            }
        });

        if (this._classificationOn)
            this.enableDragging();
        if (this._emptyGroupsHidden)
            this.hideEmptyGroups();
    },
    enableDragging: function () {
        $('.ifc-element-type').draggable({
            cursor: 'move',
            revert: 'invalid',
            containment: '#tabs-1'
        });
        $('.ifc-group').droppable({
            accept: '.ifc-element-type',
            greedy: true,
            drop: function (event, ui) {
                var elementLabel = ui.draggable.children('input.entityLabel').val();
                var sourceGroupLabel = ui.draggable.parent().parent().children('input.entityLabel').val()
                var targetGroupLabel = $(this).children('input.entityLabel').val();
                var args = elementLabel + ';' + sourceGroupLabel + ';' + targetGroupLabel;
                GroupViewControlPostBack(args);
            }
        });
        this._classificationOn = true;
    },
    disableDragging: function () {
        $('.ifc-element-type').draggable('destroy');
        $('.ifc-group').droppable('destroy');
        this._classificationOn = false;
    },
    refresh: function () {
        GroupViewControlPostBack('refresh');
    },
    reset: function () {
        GroupViewControlPostBack('reset');
    },
    markSelected: function (typeLabel) {
        $('.ifc-element-type').each(function (index, Element) {
            if ($(Element).find('input.entityLabel').val() == typeLabel) {
                $(Element).find('.icim-data-row').addClass('ui-selected');
            }
        });
    },
    markAllNotSelected: function () {
        $('.ui-selected').removeClass('ui-selected');
    },
    hideEmptyGroups: function () {
        $('.ifc-group').each(function () {
            var elements = $(this).find('.ifc-element-type');
            if (elements.length == 0)
                $(this).hide();
        });
        this._emptyGroupsHidden = true;
    },
    showAllGroups: function () {
        $('.ifc-group').show();
        this._emptyGroupsHidden = false;
    }
}

var groupView = new GroupViewControl();
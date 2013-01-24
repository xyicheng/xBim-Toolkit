$(document).ready(function () {
    //-------------------------element type change panel init---------------------
    $('#btn-change-type').toggle(
                function () {
                    elementTypePanel.UpdateSize();
                    $("#typePanel")
                    .slideDown("slow", function () {
                        $('#btn-change-type').text("Minimise ↓↓↓");
                    });
                    $('#typeSelector').resizable({
                        alsoResize: '.typeData',
                        handles: 'n, w, nw',
                        resize: function (event, ui) {
                            $('.typeSelectBoxes').width(ui.size.width);
                        },
                        stop: function () {
                            $('#btn-change-type').data('panelSize', {
                                width: $('.typeData').width(),
                                height: $('.typeData').height()
                            });
                            $('#typeSelector').css({
                                'height': '',
                                'position': '',
                                'top': '',
                                'left': ''
                            })
                        }
                    });
                },
                function () {
                    $(this).data('panelSize', {
                        width: $('.typeData').width(),
                        height: $('.typeData').height()
                    });
                    $('#typeSelector').resizable('destroy');
                    $("#typePanel").slideUp("slow", function () {
                        $('#btn-change-type').text("Change ↑↑↑");
                    });
                })
                .data('panelSize', {
                    width: $('.typeData').width(),
                    height: $('.typeData').height()
                });

    //---------------------------Browse by... panel init --------------------------
    $('#toggleBrowseBy').toggle(
                function () {
                    var widthExpand = $(this).data('widthExpand');
                    var heightExpand = $(this).data('heightExpand');
                    $(this).data('widthOrig', $('#browseBy').width());
                    $(this).data('heightOrig', $('#browseBy').height());

                    $('#browseBy').resizable({
                        minWidth: $('#browseBy').width(),
                        minHeight: $('#browseBy').height()
                    });
                    $('#browseBy').animate(
                        {
                            width: widthExpand,
                            height: heightExpand
                        },
                        'slow', function () {
                            $('#toggleBrowseBy').text('«');
                            $('#btnClassify').show();
                            $('#btnHideEmptyGroups').show();
                        });
                },
                function () {
                    $(this).data(
                        'widthExpand',
                        $('#browseBy').width()
                    ).data(
                        'heightExpand',
                        $('#browseBy').height()
                    );
                    var origWidth = $(this).data('widthOrig');
                    var origHeight = $(this).data('heightOrig');
                    $('#browseBy').animate(
                        {
                            width: origWidth,
                            height: origHeight
                        },
                        'slow', function () {
                            $('#toggleBrowseBy').text('»')
                            $('#browseBy').resizable('destroy')
                            $('#browseBy').removeAttr('style');
                        });
                    $('#btnClassify').hide();
                    $('#btnHideEmptyGroups').hide();
                }).data(
                    'widthOrig',
                    $('#browseBy').width()
                ).data(
                    'heightOrig',
                    $('#browseBy').height()
                ).data(
                    'widthExpand',
                    '550px'
                );

    /*******************     View by buttons *************************/
    $('#btnHideEmptyGroups').toggle(
    function () {
        groupView.hideEmptyGroups();
        $(this).text("Show all");
    },
    function () {
        groupView.showAllGroups();
        $(this).text("Hide empty");
    }
    );

    $('#btnClassify').toggle(
    function () {
        groupView.enableDragging();
        $(this).css("background-color", "#ccc");
    },
    function () {
        groupView.disableDragging();
        $(this).css("background-color", '');
    }
    );
    //---------------------------- Model revision panel animation ---------------------
    $('#toggleModelRevisions').toggle(
        function () {
            if (chartManager) {
                chartManager.RestoreSize();
                chartManager.Update(800, 30); //set chart with (width, baseHeight)
            }
            $(this).data('widthOrig', $('#modelRevisions').width());
            $(this).data('heightOrig', $('#modelRevisions').height());
            $('#modelRevisions').animate({
                width: 820,
                height: 350
            }, 'slow', function () {
                $('#toggleModelRevisions').text('«');
                $('#btnCollapsModelRevisions').hide();
            });
        },
        function () {
            var origW = $(this).data('widthOrig');
            var origH = $(this).data('heightOrig');
            $('#modelRevisions').animate({
                width: origW,
                height: origH
            }, 'slow', function () {
                var w = $('#revisionContent').width();
                var h = $('#revisionContent').height();
                $('#toggleModelRevisions').text('»');
                $('#btnCollapsModelRevisions').show();
                $('#modelRevisions').removeAttr('style');
                chartManager.ScaleChartTo(w, h);
                chartManager.Update(w - 10, Math.round(h / 8));
            });

        });
    $('#btnCollapsModelRevisions').toggle(
    function () {
        var h = $('#modelRevisions').height() - $('#revisionContent').height();
        $('#revisionContent').fadeOut('slow');
        $('#modelRevisions').animate({
            height: h
        },
          'slow', function () {
              $('#btnCollapsModelRevisions').text('↑');
              $('#toggleModelRevisions').hide();
          });
    },
    function () {
        $('#modelRevisions').animate({
            height: '30%'
        },
         'slow', function () {
             $('#revisionContent').fadeIn('slow');
             $('#toggleModelRevisions').show();
             $('#btnCollapsModelRevisions').text('↓');
             $('#modelRevisions').css('height', '');
         });
    });

    //------------------------- 3D viewer init ------------------------------
    try {
        if (!Selection) throw "Selection object not available. 3D Viewer cannot be connected to the selection logic.";
        else {
            Selection.AddChangeHandler(function (typeName, typeLabel, maxCount, actualCount, labels, type) {
                try {
                    if (!labels) {
                        _Viewer.select([]);
                    }
                    else {
                        var selectedElements = new Array();
                        for (var i = 0; i < labels.length; i++) {
                            selectedElements.push(labels[i]);
                        }
                        _Viewer.select(selectedElements);
                    }
                } catch (e) {
                    Console.Error("It was not possible to show selected elements in the viewer: " + e);
                }

            });
            Selection.AddIfcTypeNameChangeHandler(function (ifcTypeName, typeElement) {
                try {
                    if (!typeElement) {
                        //set all materials to normal
                        _Viewer.setNodeOverrideMaterial('all', null);
                    }
                    else if (!typeElement._elements) {
                        //set all materials to normal
                        _Viewer.setNodeOverrideMaterial('all', null);
                    }
                    else {
                        //get list of selectable elements
                        var elements = typeElement._elements;

                        //set everything semi-transparent
                        _Viewer.setNodeOverrideMaterial('all', windowMaterial);

                        //set selectable elements normal
                        for (var i = 0; i < elements.length; i++) {
                            _Viewer.setNodeOverrideMaterial(elements[i], null);
                        }
                    }
                } catch (e) {
                    Console.Error("It was not possible to show available elements of the selected type in the viewer: " + e);
                }

            });
        }
    } catch (e) {
        Console.Error("It was not possible to add any handlers to the 'Selection' object: " + e);
    }


    //callbacks for selection and clicking on elements (if PICKER tool is activated)
    $("#o3d")
            .bind("selectValidate", function (event, data) {
                if (navigation._mode != mode.SELECTION) return false;
                return Selection.IsTypeCompatible(data);
            })
            .bind("selectElementClicked", function (event, data) { })
            .bind("selectionRemoved", function (event, data) {
                if (navigation.activeButton == buttons.SELECT_BY_TYPE) {
                    Selection.Clear();
                }
                else {
                    Selection.Remove(data);
                }
            })
            .bind("selectionAdded", function (event, data) {
                if (navigation.activeButton == buttons.SELECT_BY_TYPE) {
                    var type = Selection._data.GetByElementLabel(data);
                    if (!type) return;
                    var elements = type._elements;
                    Selection.Add(elements);
                }
                else {
                    Selection.Add(data);
                }
            })
            .bind("selectionChanged", function (event, data) { })
            .bind('elementClicked', function (event, data) {
                if (navigation._mode == mode.VISIBILITY) {
                    var label = data;
                    switch (navigation.activeButton) {
                        case buttons.HIDE_ONE:
                            _Viewer.setNodeVisibility(data, false)
                            break;
                        case buttons.HIDE_SIMILAR:
                            var type = Selection._data.GetByElementLabel(data);
                            if (!type) return;
                            var elements = Selection._data.GetAllElementsOfType(type._ifcType);
                            for (var i = 0; i < elements.length; i++) {
                                _Viewer.setNodeVisibility(elements[i], false)
                            }
                            break;
                        case buttons.HIDE_TYPE:
                            var type = Selection._data.GetByElementLabel(data);
                            if (!type) return;
                            var labels = type._elements;
                            if (!labels) return;
                            for (var i = 0; i < labels.length; i++) {
                                _Viewer.setNodeVisibility(labels[i], false)
                            }
                            break;
                        default:

                    }
                }
            });

    $("#o3d").bind('modelLoaded', function () {
        $('#ModelLoadingOverlay').hide(300);
    });

    //disable text selection when navigating in the model
    $("#o3d").bind('panStart', function () { $('body').addClass('disabled-text-selection'); });
    $("#o3d").bind('orbitStart', function () { $('body').addClass('disabled-text-selection'); });
    $("#o3d").bind('panFinish', function () { $('body').removeClass('disabled-text-selection'); });
    $("#o3d").bind('orbitFinish', function () { $('body').removeClass('disabled-text-selection'); });

    //---------------------------------------------------------------------
    /**************Chart manager init to fit into the box************/
    try {
        if (chartManager) {
            var origW = $('#revisionContent').width();
            var origH = $('#revisionContent').height();
            chartManager.ScaleChartTo(origW - 20, origH - 40);
        }
    } catch (e) {
        Console.Error("It was not possible to initialize chartManager for linear DECogram. " + e);
    }


    /************* Hide element type panel as a start up state ***********/
    $("#typePanel").hide();


    /********************** Viewer navigation init *********************/
    try {
        navigation.hideButtons();
        navigation._hidingButtons = true;

        $('#viewerNavigation').draggable({
            containment: 'parent',
            stop: function () {
                $(this).css('right', 'auto');
            }
        });

        var currentTool = TOOL_ORBIT;
        navigation.addButtonClickHandler(function (event, button) {
            try {
                switch (button) {
                    case buttons.CLEAR_SELECTION:
                        Selection.Clear();
                        break;
                    case buttons.EXTENT:
                        ZoomExtents();
                        break;
                    case buttons.HIDE_ONE:
                        break;
                    case buttons.HIDE_SIMILAR:
                        break;
                    case buttons.HIDE_TYPE:
                        break;
                    case buttons.ORBITA:
                        SetTool(TOOL_ORBIT);
                        currentTool = TOOL_ORBIT;
                        break;
                    case buttons.PAN:
                        SetTool(TOOL_PAN);
                        currentTool = TOOL_PAN;
                        break;
                    case buttons.SELECT_BY_TYPE:
                        break;
                    case buttons.SELECT_ONE:
                        break;
                    case buttons.SHOW_ALL:
                        _Viewer.setNodeVisibility('all', true);
                        break;
                    case buttons.ZOOM_MINUS:
                        SetTool(TOOL_ZOOM);
                        zoom(1.5);
                        SetTool(currentTool);
                        break;
                    case buttons.ZOOM_PLUS:
                        SetTool(TOOL_ZOOM);
                        zoom(0.7);
                        SetTool(currentTool);
                        break;
                    case buttons.MODE_NAVIGATION:
                        SetTool(TOOL_ORBIT);
                        currentTool = TOOL_ORBIT;
                        break;
                    case buttons.MODE_SELECTION:
                        SetTool(TOOL_PICKER);
                        _Viewer.setSelectionMode('toggle');
                        currentTool = TOOL_PICKER;
                        break;
                    case buttons.MODE_VISIBILITY:
                        SetTool(TOOL_PICKER);
                        currentTool = TOOL_PICKER;
                        navigation.activeButton = buttons.HIDE_ONE;
                        break;
                    case buttons.MODE_CENTRAL_BUTTON:
                        break;
                    default:
                        break;
                }
            } catch (e) {
                Console.Error('It was not possible to set the navigation tool: ' + e);
            }

        });
        $(window).bind('resize', function () {
            try {
                ZoomExtents();
            } catch (e) {
                Console.Error("It was not possible to zoom extent when window was resized: " + e);
            }
        });
    } catch (e) {
        Console.Error("It was not possible to bind any functionality to the navigation control:" + e);
    }

    /************************************ Selection handler to set impact pie chart ************************************/
    Selection.AddChangeHandler(function (typeName, typeLabel, maxCount, actualCount, labels, type) {
        elementTypePanel.UpdatePieChart();
    });

    /********************* Window unload handling *************************/
    needToConfirm = false;
    window.onbeforeunload = function () {
        if (needToConfirm) {
            return "You have unsaved changes.";
        }
    }

});



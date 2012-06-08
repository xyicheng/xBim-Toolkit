$(document).ready(function () {
    makeMenuDraggableResizeable();
    $("#types").height(250); $("#types").width(300);
    $("#properties").height(250); $("#properties").width(300);
    $("#modelmenu").height(250); $("#modelmenu").width(300);

    $("#properties").hide();
    $("#types").hide();

    $("#quickProperties").hide();

    $("#propertiesAccordion").accordion({ autoHeight: false, clearStyle: true });

    $("#types").bind("resizestart", function (event, ui) {
        $("#types").stop();
    });

});

function makeMenuDraggableResizeable() {
    $('#types').draggable({ containment: '#scenejsCanvas', handle: '.menuHeader', opacity: '0.5' }).resizable({ minWidth: 250 });
    $('#properties').draggable({ containment: '#scenejsCanvas', handle: '.menuHeader', opacity: '0.5' }).resizable({ minWidth: 250 });
    $('#modelmenu').draggable({ containment: '#scenejsCanvas', handle: '.menuHeader', opacity: '0.5' }).resizable({ minWidth: 200 });
}

function checkBounds(div) {

    var myBox = $(div),
    bHeight = myBox.height(),
    wHeight = $(window).height(),
    bWidth = myBox.width(),
    wWidth = $(window).width();

    if (myBox.offset().top + bHeight >= wHeight) {

        var moveTop = wHeight - bHeight - myBox.offset().top
        if (moveTop <= 0) { moveTop = myBox.offset().top + moveTop; } else { myBox.offset().top - moveTop; }

        myBox.css({
            top: moveTop - 5
        });
    }

    if (myBox.offset().left + bWidth >= wWidth) {

        var moveleft = wWidth - bWidth - myBox.offset().left
        if (moveleft <= 0) { moveleft = myBox.offset().left + moveleft; } else { myBox.offset().left - moveleft; }

        myBox.css({
            left: moveleft - 5
        });
    }
}
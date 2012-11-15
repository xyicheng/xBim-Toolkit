//helper function to simulate mouse action of mouse over -> click -> mouseout
var launchEventSequence = function (id) {
    var element = document.getElementById(id);
    var mouseover = document.createEvent("MouseEvents");
    mouseover.initMouseEvent("mouseover", true, true, window, 0, 0, 0, 0, 0, false, false, false, false, 0, null);
    var click = document.createEvent("MouseEvents");
    click.initMouseEvent("click", true, true, window, 0, 0, 0, 0, 0, false, false, false, false, 0, null);
    var mouseout = document.createEvent("MouseEvents");
    mouseout.initMouseEvent("mouseout", true, true, window, 0, 0, 0, 0, 0, false, false, false, false, 0, null);
    
    element.dispatchEvent(mouseover);
    element.dispatchEvent(click);
    element.dispatchEvent(mouseout);
}


/************* Show Console shortcut ****************/
$.Shortcuts.add({
    type: 'down',
    mask: 'Ctrl+Shift+C',
    handler: function () {
        if (Console.visible)
            Console.Hide();
        else
            Console.Show();
    }
}).start();

/**************** Change mode to navigation ******************/
$.Shortcuts.add({
    type: 'down',
    mask: 'N', 
    handler: function () {
        launchEventSequence(buttons.MODE_NAVIGATION);
    }
}).start();

/**************** Change mode to selection ******************/
$.Shortcuts.add({
    type: 'down',
    mask: 'S',
    handler: function () {
        launchEventSequence(buttons.MODE_SELECTION);
    }
}).start();

/**************** Change mode to navigation ******************/
$.Shortcuts.add({
    type: 'down',
    mask: 'V',
    handler: function () {
        launchEventSequence(buttons.MODE_VISIBILITY);
    }
}).start();

/**************** Visibility all ******************/
$.Shortcuts.add({
    type: 'down',
    mask: 'A',
    handler: function () {
        launchEventSequence(buttons.SHOW_ALL);
    }
}).start();

/**************** Clear selection ******************/
$.Shortcuts.add({
    type: 'down',
    mask: 'C',
    handler: function () {
        launchEventSequence(buttons.CLEAR_SELECTION);
    }
}).start();

/**************** Zoom in ******************/
$.Shortcuts.add({
    type: 'hold',
    mask: 'I',
    handler: function () {
        launchEventSequence(buttons.ZOOM_PLUS);
    }
}).start();

/**************** Zoom out ******************/
$.Shortcuts.add({
    type: 'hold',
    mask: 'O',
    handler: function () {
        launchEventSequence(buttons.ZOOM_MINUS);
    }
}).start();

/**************** Full extent ******************/
$.Shortcuts.add({
    type: 'hold',
    mask: 'F+E',
    handler: function () {
        launchEventSequence(buttons.EXTENT);
    }
}).start();


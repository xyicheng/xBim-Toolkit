//This script will initialize web viewer user interface based on jquery UI and plugins
define (['jquery-ui-1.10.4.min', 'jstree', 'ace'], function (jq, jst, aceditor) {
    $(document).ready(function () {
        $('#menu').menu();
        $('#left-col').accordion({ heightStyle: "fill" });
        $('#left-col-wrapper').resizable({ handles: "e" });
        $('#spatial-tree').jstree();

        //**************** script editor **********************
        var editor = ace.edit('script-editor');
        editor.setTheme('ace/theme/eclipse');
        editor.getSession().setMode('ace/mode/bql')
        editor.setValue("SELECT EVERY wall WHERE name CONTAINS 'Brick';")

        $('#script-editor-buttons span').button();
        $('#btnClearScript').click(function () {
            editor.setValue('');
        });
        $('#btnUndoScriptEdit').click(function () {
            var undoManager = editor.getSession().getUndoManager();
            if (undoManager.hasUndo())
                undoManager.undo(false);
        });
        $('#btnRedoScriptEdit').click(function () {
            var redoManager = editor.getSession().getUndoManager();
            if (redoManager.hasRedo())
                redoManager.redo(false);
        });
        $('#btnExecuteScript').click(function () {
            var script = editor.getValue();
            var parser = new XbimProxy.BQLScript();
            parser.Execute(ModelID, script, function (result) {
                var resultWin = $('#script-results');
                //report errors if there are any
                if (result.Errors.length != 0)
                {
                    for (var i = 0; i < result.Errors.length; i++) {
                        var errMsg = result.Errors[i];
                        var msg = '<p style="color: red;">' + errMsg + '</p>';
                        resultWin.append(msg);
                    }
                }

                //download file if there is one created
                if (result.File)
                {
                    $('#download-frame').attr('src', result.File);
                }

                //add output to the result window
                if (result.Message)
                {
                    var lines = result.Message.trim().split('\n');
                    for (var i = 0; i < lines.length; i++) {
                        var line = lines[i];
                        var msg = '<p>' + line + '</p>';
                        resultWin.append(msg);
                    }
                }

                if (result.LatestResults.length != 0)
                {
                    var msg = '';
                    for (var i in result.LatestResults) {
                        var res = result.LatestResults[i];
                        msg += '#' + res + ', ';
                    }
                    msg = msg.substring(0, msg.length - 2);
                    resultWin.append('<p>' + msg + '</p>');
                }

                $('#script-results').animate({ scrollTop: $(this).height() });
            });
        });
        
    });
}
);
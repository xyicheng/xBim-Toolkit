//------------------------ Console log -----------------------------------------
function _Console() {
    this._ready = false;
    try {
        $('body').append("<div id='app-log' style='position:fixed; display:none; width:800px; height:400px; overflow:auto; z-index:99; background:white;color:black; border:1px black solid; font-family: courier; font-size:12;'></div>");
        $('body').append("<div id='app-log-btn-close' style='position:fixed; top:2; left:770px; font-weight:bold; font-size:15px; color:red; display:none; z-index:100; cursor:pointer'>X</div>")
        this._log = $('#app-log');
        this._btn = $('#app-log-btn-close');
        var This = this;
        this._btn.click(function () {
            This.Hide();
        })
        if (this._log.get(0))
            this._ready = true;
    } catch (e) {
        this._ready = false;
    }
}
_Console.prototype = {
    Write: function (message) {
        if (!this._ready) return;
        message = this._addTime(message);
        this._log.append(message + '<br />');
        this._scroll();
    },

    WriteLine: function (message) {
        if (!this._ready) return;
        message = this._addTime(message);
        this._log.append(message + '<br />');
        this._scroll();
    },

    Error: function (message) {
        if (!this._ready) return;
        var wrap = "<span style='color:red'>" + message + '</span><br />';
        wrap = this._addTime(wrap);
        this._log.append(wrap);
        this._scroll();
    },

    Show: function () {
        if (!this._ready) return;
        this._log.show(100);
        this._btn.show(100);
        this.visible = true;
        this._log.get(0).scrollTop += 50000;
    },

    Hide: function () {
        if (!this._ready) return;
        this._log.hide(100);
        this._btn.hide(100);
        this.visible = false;
    },

    _addTime: function (message) {
        var a = new Date();
        return '<span style="color:#4ABA4A">' + a.getHours() + ":" + a.getMinutes() + ":" + a.getSeconds() + "#&nbsp;</span>" + message;
    },
    _scroll: function () {
        this._log.get(0).scrollTop += 500;
    },
    visible: false
}
$(document).ready(function () {
//reinit of console when DOM is ready to go
    Console = new _Console();
});


/*****************    Message box  *************************/
function Message(message, time) {
    $('#MsgBox').html(message).dialog({
        modal: true,
        title: "Message",
        buttons: [
        {
            text: "Close",
            click: function () {
                $(this).dialog("close");
                $(this).dialog("destroy");
            }
        }
        ]
    });
    Console.WriteLine(message);
    if (time) {
        setTimeout(function () {
            $('#MsgBox').dialog("close");
            $('#MsgBox').dialog("destroy");
        }, time);
    }
}

function Confirm(message, fnYes, fnNo) {
    $('#MsgBox').html(message).dialog({
        modal: true,
        title: "Confirmation",
        buttons: [
        {
            text: "Yes",
            click: function () {
                $(this).dialog("close");
                $(this).dialog("destroy");
                if (fnYes) fnYes();
            }
        },
        {
            text: "No",
            click: function () {
                $(this).dialog("close");
                $(this).dialog("destroy");
                if (fnNo) fnNo();
            }
        }
        ]
    });
    Console.WriteLine(message);
}


//declare console so that it is available all the time
var Console = new _Console();
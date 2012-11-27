<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="model04.aspx.cs" Inherits="Xbim.SceneJSWebViewer.model04" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <link href="Styles/ui.dynatree.css" rel="stylesheet" type="text/css" />
     <link href="Content/themes/base/minified/jquery-ui.min.css" rel="stylesheet" type="text/css" />
     <script type="text/javascript" src="Scripts/jquery-1.7.2.min.js"></script>
     <script type="text/javascript" src="Scripts/jquery-ui-1.8.20.min.js" ></script>
     <script type="text/javascript" src="Scripts/jquery.dynatree.min.js" ></script>
     <script type="text/javascript" src="Scripts/jquery.hotkeys.js" ></script>
     <script type="text/javascript" src="Scripts/jquery.signalR-0.5.0.min.js" ></script>
     <script type="text/javascript" src="Scripts/jquery.viewport.mini.js" ></script>
     <script type="text/javascript" src="Scripts/scenejs.js"></script>
     <script type="text/javascript" src="Scripts/basescenedefinition.js"></script>
     <script type="text/javascript" src="Scripts/camera.js" ></script>
     <script type="text/javascript" src="Scripts/jdataview.js" ></script>
     <script type="text/javascript" src="Scripts/key_status.js" ></script>
     <script type="text/javascript" src="Scripts/orbit.js" ></script>
     <script type="text/javascript" src="Scripts/quaternion.js" ></script>
     <script type="text/javascript" src="Scripts/viewer-mesh.js" ></script>
     <script type="text/javascript" src="Scripts/modelbuilder.js" ></script>
     <script type="text/javascript" src="Scripts/modelstreamer.js" ></script>
     <script type="text/javascript" src="Scripts/menus.js" ></script>
     <script type="text/javascript" src="Scripts/viewer.js" ></script>

     <script type="text/javascript" src="Scripts/model04.js" ></script>
</head>
<body>
    <canvas id="scenejsCanvas">
        <p>This example requires a browser that supports the <a href="http://www.w3.org/html/wg/html5/">HTML5</a> &lt;canvas&gt; feature.</p>
    </canvas>

    <div style="display:none;">
        <div id="types" class="dragmenu unselectable">
            <div class="menuContent">
                <div id="navTreeContainer">
                    <div id="navtree"></div>
                </div>
            </div>
        </div>
    </div>
</body>
</html>

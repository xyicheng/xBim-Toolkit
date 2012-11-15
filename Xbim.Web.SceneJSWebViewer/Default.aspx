<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true"
    CodeBehind="Default.aspx.cs" Inherits="Xbim.SceneJSWebViewer._Default" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
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

 <script type="text/javascript" src="Scripts/ajaxfileupload.js"></script>

 <script src="Scripts/icim-console.js" type="text/javascript"></script>
 <script src="Scripts/icim-elementTypePanel.js" type="text/javascript"></script>
 <script src="Scripts/icim-groupViewControl.js" type="text/javascript"></script>
 <script src="Scripts/icim-selection.js" type="text/javascript"></script>
 <script src="Scripts/icim-shortcuts.js" type="text/javascript"></script>
 <script src="Scripts/icim-app-init.js" type="text/javascript"></script>
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
    <div id="loadingScreen" style="display:none; background-repeat:no-repeat; background-image: url('Styles/modelloading.gif');  background-position:center center; text-align:center; z-index:10000000; background-color:Gray; position:absolute;">
        <p style="color:White;">Loading Geometry Manifest. Depending on file size, this may take a while.</p>
    </div>
    <canvas id="scenejsCanvas">
        <p>This example requires a browser that supports the <a href="http://www.w3.org/html/wg/html5/">HTML5</a> &lt;canvas&gt; feature.</p>
    </canvas>
    <div id="loadedWrapper">
        <div id="loaded"></div>
        <div id="loadedbar"></div>
    </div>

    

    <div id="modelmenu" class="dragmenu unselectable">
        <span class="resetBoundary"></span>
        <div class="menuHeader">
            Model Menu
        </div>
        <div id="menu" runat="server">
        </div>
    </div>
    <div id="types" class="dragmenu unselectable">
        <span class="resetBoundary"></span>
        <div class="menuHeader">
            Types
        </div>

        <div class="menuContent">
            <p><a href="#" onclick="ZoomExtents();">Zoom Extents</a></p>
            <div id="navTreeContainer">
                <div id="navtree"></div>
            </div>
        </div>
    </div>

    <div id="classification" class="dragmenu unselectable ui-draggable ui-resizable" style="height: 250px; width: 300px; display: block; opacity: 1; left: 0px;">
        <span class="resetBoundary"></span>
        <div class="menuHeader">
            Classification
        </div>

        <div class="menuContent">
            <div id="navTreeContainerClassification">
                <div id="navtreeClassification"></div>
            </div>
        </div>
    </div>

    <div id="uploadCtl" class="dragmenu unselectable">
        <span class="resetBoundary"></span>
        <div id="uploadCtlInner">
            <div class="menuHeader">
                <span>File</span>
                <input type="file" id="file" name="file" size="10"/>
                <button id="buttonUpload" onclick="return ajaxFileUpload();">Upload</button>
                <img id="loading" src="Styles/loading.gif" style="display:none;" alt="" />
            </div>
        </div>
        <a id="linkLoadAnotherFile" href="Default.aspx" style="display:none;">Load another file</a>
    </div>

    <div id="properties" class="dragmenu unselectable">
        <span class="resetBoundary"></span>
        <div class="menuHeader">
            Properties
        </div>
        <div id="propertiesContent" class="menuContent">
            <div id="propertiesAccordion">
                <h3><a href="#">Section 1</a></h3>
                <div>
                    <p>Section 1 Content Goes Here</p>
                </div> 

                <h3><a href="#">Section 2</a></h3>
                <div>
                    <p>Section 2 Content Goes Here</p>
                </div> 

                <h3><a href="#">Section 3</a></h3>
                <div>
                    <p>Section 3 Content Goes Here</p>
                </div> 

                <h3><a href="#">Section 4</a></h3>
                <div>
                    <p>Section 4 Content Goes Here</p>
                </div> 
            </div>
        </div>
    </div>

    <div id="quickProperties">
        <p>Loading...</p>
    </div>

    <div id="debuginfo" style="display:none; width:100%; height: 200px;"><h1>Debug Info:</h1></div>

    

</asp:Content>



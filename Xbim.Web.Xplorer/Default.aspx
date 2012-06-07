<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="Xbim.Web.Xplorer._Default" %>
<%@ Register assembly="Xbim.Web.Viewer3D" namespace="Xbim.Web.Viewer3D" tagprefix="v3D" %>
<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
<script type="text/javascript">
    function OnElementPicked(event, data) {
        alert(data);
    }
    function RegisterSelectionHandlers() {
        $("#o3d")
            .bind("selectValidate", TestObjectSelection)
            .bind("selectElementClicked", genericSelectionEvent)
            .bind("selectionRemoved", genericSelectionEvent)
            .bind("selectionAdded", genericSelectionEvent)
            .bind("selectionChanged", genericSelectionEvent);
        }
    function genericSelectionEvent(event, data) {
        $("#SemanticTreePanel").append('<B>' + event.type + ':</B>' + data + '<br />')
    }
    function UnRegisterSelectionHandlers() {
        $("#o3d")
            .unbind("selectValidate")
            .unbind("selectElementClicked")
            .unbind("selectionRemoved")
            .unbind("selectionAdded")
            .unbind("selectionChanged");
    }
    function TestObjectSelection(event, data) {
        var node = _Viewer.transformGraph.Root.findNode(data);
        if (node != null) {
            if (node.Type.indexOf('IfcWall') != -1)
                return true
            else
                return false;
        }
        return false;
    } 
</script>
</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
<asp:ScriptManager ID="ScriptManager1" runat="server">
    </asp:ScriptManager>
    
    <div id="Viewer3DPanel">
        <v3D:ServerViewer3DControl ID="o3d" runat="server" 
            InitialisationMode="GetWholeMesh" />
    </div>

    <div id="Instructiondiv">
    <h2>Basics</h2>
    <p>The following commands allow you to interact with the model.</p>
    <ol>
    <li><a href="#" onclick="_Viewer.tgStruct('SemanticTreePanel','<a href=# onclick={dq}_Viewer.select({label});{dq}>{label}</a>  {type}<br>{children}');">SelectElements SelectInviewer</a><br /></li>
    <li><a href="#" onclick="_Viewer.tgStruct('SemanticTreePanel','<a href=# onclick={dq}zoomToElement({label});{dq}>{label}</a>  {type}<br>{children}');">ZoomToElement</a><br /></li>

    
    <li>Tools: 
        <a href="#" onclick="ZoomExtents();">Zoom Extents</a>, 
        <a href="#" onclick="SetTool(TOOL_ORBIT);">Orbit</a>, 
        <a href="#" onclick="SetTool(TOOL_PICKER);">Pick</a>, 
        <a href="#" onclick="SetTool(TOOL_PAN);">Pan</a> <br />
    </li>
    <li>Selection Mode: 
        <a href="#" onclick="_Viewer.setSelectionMode('replace');">Replace</a>, 
        <a href="#" onclick="_Viewer.setSelectionMode('toggle');">Toggle</a> 
    </li>
    <li>Walls: 
        <a href="#" onclick="_Viewer.setNodeVisibility('IfcWallStandardCase', false);">Hide</a>, 
        <a href="#" onclick="_Viewer.setNodeVisibility('IfcWallStandardCase', true);">Show</a> 
        <a href="#" onclick="_Viewer.setNodeOverrideMaterial('IfcWallStandardCase', windowMaterial);">SemiTransparent</a>, 
        <a href="#" onclick="_Viewer.setNodeOverrideMaterial('IfcWallStandardCase', null);">Normal</a> 
    </li>
    </ol>
    <h2>Loading control</h2>
    <p>
    To see how to control the loading process of the component set the property 'InitialisationMode' of the 3D viewer to 'InitO3dEngine', then try launching the following commands 
        <b>in sequence</b>.
    </p>
    <ol>
    <li><a href="#" onclick="_Viewer.downloadTG();">Load TransformGraph in memory</a><br /></li>
    <li><a href="#" onclick="_Viewer.loadLabelMesh(_Viewer.reportWalls());">Load Walls</a><br /></li>
    <li><a href="#" onclick="_Viewer.downloadWholeModel();">Load Whole Mesh</a></li> 
    <li><a href="#" onclick="_Viewer.tgStruct('SemanticTreePanel','<a href=# onclick={dq}_Viewer.loadLabelMesh({label});{dq}>{label}</a> {type}<br>{children}');">Load mesh by node</a><br /></li>
    </ol>

    <p>
    A much simpler binary format is available to parse is available form the web repository calling the function
    WriteBinaryMesh with the <b>XbimRepo.BinaryMeshMode.PositionsNormalsIndices</b> 
        parameter (see Data3D.aspx for the calling code).</p>
    <h2>Events</h2>
    <ol>
    <li><a href="#" onclick="RegisterSelectionHandlers();">Register Selection Handlers</a><br /></li>
    <li><a href="#" onclick="UnRegisterSelectionHandlers();">UnRegister Selection Handlers</a><br /></li>
    </ol>
    <h2>Todo</h2>
    <ol>
    <li>backgroundcolor <strong>bloody canvas does not work!!!!</strong></li>
    </ol>
    <a href="#" onclick="$('#SemanticTreePanel').text('');return null;">Clear log</a>
    <div id="SemanticTreePanel">
    </div>
    </div>
</asp:Content>

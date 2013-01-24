<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/Site.master" CodeBehind="MultiFiles.aspx.cs" Inherits="Xbim.SceneJSWebViewer.MultiFiles" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="HeadContent">
 <link href="Content/themes/base/minified/jquery-ui.min.css" rel="stylesheet" type="text/css" />
 <script type="text/javascript" src="Scripts/jquery-1.7.2.min.js"></script>
 <script type="text/javascript" src="Scripts/jquery-ui-1.8.20.min.js" ></script>

<style type="text/css">
    
div.maindiv     {width:900px; height:170px; background-color:Gray;}
div.iframediv   {width:300px; height:170px; float:left; display:inline}
div.download    {width:100px; height:170px; float:left; display:inline; padding-left:10px; padding-top:20px;}
div.title       {width:400px; height:170px; float:left; display:inline; padding-left:10px; padding-top:20px;}

</style>

</asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="MainContent">
    
    <div class="maindiv">
        <div class="iframediv">
            <iframe width="300px" height="200px" frameborder="0" scrolling="no" src="model01.aspx"></iframe>
        </div>
        <div class="title">
            <panel id="model01_title" runat="server"></panel>
        </div>

        <div class="download">
            <a href="models/nbl_WC_Close-Coupled.ifc">download button</a>
        </div>
    </div>
    <div style="clear:both;"></div>

    <div class="maindiv">
        <div class="iframediv">
            <iframe width="300px" height="200px" frameborder="0" scrolling="no" src="model02.aspx"></iframe>
        </div>
        <div class="title">
            <panel id="model02_title" runat="server"></panel>
        </div>

        <div class="download">
            <a href="models/nbl_WC_Close-Coupled.ifc">download button</a>
        </div>
    </div>
    <div style="clear:both;"></div>

    <div class="maindiv">
        <div class="iframediv">
            <iframe width="300px" height="200px" frameborder="0" scrolling="no" src="model03.aspx"></iframe>
        </div>
        <div class="title">
            <panel id="model03_title" runat="server"></panel>
        </div>

        <div class="download">
            <a href="models/nbl_WC_Close-Coupled.ifc">download button</a>
        </div>
    </div>
    <div style="clear:both;"></div>

    <div class="maindiv">
        <div class="iframediv">
            <iframe width="300px" height="200px" frameborder="0" scrolling="no" src="model04.aspx"></iframe>
        </div>
        <div class="title">
            <panel id="model04_title" runat="server"></panel>
        </div>

        <div class="download">
            <a href="models/nbl_Sink_Belfast.ifc">download button</a>
        </div>
    </div>
    <div style="clear:both;"></div>

    <div class="maindiv">
        <div class="iframediv">
            <iframe width="300px" height="200px" frameborder="0" scrolling="no" src="model05.aspx"></iframe>
        </div>
        <div class="title">
            <panel id="model05_title" runat="server"></panel>
        </div>

        <div class="download">
            <a href="models/nbl_Shower_Rctngl.ifc">download button</a>
        </div>
    </div>
    <div style="clear:both;"></div>

    <div class="maindiv">
        <div class="iframediv">
            <iframe width="300px" height="200px" frameborder="0" scrolling="no" src="model06.aspx"></iframe>
        </div>
        <div class="title">
            <panel id="model06_title" runat="server"></panel>
        </div>

        <div class="download">
            <a href="models/nbl_SanitaryAccessory_Hand-Drier.ifc">download button</a>
        </div>
    </div>
    
</asp:Content>










<%@ Page Title="Home Page" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="DataProcessingWebApp._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <div class="jumbotron">
        <h1>File And Data Processing App</h1>
        <p class="lead">This App automates File and Data Processing for Alegeus and COBRA Source Files.</p>
        <p class="lead">It also fetches Result &amp; Error Files from Alegeus and allows easy tracking </p>
    </div>

    <div class="row">
        <div class="">
            <p>
                &nbsp;<asp:Button ID="cmdCopyTestFiles" runat="server" Text="CopyTestFiles" OnClick="cmdCopyTestFiles_Click" />
&nbsp;&nbsp;&nbsp;&nbsp; Copy test files BEFORE starting a new process</p>
        </div>
        <div class="">
            <p>
           
            </p>
        </div>
        <div class="">
            <p>
                &nbsp;
                <asp:Button ID="cmdProcessCobraFiles" runat="server" Text="ProcessCobraFiles" OnClick="cmdProcessCobraFiles_Click" />
&nbsp;&nbsp;&nbsp;&nbsp; Process COBRA Source Files</p>
        </div>
        <div class="">
            <p>
               
                &nbsp;
                <asp:Button ID="cmdProcessAlegeusFiles" runat="server" Text="ProcessAlegeusFiles" OnClick="cmdProcessAlegeusFiles_Click" />
&nbsp;&nbsp;&nbsp;&nbsp; Process Alegeus Source Files</p>
        </div>
        <div class="">
            <p>
               
                &nbsp;
                <asp:Button ID="cmdRetrieveFtpErrorLogs" runat="server" Text="RetrieveFtpErrorLogs" OnClick="cmdRetrieveFtpErrorLogs_Click" />
                &nbsp;&nbsp;&nbsp;&nbsp;Get Alegeus Upload Results
               
            </p>
        </div>
        <div class="">
            <p>
               
                &nbsp;
                <asp:Button ID="cmdOpenAccessDB" runat="server" Text="Open Access UI" OnClick="cmdOpenAccessDB_Click" />
&nbsp;&nbsp;&nbsp;&nbsp; Open MS Access User Interface</p>
        </div>
         <div class="">
            <p>
               
                &nbsp;
                <asp:Button ID="cmdDoALL" runat="server" Text="Do All Above" />
&nbsp;&nbsp;&nbsp;&nbsp; Do All Above in Sequence</p>
        </div>
             <p>
                Logs</p>
        </div>
        <div class="">
            <asp:GridView ID="listLogs" runat="server" Width="1434px" AllowPaging="True" AllowSorting="True">
            </asp:GridView>
        </div>

</asp:Content>

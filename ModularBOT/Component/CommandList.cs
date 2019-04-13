using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ModularBOT.Component
{
    public enum CommandTypes
    {
        Core,
        Custom,
        GCustom,
        Module
    }
    internal class CommandItem
    {
        public string Cmdname { get; private set; }
        public string CmdPerms { get; private set; }
        public string CmdSummary { get; private set; }
        public string CmdUsage { get; private set; }
        public string CoreNote { get; private set; }
        public int Index { get; private set; }

        private CommandTypes CmdType = CommandTypes.Custom;
        public string CoreID()
        {
            if (CmdType == CommandTypes.Core)
            {
                return "CORE";
            }
            if (CmdType == CommandTypes.Module)
            {
                return "MODL";
            }
            else
            {
                return "CSTM";
            }
        }
        public string GenerateTab()
        {
            string UsageSummary = $@"<p><b>Summary: </b>{CmdSummary}</p>
														<br/>
														<p><b>Usage: </b></p>
														<ul>
															<li>{CmdUsage}</li>
														</ul>";
            if(CmdType == CommandTypes.Custom || CmdType == CommandTypes.GCustom)
            {
                UsageSummary = "";
            }
            return
                $@"<!-- START-TAB --><div class='panel panel-default'>
												<div class='panel-heading ' role='tab' id='hd{CoreID()}{Index}'>
													<table>
													  <tr><td><img src = 'http://rmsoftware.org/images/Icons/appIcons/cbot.png' style='padding-right:5px;'/></td><td>
													  <h3 class='panel-title' data-toggle='collapse' data-parent='#accordion{CoreID()}'><a class='collapsed' data-toggle='collapse' data-parent='#accordion{CoreID()}' href='#dlid{CoreID()}{Index}' aria-expanded='false' aria-controls='dlid{CoreID()}{Index}'>{Cmdname}</a></h3>{CoreNote}
													  </td>
													   </tr>
													   <tr><td></td><td></td></tr></table>
												</div>
												<div id = 'dlid{CoreID()}{Index}' class='panel-collapse collapse' role='tabpanel' aria-labelledby='hd{CoreID()}{Index}'>
													<div class='panel-body'>
                                                        {UsageSummary}
														<p><b>Permissions: </b></p>
														<ul>
														<li>{CmdPerms}</li>
														</ul>
														<br/>
													</div>
												</div>
										</div>
										<!-- END TAB -->";
        }

        public CommandItem(string CmdName, string CmdPerms, string CmdSummary, string CmdUsage, CommandTypes CmdType, int Index)
        {
            Cmdname = WebUtility.HtmlEncode(CmdName);
            this.CmdPerms = CmdPerms;
            this.CmdSummary = WebUtility.HtmlEncode(CmdSummary);
            this.CmdUsage = WebUtility.HtmlEncode(CmdUsage);
            this.CmdType = CmdType;
            if (CmdType == CommandTypes.Core)
            {
                CoreNote = "<b class='SourceNote'>CORE</b><b class='BetaNote'>GLOBAL</b>";
            }
            if (CmdType == CommandTypes.Module)
            {
                CoreNote = "<b class='SourceNote'>MODULE</b><b class='BetaNote'>GLOBAL</b>";//TODO: Make module commands subject to guild availability.
            }
            if (CmdType == CommandTypes.GCustom)
            {
                CoreNote = "<b class='BetaNote'>GLOBAL</b>";
            }
            this.Index = Index;

        }
    }

    internal class CommandList
    {
        List<CommandItem> Commands { get; set; }

        int index = 0;

        public string Botname { get; private set; }

        public string ContextName { get; private set; }

        public string GenerateCoreCommandList()
        {
            string outresult = "";
            foreach (var item in Commands)
            {
                if (item.CoreID() != "CORE")
                {
                    continue;
                }
                outresult += item.GenerateTab();
            }
            if(string.IsNullOrWhiteSpace(outresult))
            {
                outresult = "<center><img src='https://cdn.rms0.org/assets/images/MB/CL/err_null.jpg'/><br/><b>There's nothing here.</b></center>";
            }
            return outresult;
        }
        public string GenerateModuleCommandList()
        {
            string outresult = "";
            foreach (var item in Commands)
            {
                if (item.CoreID() != "MODL")
                {
                    continue;
                }
                outresult += item.GenerateTab();
            }
            if (string.IsNullOrWhiteSpace(outresult))
            {
                outresult = "<center><img src='https://cdn.rms0.org/assets/images/MB/CL/err_null.jpg'/><br/><b>There's nothing here.</b></center>";
            }
            return outresult;
        }

        public string GenerateOtherCommandList()
        {
            string outresult = "";
            foreach (var item in Commands)
            {
                if (item.CoreID() != "CSTM")
                {
                    continue;
                }
                outresult += item.GenerateTab();
            }
            if (string.IsNullOrWhiteSpace(outresult))
            {
                outresult = "<center><img src='https://cdn.rms0.org/assets/images/MB/CL/err_null.jpg'/><br/><b>There's nothing here.</b></center>";
            }
            return outresult;
        }

        public CommandList(string clientUsername, string ContextName)
        {
            Botname = clientUsername;
            this.ContextName = ContextName;
            Commands = new List<CommandItem>();
        }

        public void AddCommand(string cmdName, AccessLevels? requiredAccessLevel, CommandTypes commandType, string summary = null, string usage = null)
        {
            
            string permission = "AccessLevels." + requiredAccessLevel?.ToString();
            if((commandType == CommandTypes.Module || commandType == CommandTypes.Core) && !requiredAccessLevel.HasValue)
            {
                permission = "The Required access level was not properly annotated within the Remarks() attribute.";
            }
            if ((commandType == CommandTypes.Module || commandType == CommandTypes.Core) && requiredAccessLevel == AccessLevels.Blacklisted)
            {
                permission = "<img src='https://cdn.rms0.org/assets/images/MB/CL/err_illg.jpg' width='200px' height='112px'/> <b>AccessLevels.Blacklisted</b>";
            }
            if (string.IsNullOrWhiteSpace(summary))
            {
                summary = "No summary was provided for this command.";
            }
            if (string.IsNullOrWhiteSpace(usage))
            {
                usage = "No usage information was provided for this command.";
            }
            Commands.Add(new CommandItem(cmdName, permission, summary, usage, commandType, index));
            index++;
        }

        public string GetFullHTML()
        {
            return $@"
<!DOCTYPE html>

<html class='no-js'>
    <head>
        <meta charset='utf-8'>
        <title>{Botname}'s Command List</title>
        <meta name='description' content=''>
        <meta name='viewport' content='width=device-width, initial-scale=1, maximum-scale=1'>

        <!-- Fonts -->
        <link href='http://fonts.googleapis.com/css?family=Open+Sans:400,300,700' rel='stylesheet' type='text/css'>
        <link href='http://fonts.googleapis.com/css?family=Dosis:400,700' rel='stylesheet' type='text/css'>

        <!-- Bootsrap -->
        <link rel='stylesheet' href='http://rmsoftware.org/assets/css/bootstrap.min.css'>


        <!-- Font awesome -->
        <link rel='stylesheet' href='http://rmsoftware.org/assets/css/font-awesome.min.css'>

        <!-- PrettyPhoto -->
        <link rel='stylesheet' href='http://rmsoftware.org/assets/css/prettyPhoto.css'>

        <!-- Template main Css -->
        <link rel='stylesheet' href='http://rmsoftware.org/assets/css/style.css'>
        
        <!-- Modernizr -->
        <script src='http://rmsoftware.org/assets/js/modernizr-2.6.2.min.js'></script>


    </head>
    <body>
    <!-- NAVBAR
    ================================================== -->

    <header class='main-header'>
        
    
        <nav class='navbar navbar-static-top'>

           
            <div class='navbar-main'>
              
              <div class='container'>

                <div class='navbar-header'>
                  <button type='button' class='navbar-toggle collapsed' data-toggle='collapse' data-target='#navbar' aria-expanded='false' aria-controls='navbar'>

                    <span class='sr-only'>Toggle navigation</span>
                    <span class='icon-bar'></span>
                    <span class='icon-bar'></span>
                    <span class='icon-bar'></span>

                  </button>
                  
                  <a class='navbar-brand' href='https://rmsoftware.org'><img src='http://rmsoftware.org/assets/images/sadaka-logo.png' alt=''></a>
                  
                </div>

                <div id='navbar' class='navbar-collapse collapse pull-right'>

                  <ul class='nav navbar-nav'>
					<li><a href='#' class='is-active'>List</a></li>
                    
                  </ul>

                </div> <!-- /#navbar -->

              </div> <!-- /.container -->
              
            </div> <!-- /.navbar-main -->


        </nav> 

    </header> <!-- /. main-header -->


	<div class='page-heading text-center'>

		<div class='container zoomIn animated'>
			
			<h1 class='page-title'>{Botname}'s Command list<span class='title-under'></span></h1>
			<p class='page-description'>
				Powered by RMSoftware.ModularBot.<br/><small>List generated in {ContextName} on {DateTime.Now}</small>
			</p>
			
		</div>

	</div>

	<div class='main-container'>

		<div class='container'>

			
			
			<div class='row '>

				<div class='col-md-12 fadeIn'>

						<div role='tabpanel'>

							  <!-- Nav tabs -->
							  <ul class='nav nav-tabs' role='tablist'>
							    <li role='presentation'  class='active'><a href='#cmdlist_Core' aria-controls='cmdlist_Core' role='tab' data-toggle='tab'>Core Commands</a></li>
                                <li role='presentation'  ><a href='#cmdlist_Modules' aria-controls='cmdlist_Modules' role='tab' data-toggle='tab'>Module Commands</a></li>
                                <li role='presentation'  ><a href='#cmdlist_Other' aria-controls='cmdlist_Other' role='tab' data-toggle='tab'>Custom Commands</a></li>
							  </ul>

							  <!-- Tab panes -->
							  <div class='tab-content'>
							    <div role='tabpanel' class='tab-pane active' id='cmdlist_Core'>
							    	<div class='panel-group' id='accordionCORE' role='tablist' aria-multiselectable='true'>{GenerateCoreCommandList()}</div>

									<p></p>

								</div>
                                <div role='tabpanel' class='tab-pane' id='cmdlist_Modules'>
							    	<div class='panel-group' id='accordionMODL' role='tablist' aria-multiselectable='true'>{GenerateModuleCommandList()}</div>

									<p></p>

								</div>
                                <div role='tabpanel' class='tab-pane' id='cmdlist_Other'>
							    	<div class='panel-group' id='accordionCSTM' role='tablist' aria-multiselectable='true'>{GenerateOtherCommandList()}</div>

									<p></p>

								</div>

						</div>

						<p></p>
					</div>

				</div>

				
			</div>

		</div>

		

		


	</div> <!-- /.main-container  -->


    <footer class='main-footer'>
	
        <div class='footer-bottom'>

            <div class='container text-right'>
                Copyright © 2011-{DateTime.Now.Year} RMSoftware Development.
            </div>
        </div>
    </footer>
        <!-- jQuery -->
        <script src='https://ajax.googleapis.com/ajax/libs/jquery/1.11.1/jquery.min.js'></script>

        <!-- Bootsrap javascript file -->
        <script src='http://rmsoftware.org/assets/js/bootstrap.min.js'></script>

        <!-- PrettyPhoto javascript file -->
        <script src='http://rmsoftware.org/assets/js/jquery.prettyPhoto.js'></script>

        <!-- Template main javascript -->
        <script src='http://rmsoftware.org/assets/js/main.js'></script>

    </body>
</html>
";
        }
    }
}

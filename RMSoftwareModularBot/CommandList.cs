using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Net;
namespace RMSoftware.ModularBot
{
    public class CommandItem
    {
        public string Cmdname { get; private set; }
        public string cmdPerms { get; private set; }
        public string cmdSummary { get; private set; }
        public string cmdUsage { get; private set; }
        public string coreNote { get; private set; }
        public int index { get; private set; }
        bool is_core = false;
        string CoreID()
        {
            if(is_core)
            {
                return "CORE";
            }
            else
            {
                return "CSTM";
            }
        }
        public string generateTab()
        {
            return
                $@"<!-- START-TAB --><div class='panel panel-default'>
												<div class='panel-heading ' role='tab' id='hd{CoreID()}{index}'>
													<table>
													  <tr><td><img src = 'http://rmsoftware.org/images/Icons/appIcons/cbot.png' style='padding-right:5px;'/></td><td>
													  <h3 class='panel-title' data-toggle='collapse' data-parent='#accordion{CoreID()}'><a class='collapsed' data-toggle='collapse' data-parent='#accordion{CoreID()}' href='#dlid{CoreID()}{index}' aria-expanded='false' aria-controls='dlid{CoreID()}{index}'>{Cmdname}</a></h3>{coreNote}
													  </td>
													   </tr>
													   <tr><td></td><td></td></tr></table>
												</div>
												<div id = 'dlid{CoreID()}{index}' class='panel-collapse collapse' role='tabpanel' aria-labelledby='hd{CoreID()}{index}'>
													<div class='panel-body'>
														<p><b>Summary: </b>{cmdSummary}</p>
														<br/>
														<p><b>Usage: </b></p>
														<ul>
															<li>{cmdUsage}</li>
														</ul>
														<p><b>Permissions: </b></p>
														<ul>
														<li>{cmdPerms}</li>
														</ul>
														<br/>
													</div>
												</div>
										</div>
										<!-- END TAB -->";
        }

        public CommandItem(string CmdName, string CmdPerms, string CmdSummary, string CmdUsage, bool IsCore,int Index)
        {
            Cmdname =  WebUtility.HtmlEncode(CmdName);
            cmdPerms = WebUtility.HtmlEncode(CmdPerms);
            cmdSummary = WebUtility.HtmlEncode(CmdSummary);
            cmdUsage = WebUtility.HtmlEncode(CmdUsage);
            is_core = IsCore;
            if(IsCore)
            {
                coreNote = "<small class='SourceNote'>CORE</small>";
            }
            index = Index;

        }
    }

    public class CommandList
    {
        List<CommandItem> commands { get; set; }

        int index = 0;
        
        public string botname { get; private set; }

        public string GenerateCoreCommandList()
        {
            string outresult = "";
            foreach (var item in commands)
            {
                if (string.IsNullOrWhiteSpace(item.coreNote))
                {
                    continue;
                }
                outresult += item.generateTab();
            }
            return outresult;
        }
        public string GenerateOtherCommandList()
        {
            string outresult = "";
            foreach (var item in commands)
            {
                if (!string.IsNullOrWhiteSpace(item.coreNote))
                {
                    continue;
                }
                outresult += item.generateTab();
            }
            return outresult;
        }

        public CommandList(string clientUsername)
        {
            botname = clientUsername;
            commands = new List<CommandItem>();
        }

        public void AddCommand(string cmdName,bool restricted, bool isCore,string summary=null,string usage=null)
        {
            string permission = restricted ? "Requires special permissions" : "Unrestricted";
            if(string.IsNullOrWhiteSpace(summary))
            {
                summary = "No summary was provided for this command.";
            }
            if (string.IsNullOrWhiteSpace(usage))
            {
                usage = "No usage information was provided for this command.";
            }
            commands.Add(new CommandItem(cmdName, permission, summary, usage, isCore, index));
            index++;
        }

        public string GetFullHTML()
        {
            return $@"
<!DOCTYPE html>

<html class='no-js'>
    <head>
        <meta charset='utf-8'>
        <title>{botname}'s Command List</title>
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
                  
                  <a class='navbar-brand' href='index.php'><img src='http://rmsoftware.org/assets/images/sadaka-logo.png' alt=''></a>
                  
                </div>

                <div id='navbar' class='navbar-collapse collapse pull-right'>

                  <ul class='nav navbar-nav'>
					<li><a href='http://rmsoftware.org'>RMsoftware.org</a></li>
                    
                  </ul>

                </div> <!-- /#navbar -->

              </div> <!-- /.container -->
              
            </div> <!-- /.navbar-main -->


        </nav> 

    </header> <!-- /. main-header -->


	<div class='page-heading text-center'>

		<div class='container zoomIn animated'>
			
			<h1 class='page-title'>Command list<span class='title-under'></span></h1>
			<p class='page-description'>
				Powered by RMSoftware.ModularBot
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
                                <li role='presentation'  ><a href='#cmdlist_Other' aria-controls='cmdlist_Other' role='tab' data-toggle='tab'>Module & Custom Commands</a></li>
							  </ul>

							  <!-- Tab panes -->
							  <div class='tab-content'>
							    <div role='tabpanel' class='tab-pane active' id='cmdlist_Core'>
							    	<div class='panel-group' id='accordionCORE' role='tablist' aria-multiselectable='true'>{GenerateCoreCommandList()}</div>

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
                Copyright © 2011-2017 RMSoftware Development. Responsive framework by <a href='http://ouarmedia.com'>SADAKA @ Ouarmedia</a>
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

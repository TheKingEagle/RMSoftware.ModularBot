using ModularBOT.Entity;
using RMSoftware.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;

namespace ModularBOT.Component
{
    internal class WebPortal
    {
        private QuickServer qs;
        private int port;
        private string host;

        internal WebPortal(int _port, string _host)
        {

            port = _port;
            host = _host;
            qs = new QuickServer(host, port);
            qs.DefineRoute("/", HomeRoute);
            qs.DefineStaticFileRoute("/asset", "wrd_asset");
            qs.Start();
        }


        private void HomeRoute(HttpListenerContext context) {
            WebPortalPage home = new WebPortalPage()
            {
                LogoUrl = "https://workflow.rms0.org/assets/static/RMSoftwareICO.png",
                Title = "ModularBOT",
                Content = "<h1>Hello World</h1>"
            };
            qs.SendResponse(context.Response, home.ToHTML());
        }
    }
}

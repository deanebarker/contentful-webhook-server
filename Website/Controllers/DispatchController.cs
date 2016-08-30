using ContentfulWebhookServer.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

namespace Website.Controllers
{
    public class DispatchController : Controller
    {
        [AcceptVerbs(HttpVerbs.Get | HttpVerbs.Post)]
        public JsonResult Index()
        {
            var results = WebhookDispatcher.Process(new WebhookRequest(Request));
            return Json(results, JsonRequestBehavior.AllowGet);
        }
    }
}
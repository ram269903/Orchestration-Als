//using Common.ActivityLogs;
//using Common.ActivityLogs.Model;
//using Microsoft.AspNetCore.Mvc.Controllers;
//using Microsoft.AspNetCore.Mvc.Filters;
//using Newtonsoft.Json;
//using System;
//using System.Linq;

//namespace Common.Web.Filters
//{
//    public class ActivityLogFilter : ActionFilterAttribute
//    {
//        private readonly IActivityLogService _activityLogService;

//        public ActivityLogFilter(IActivityLogService activityLogService)
//        {
//            _activityLogService = activityLogService;
//        }

//        public override void OnActionExecuting(ActionExecutingContext filterContext)
//        {
//            var loginUser = filterContext.HttpContext.User?.FindFirst("LoginId")?.Value;
//            var parameters = GetParameterValues(filterContext);
//            var remoteIpAddress = filterContext.HttpContext.Connection.RemoteIpAddress;
//            var machine = Environment.GetEnvironmentVariable("COMPUTERNAME");
//            var module = filterContext.Controller.ToString().Split('.').Last();
//            var action = ((ControllerActionDescriptor)filterContext.ActionDescriptor).ActionName;
//            var application = ((ControllerActionDescriptor)filterContext.ActionDescriptor).DisplayName.Split('(', ')')[1];
            
//            //_ = _activityLogService.LogActivity(new ActivityLog
//            //{

//            //    Action = action,
//            //    ActivityOn = DateTime.Now,
//            //    Application = application,
//            //    LoginId = loginUser,
//            //    Module = module,
//            //    OtherInfo = parameters,
//            //    SourceIP = remoteIpAddress.ToString(),
//            //    WebServer = machine

//            //}).Result;

//            // Finishes executing the Action as normal 
//            base.OnActionExecuting(filterContext);
//        }

//        private static string GetParameterValues(ActionExecutingContext filterContext)
//        {

//            //StringBuilder builder = new StringBuilder();

//            //foreach (KeyValuePair<string, object> entry in filterContext.ActionArguments)
//            //{
//            //    builder.AppendFormat("ParamterName = {0}; ParamterValue = {1} \n ", entry.Key, JsonConvert.SerializeObject(entry.Value) ?? "");
//            //}

//            //return builder.ToString();

//            return JsonConvert.SerializeObject(filterContext.ActionArguments);


//        }
//    }
//}

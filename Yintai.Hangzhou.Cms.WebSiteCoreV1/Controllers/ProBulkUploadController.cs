﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Yintai.Hangzhou.Cms.WebSiteCoreV1.Models;
using Yintai.Hangzhou.Data.Models;
using Yintai.Hangzhou.WebSupport.Mvc;
using Yintai.Hangzhou.Cms.WebSiteCoreV1.Util;
using Yintai.Architecture.Common.Models;

namespace Yintai.Hangzhou.Cms.WebSiteCoreV1.Controllers
{
    [AdminAuthorize]
    public class ProBulkUploadController : UserController
    {
        private string _fileFullPath;
        private const string _Session_Key = "probulksessionid";
        public ProBulkUploadController()
        {
        }

        private string StorageRoot
        {
            get { return Path.Combine(System.Web.HttpContext.Current.Server.MapPath(ConfigurationManager.AppSettings["BulkFileFolder"])); } //Path should! always end with '/'
        }
        public int JobId
        {
            get
            {
                int jobID;
                int.TryParse(this.ControllerContext.HttpContext.Request.Cookies[_Session_Key].Value, out jobID);
                return jobID;
            }
            set
            {
                if (HttpContext.Response.Cookies[_Session_Key] != null)
                    HttpContext.Response.Cookies[_Session_Key].Value = value.ToString();
                else
                    HttpContext.Response.Cookies.Add(new HttpCookie(_Session_Key, value.ToString()));

            }
        }
        public ActionResult Display(int? id)
        {
            PrepareBulkUpload();

            if (id != null)
                JobId = id.Value;
            ProUploadService helpService = new ProUploadService(this);
            var job = helpService.Job(id);
            ViewBag.UploadedProducts = helpService.List(id);
            ViewBag.UploadedImages = helpService.ListImages(id);
            SetGroupIdIfNeed(id);
            return View(job);
        }
        public ActionResult List(PagerRequest request)
        {
            int totalCount;
            var jobs = new ProUploadService(this).JobList(request.PageIndex,request.PageSize,out totalCount);
            return View(new Pager<ProductUploadJob>(request,totalCount){
                 Data = jobs
            });
        }
        public ActionResult Delete(int? id)
        {
            if (id != null &&
                id.Value > 0)
                new ProUploadService(this).Delete(id.Value);

            return RedirectToAction("List");
        }
        public ActionResult Detail(int? uiid, int? groupId)
        {
            ViewBag.JobId = groupId;
            if (uiid != null &&
                uiid.Value > 0)
                return View(new ProUploadService(this).UploadItemDetail(uiid.Value));
            return RedirectToAction("List");
        }
        [HttpPost]
        public ActionResult Detail(ProductUploadInfo updatedModel, int? groupId)
        {
            if (ModelState.IsValid)
            {
                var model = new ProUploadService(this).ItemDetailUpdate(updatedModel);
                return View(model);
            }
            else
                return View(updatedModel);

        }
        public ActionResult DeleteItem(int id, int groupId)
        {
            new ProUploadService(this).DeleteItem(id);
            return RedirectToAction("Display", new { id = groupId });
        }
        private void SetGroupIdIfNeed(int? groupId)
        {
            if (groupId == null ||
                groupId.Value == 0)
                return;
            JobId = groupId.Value;
        }

        private void PrepareBulkUpload()
        {
            if (HttpContext.Response.Cookies[_Session_Key] != null)
                HttpContext.Response.Cookies[_Session_Key].Value = "0";
        }

        public PartialViewResult Validate()
        {
            var valResult = new ProUploadService(this).Validate();
            return PartialView("_ValidatePartial", valResult);
        }
  
        public PartialViewResult Publish()
        {
            var pubResult = new ProUploadService(this).Publish().ToArray();
            return PartialView("_PublishPartial", pubResult);
        }
        [HttpPost]
        public JsonResult Upload()
        {
            HttpContextBase context = ControllerContext.HttpContext;
            UploadFile(context);
            ProductUploadInfo[] array = new ProUploadService(_fileFullPath, this).Stage().ToArray<ProductUploadInfo>();
            return Json(array).EnsureContentType(context.Request);
        }
        [HttpPost]
        public JsonResult UploadImage()
        {
            if (!EnsureJobIdContext())
                throw new Exception("还没有导入商品");
            HttpContextBase context = ControllerContext.HttpContext;
            UploadFile(context);

            return Json(new ProUploadService(_fileFullPath, this).ImageStage().ToArray()).EnsureContentType(context.Request);
        }

        private bool EnsureJobIdContext()
        {
            return JobId > 0;
        }
        // Upload file to the server
        private void UploadFile(HttpContextBase context)
        {
            var statuses = new List<FilesStatus>();
            var headers = context.Request.Headers;
            UploadWholeFile(context, statuses);
            /*
            if (string.IsNullOrEmpty(headers["X-File-Name"]))
            {
                UploadWholeFile(context, statuses);
            }
            else
            {
                UploadPartialFile(headers["X-File-Name"], context, statuses);
            }
            */
        }

        // Upload partial file
        private void UploadPartialFile(string fileName, HttpContextBase context, List<FilesStatus> statuses)
        {
            if (context.Request.Files.Count != 1) throw new HttpRequestValidationException("Attempt to upload chunked file containing more than one fragment per request");
            var inputStream = context.Request.Files[0].InputStream;
            var fullName = StorageRoot + Path.GetFileName(fileName);

            using (var fs = new FileStream(fullName, FileMode.Append, FileAccess.Write))
            {
                var buffer = new byte[1024];

                var l = inputStream.Read(buffer, 0, 1024);
                while (l > 0)
                {
                    fs.Write(buffer, 0, l);
                    l = inputStream.Read(buffer, 0, 1024);
                }
                fs.Flush();
                fs.Close();
            }
            _fileFullPath = fullName;
        }

        // Upload entire file
        private void UploadWholeFile(HttpContextBase context, List<FilesStatus> statuses)
        {
            for (int i = 0; i < context.Request.Files.Count; i++)
            {
                var file = context.Request.Files[i];

                var fullPath = StorageRoot + Path.GetFileName(file.FileName);

                file.SaveAs(fullPath);

                _fileFullPath = fullPath;
            }

        }
    }
}

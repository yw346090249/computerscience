﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using ImageResizer;
using Intellipix.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Configuration;
using System.Threading.Tasks;
using System.IO;
using Microsoft.ProjectOxford.Vision;

namespace Intellipix.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index(string id)
        {
            // Pass a list of blob URIs and captions in ViewBag
            CloudStorageAccount account = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
            CloudBlobClient client = account.CreateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference("photos");
            List<BlobInfo> blobs = new List<BlobInfo>();

            foreach (IListBlobItem item in container.ListBlobs())
            {
                var blob = item as CloudBlockBlob;

                if (blob != null)
                {
                    blob.FetchAttributes(); // Get blob metadata

                    if (String.IsNullOrEmpty(id) || HasMatchingMetadata(blob, id))
                    {
                        var caption = blob.Metadata.ContainsKey("Caption") ? blob.Metadata["Caption"] : blob.Name;

                        blobs.Add(new BlobInfo()
                        {
                            ImageUri = blob.Uri.ToString(),
                            ThumbnailUri = blob.Uri.ToString().Replace("/photos/", "/thumbnails/"),
                            Caption = caption
                        });
                    }
                }
            }

            ViewBag.Blobs = blobs.ToArray();
            ViewBag.Search = id; // Prevent search box from losing its content
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        [HttpPost]
        public async Task<ActionResult> Upload(HttpPostedFileBase file)
        {
            if (file != null && file.ContentLength > 0)
            {
                // Make sure the user selected an image file
                if (!file.ContentType.StartsWith("image"))
                {
                    TempData["Message"] = "Only image files may be uploaded";
                }
                else
                {
                    try
                    {
                        // Save the original image in the "photos" container
                        CloudStorageAccount account = CloudStorageAccount.Parse(ConfigurationManager.AppSettings["StorageConnectionString"]);
                        CloudBlobClient client = account.CreateCloudBlobClient();
                        CloudBlobContainer container = client.GetContainerReference("photos");
                        CloudBlockBlob photo = container.GetBlockBlobReference(Path.GetFileName(file.FileName));
                        await photo.UploadFromStreamAsync(file.InputStream);

                        // Generate a thumbnail and save it in the "thumbnails" container
                        using (var outputStream = new MemoryStream())
                        {
                            file.InputStream.Seek(0L, SeekOrigin.Begin);
                            var settings = new ResizeSettings { MaxWidth = 192 };
                            ImageBuilder.Current.Build(file.InputStream, outputStream, settings);
                            outputStream.Seek(0L, SeekOrigin.Begin);
                            container = client.GetContainerReference("thumbnails");
                            CloudBlockBlob thumbnail = container.GetBlockBlobReference(Path.GetFileName(file.FileName));
                            await thumbnail.UploadFromStreamAsync(outputStream);
                        }

                        // Submit the image to Azure's Computer Vision API
                        VisionServiceClient vision = new VisionServiceClient(
                            ConfigurationManager.AppSettings["SubscriptionKey"],
                            ConfigurationManager.AppSettings["VisionEndpoint"]
                        );

                        VisualFeature[] features = new VisualFeature[] { VisualFeature.Description };
                        var result = await vision.AnalyzeImageAsync(photo.Uri.ToString(), features);

                        // Record the image description and tags in blob metadata
                        photo.Metadata.Add("Caption", result.Description.Captions[0].Text);

                        for (int i = 0; i < result.Description.Tags.Length; i++)
                        {
                            string key = String.Format("Tag{0}", i);
                            photo.Metadata.Add(key, result.Description.Tags[i]);
                        }

                        await photo.SetMetadataAsync();
                    }
                    catch (Exception ex)
                    {
                        // In case something goes wrong
                        TempData["Message"] = ex.Message;
                    }
                }
            }

            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Search(string term)
        {
            return RedirectToAction("Index", new { id = term });
        }

        private bool HasMatchingMetadata(CloudBlockBlob blob, string term)
        {
            foreach (var item in blob.Metadata)
            {
                if (item.Key.StartsWith("Tag") && item.Value.Equals(term, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }

            return false;
        }
    }
}